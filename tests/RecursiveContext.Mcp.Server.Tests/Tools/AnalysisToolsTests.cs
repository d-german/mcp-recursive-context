using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.Analysis;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for all 7 analysis tools.
/// </summary>
public class AnalysisToolsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrails;
    private readonly ContentAnalysisService _contentAnalysisService;
    private readonly ChunkingService _chunkingService;
    private readonly AggregationService _aggregationService;

    public AnalysisToolsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"analysis_tools_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
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

    private string CreateTestFile(string name, string content)
    {
        var filePath = Path.Combine(_tempDir, name);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content);
        return name;
    }

    #region CountPatternMatchesTool Tests

    [Fact]
    public async Task CountPatternMatches_ReturnsValidJson()
    {
        var relativePath = CreateTestFile("test.cs", "class Foo { }\nclass Bar { }");

        var result = await CountPatternMatchesTool.CountPatternMatches(
            _contentAnalysisService, relativePath, @"class\s+\w+", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task CountPatternMatches_InvalidRegex_ReturnsError()
    {
        var relativePath = CreateTestFile("test.txt", "content");

        var result = await CountPatternMatchesTool.CountPatternMatches(
            _contentAnalysisService, relativePath, "[invalid", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task CountPatternMatches_NonexistentFile_ReturnsError()
    {
        var result = await CountPatternMatchesTool.CountPatternMatches(
            _contentAnalysisService, "nonexistent.txt", "pattern", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    #endregion

    #region SearchWithContextTool Tests

    [Fact]
    public async Task SearchWithContext_ReturnsValidJson()
    {
        var relativePath = CreateTestFile("test.txt", "line1\nMATCH here\nline3");

        var result = await SearchWithContextTool.SearchWithContext(
            _contentAnalysisService, relativePath, "MATCH", 1, 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        var matches = doc.RootElement.EnumerateArray().ToList();
        Assert.Single(matches);
        Assert.Equal(2, matches[0].GetProperty("lineNumber").GetInt32());
    }

    [Fact]
    public async Task SearchWithContext_NoMatches_ReturnsEmptyArray()
    {
        var relativePath = CreateTestFile("test.txt", "no matches here");

        var result = await SearchWithContextTool.SearchWithContext(
            _contentAnalysisService, relativePath, "xyz", 1, 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Empty(doc.RootElement.EnumerateArray().ToList());
    }

    #endregion

    #region CountLinesTool Tests

    [Fact]
    public async Task CountLines_ReturnsValidJson()
    {
        var relativePath = CreateTestFile("test.txt", "line1\nline2\nline3\nline4\nline5");

        var result = await CountLinesTool.CountLines(
            _contentAnalysisService, relativePath, CancellationToken.None);

        // CountLines returns just a number (int serialized)
        Assert.Equal("5", result);
    }

    [Fact]
    public async Task CountLines_EmptyFile_ReturnsZero()
    {
        var relativePath = CreateTestFile("empty.txt", "");

        var result = await CountLinesTool.CountLines(
            _contentAnalysisService, relativePath, CancellationToken.None);

        Assert.Equal("0", result);
    }

    #endregion

    #region GetChunkInfoTool Tests

    [Fact]
    public async Task GetChunkInfo_ReturnsValidJson()
    {
        var relativePath = CreateTestFile("test.txt", string.Join("\n", Enumerable.Range(1, 25).Select(i => $"Line {i}")));

        var result = await GetChunkInfoTool.GetChunkInfo(
            _chunkingService, relativePath, 10, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(25, doc.RootElement.GetProperty("totalLines").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("chunkCount").GetInt32());
    }

    [Fact]
    public async Task GetChunkInfo_IncludesChunkBoundaries()
    {
        var relativePath = CreateTestFile("test.txt", string.Join("\n", Enumerable.Range(1, 20).Select(i => $"Line {i}")));

        var result = await GetChunkInfoTool.GetChunkInfo(
            _chunkingService, relativePath, 10, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        var boundaries = doc.RootElement.GetProperty("chunkBoundaries");
        Assert.Equal(JsonValueKind.Array, boundaries.ValueKind);
        Assert.Equal(2, boundaries.GetArrayLength());
    }

    #endregion

    #region ReadChunkByIndexTool Tests

    [Fact]
    public async Task ReadChunkByIndex_ReturnsValidJson()
    {
        var relativePath = CreateTestFile("test.txt", string.Join("\n", Enumerable.Range(1, 25).Select(i => $"Line {i}")));

        var result = await ReadChunkByIndexTool.ReadChunkByIndex(
            _chunkingService, relativePath, 0, 10, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetProperty("chunkIndex").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("startLine").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("endLine").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task ReadChunkByIndex_InvalidIndex_ReturnsError()
    {
        var relativePath = CreateTestFile("test.txt", "Line 1\nLine 2");

        var result = await ReadChunkByIndexTool.ReadChunkByIndex(
            _chunkingService, relativePath, 99, 10, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    #endregion

    #region CountFilesTool Tests

    [Fact]
    public async Task CountFiles_ReturnsValidNumber()
    {
        CreateTestFile("file1.txt", "content");
        CreateTestFile("file2.txt", "content");
        CreateTestFile("file3.cs", "content");

        var result = await CountFilesTool.CountFiles(
            _aggregationService, ".", "*.txt", true, CancellationToken.None);

        // CountFiles returns just a number (int serialized)
        Assert.Equal("2", result);
    }

    [Fact]
    public async Task CountFiles_Recursive_CountsAllLevels()
    {
        CreateTestFile("root.txt", "content");
        CreateTestFile("subdir/nested.txt", "content");

        var result = await CountFilesTool.CountFiles(
            _aggregationService, ".", "*.txt", true, CancellationToken.None);

        // CountFiles returns just a number
        Assert.Equal("2", result);
    }

    #endregion

    #region AggregateMatchesTool Tests

    [Fact]
    public async Task AggregateMatches_ReturnsValidJson()
    {
        CreateTestFile("subdir/file1.txt", "pattern pattern");
        CreateTestFile("subdir/file2.txt", "pattern");

        var result = await AggregateMatchesTool.AggregateMatches(
            _aggregationService, ".", "*.txt", "pattern", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("filesSearched").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task AggregateMatches_InvalidRegex_ReturnsError()
    {
        CreateTestFile("subdir/test.txt", "content");

        var result = await AggregateMatchesTool.AggregateMatches(
            _aggregationService, ".", "*.txt", "[invalid", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task AggregateMatches_IncludesMatchesByFile()
    {
        CreateTestFile("subdir/a.txt", "pattern pattern");
        CreateTestFile("subdir/b.txt", "other");
        CreateTestFile("subdir/c.txt", "pattern");

        var result = await AggregateMatchesTool.AggregateMatches(
            _aggregationService, ".", "*.txt", "pattern", 100, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        var matchesByFile = doc.RootElement.GetProperty("matchesByFile");
        Assert.Equal(JsonValueKind.Array, matchesByFile.ValueKind);
        Assert.Equal(2, matchesByFile.GetArrayLength()); // Only 2 files have matches
    }

    #endregion

    #region JSON Format Consistency Tests

    [Fact]
    public async Task SuccessfulObjectResults_HaveNoErrorProperty()
    {
        var relativePath = CreateTestFile("test.txt", "pattern content here");

        var countResult = await CountPatternMatchesTool.CountPatternMatches(
            _contentAnalysisService, relativePath, "pattern", 100, CancellationToken.None);

        using var countDoc = JsonDocument.Parse(countResult);
        Assert.False(countDoc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task AllErrorResults_HaveErrorProperty()
    {
        var countError = await CountPatternMatchesTool.CountPatternMatches(
            _contentAnalysisService, "nonexistent.txt", "pattern", 100, CancellationToken.None);
        var lineError = await CountLinesTool.CountLines(
            _contentAnalysisService, "nonexistent.txt", CancellationToken.None);

        using var countDoc = JsonDocument.Parse(countError);
        using var lineDoc = JsonDocument.Parse(lineError);

        Assert.True(countDoc.RootElement.TryGetProperty("error", out _));
        Assert.True(lineDoc.RootElement.TryGetProperty("error", out _));
    }

    #endregion
}
