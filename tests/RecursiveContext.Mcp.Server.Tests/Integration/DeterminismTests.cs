using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.Analysis;

namespace RecursiveContext.Mcp.Server.Tests.Integration;

/// <summary>
/// Determinism proof tests - THE critical requirement.
/// These tests prove that same input = same output every time.
/// If any test fails, the fundamental RLM principle is violated.
/// </summary>
public class DeterminismTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrails;
    private readonly ContentAnalysisService _contentAnalysisService;
    private readonly ChunkingService _chunkingService;
    private readonly AggregationService _aggregationService;
    
    private const int RepeatCount = 10;

    public DeterminismTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"determinism_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 1000, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrails = new GuardrailService(settings);
        _contentAnalysisService = new ContentAnalysisService(_pathResolver, _guardrails);
        _chunkingService = new ChunkingService(_pathResolver, _guardrails);
        var patternService = new PatternMatchingService(_pathResolver, _guardrails);
        _aggregationService = new AggregationService(_pathResolver, _guardrails, patternService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateFixedTestFile(string name, string content)
    {
        var filePath = Path.Combine(_tempDir, name);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content);
        return name;
    }

    #region CountPatternMatches Determinism

    [Fact]
    public async Task CountPatternMatches_TenCalls_IdenticalResults()
    {
        // Fixed test data
        var relativePath = CreateFixedTestFile("count_test.cs", 
            "public class Foo { }\npublic class Bar { }\npublic class Baz { }\nprivate class Qux { }");
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await CountPatternMatchesTool.CountPatternMatches(
                _contentAnalysisService, relativePath, @"class\s+\w+", 100, true, true, CancellationToken.None);
            results.Add(result);
        }

        // All results must be identical
        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));

        // Verify count is correct
        using var doc = JsonDocument.Parse(first);
        Assert.Equal(4, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task CountPatternMatches_ComplexRegex_Deterministic()
    {
        var relativePath = CreateFixedTestFile("regex_test.cs", 
            "int x = 1;\nint y = 2;\nstring s = \"hello\";\nint z = 99;\ndouble d = 3.14;");
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await CountPatternMatchesTool.CountPatternMatches(
                _contentAnalysisService, relativePath, @"int\s+\w+\s*=\s*\d+", 100, true, true, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));
    }

    #endregion

    #region SearchWithContext Determinism

    [Fact]
    public async Task SearchWithContext_TenCalls_IdenticalResults()
    {
        var content = "line 1\nTARGET line\nline 3\nTARGET again\nline 5\nTARGET final\nline 7";
        var relativePath = CreateFixedTestFile("search_test.txt", content);
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await SearchWithContextTool.SearchWithContext(
                _contentAnalysisService, relativePath, "TARGET", 1, 100, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));

        // Verify 3 matches
        using var doc = JsonDocument.Parse(first);
        Assert.Equal(3, doc.RootElement.GetArrayLength());
    }

    #endregion

    #region CountLines Determinism

    [Fact]
    public async Task CountLines_TenCalls_IdenticalResults()
    {
        var content = string.Join("\n", Enumerable.Range(1, 47).Select(i => $"Line number {i}"));
        var relativePath = CreateFixedTestFile("lines_test.txt", content);
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await CountLinesTool.CountLines(
                _contentAnalysisService, relativePath, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));
        Assert.Equal("47", first);
    }

    #endregion

    #region GetChunkInfo Determinism - Critical for Chunk Traversal

    [Fact]
    public async Task GetChunkInfo_TenCalls_IdenticalBoundaries()
    {
        var content = string.Join("\n", Enumerable.Range(1, 103).Select(i => $"Code line {i}"));
        var relativePath = CreateFixedTestFile("chunk_test.txt", content);
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await GetChunkInfoTool.GetChunkInfo(
                _chunkingService, relativePath, 25, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));

        // Verify structure
        using var doc = JsonDocument.Parse(first);
        Assert.Equal(103, doc.RootElement.GetProperty("totalLines").GetInt32());
        Assert.Equal(5, doc.RootElement.GetProperty("chunkCount").GetInt32()); // 103/25 = 5 chunks
    }

    [Fact]
    public async Task GetChunkInfo_ChunkBoundaries_MathematicallyDeterministic()
    {
        var content = string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Line {i}"));
        var relativePath = CreateFixedTestFile("boundaries_test.txt", content);
        
        // Run multiple times
        var allBoundaries = new List<List<(int, int)>>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await _chunkingService.GetChunkInfoAsync(relativePath, 15, CancellationToken.None);
            Assert.True(result.IsSuccess);
            
            var boundaries = result.Value.ChunkBoundaries.Select(b => (b.StartLine, b.EndLine)).ToList();
            allBoundaries.Add(boundaries);
        }

        // All boundary sets must be identical
        var firstBoundaries = allBoundaries[0];
        foreach (var boundaries in allBoundaries.Skip(1))
        {
            Assert.Equal(firstBoundaries.Count, boundaries.Count);
            for (int j = 0; j < firstBoundaries.Count; j++)
            {
                Assert.Equal(firstBoundaries[j], boundaries[j]);
            }
        }

        // Verify expected boundaries: (1-15), (16-30), (31-45), (46-50)
        Assert.Equal(4, firstBoundaries.Count);
        Assert.Equal((1, 15), firstBoundaries[0]);
        Assert.Equal((16, 30), firstBoundaries[1]);
        Assert.Equal((31, 45), firstBoundaries[2]);
        Assert.Equal((46, 50), firstBoundaries[3]);
    }

    #endregion

    #region ReadChunkByIndex Determinism

    [Fact]
    public async Task ReadChunkByIndex_TenCalls_IdenticalContent()
    {
        var content = string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Content line {i}"));
        var relativePath = CreateFixedTestFile("read_chunk_test.txt", content);
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await ReadChunkByIndexTool.ReadChunkByIndex(
                _chunkingService, relativePath, 2, 15, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));

        // Verify chunk 2 (0-indexed) with size 15 = lines 31-45
        using var doc = JsonDocument.Parse(first);
        Assert.Equal(2, doc.RootElement.GetProperty("chunkIndex").GetInt32());
        Assert.Equal(31, doc.RootElement.GetProperty("startLine").GetInt32());
        Assert.Equal(45, doc.RootElement.GetProperty("endLine").GetInt32());
    }

    #endregion

    #region CountFiles Determinism

    [Fact]
    public async Task CountFiles_TenCalls_IdenticalResults()
    {
        // Create fixed set of files
        CreateFixedTestFile("file1.cs", "content");
        CreateFixedTestFile("file2.cs", "content");
        CreateFixedTestFile("file3.cs", "content");
        CreateFixedTestFile("file4.txt", "content");
        CreateFixedTestFile("sub/file5.cs", "content");
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await CountFilesTool.CountFiles(
                _aggregationService, ".", "*.cs", true, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));
        Assert.Equal("4", first); // 4 .cs files
    }

    #endregion

    #region AggregateMatches Determinism

    [Fact]
    public async Task AggregateMatches_TenCalls_IdenticalResults()
    {
        CreateFixedTestFile("subdir/code1.cs", "class Alpha { }\nclass Beta { }");
        CreateFixedTestFile("subdir/code2.cs", "class Gamma { }\nclass Delta { }\nclass Epsilon { }");
        CreateFixedTestFile("subdir/code3.cs", "interface IFoo { }");
        
        var results = new List<string>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await AggregateMatchesTool.AggregateMatches(
                _aggregationService, ".", "*.cs", @"class\s+\w+", 100, CancellationToken.None);
            results.Add(result);
        }

        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));

        using var doc = JsonDocument.Parse(first);
        Assert.Equal(5, doc.RootElement.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task AggregateMatches_MatchesByFile_DeterministicOrder()
    {
        CreateFixedTestFile("subdir2/a.txt", "match match");
        CreateFixedTestFile("subdir2/b.txt", "match");
        CreateFixedTestFile("subdir2/c.txt", "match match match");
        
        var allMatchesByFile = new List<string[]>();
        
        for (int i = 0; i < RepeatCount; i++)
        {
            var result = await _aggregationService.AggregateMatchesAsync(
                ".", "*.txt", "match", 100, CancellationToken.None);
            Assert.True(result.IsSuccess);
            
            var files = result.Value.MatchesByFile.Select(m => m.Path).ToArray();
            allMatchesByFile.Add(files);
        }

        // All file orderings must be the same
        var firstOrder = allMatchesByFile[0];
        foreach (var order in allMatchesByFile.Skip(1))
        {
            Assert.Equal(firstOrder.Length, order.Length);
            for (int j = 0; j < firstOrder.Length; j++)
            {
                Assert.Equal(firstOrder[j], order[j]);
            }
        }
    }

    #endregion

    #region Cross-Tool Consistency

    [Fact]
    public async Task ChunkInfo_And_ReadChunks_Consistent()
    {
        var content = string.Join("\n", Enumerable.Range(1, 73).Select(i => $"Line {i}"));
        var relativePath = CreateFixedTestFile("consistency_test.txt", content);
        
        // Get chunk info
        var infoResult = await _chunkingService.GetChunkInfoAsync(relativePath, 20, CancellationToken.None);
        Assert.True(infoResult.IsSuccess);
        
        // Read all chunks and verify they match boundaries
        for (int i = 0; i < infoResult.Value.ChunkCount; i++)
        {
            var chunkResult = await _chunkingService.ReadChunkAsync(relativePath, i, 20, CancellationToken.None);
            Assert.True(chunkResult.IsSuccess);
            
            var expectedBoundary = infoResult.Value.ChunkBoundaries[i];
            Assert.Equal(expectedBoundary.StartLine, chunkResult.Value.StartLine);
            Assert.Equal(expectedBoundary.EndLine, chunkResult.Value.EndLine);
        }

        // Run the same verification again to ensure consistency
        var infoResult2 = await _chunkingService.GetChunkInfoAsync(relativePath, 20, CancellationToken.None);
        Assert.Equal(infoResult.Value.ChunkCount, infoResult2.Value.ChunkCount);
        Assert.Equal(infoResult.Value.TotalLines, infoResult2.Value.TotalLines);
    }

    #endregion
}
