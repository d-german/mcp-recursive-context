using System.Collections.Immutable;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class AggregationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AggregationService _service;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrailService;
    private readonly PatternMatchingService _patternMatchingService;

    public AggregationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aggregation_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrailService = new GuardrailService(settings);
        _patternMatchingService = new PatternMatchingService(_pathResolver, _guardrailService);
        _service = new AggregationService(_pathResolver, _guardrailService, _patternMatchingService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void CreateTestFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }

    private void CreateSubdirectory(string relativePath)
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, relativePath));
    }

    #region CountFilesAsync Tests

    [Fact]
    public async Task CountFilesAsync_EmptyDirectory_ReturnsZero()
    {
        CreateSubdirectory("empty");

        var result = await _service.CountFilesAsync("empty", "*.txt", recursive: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task CountFilesAsync_MatchingFiles_ReturnsCorrectCount()
    {
        CreateTestFile("file1.txt", "content1");
        CreateTestFile("file2.txt", "content2");
        CreateTestFile("file3.txt", "content3");
        CreateTestFile("file4.cs", "content4"); // Different extension

        var result = await _service.CountFilesAsync(".", "*.txt", recursive: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task CountFilesAsync_NonRecursive_CountsOnlyTopLevel()
    {
        CreateTestFile("file1.txt", "content1");
        CreateTestFile("subdir/file2.txt", "content2");
        CreateTestFile("subdir/nested/file3.txt", "content3");

        var result = await _service.CountFilesAsync(".", "*.txt", recursive: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task CountFilesAsync_Recursive_CountsAllLevels()
    {
        CreateTestFile("file1.txt", "content1");
        CreateTestFile("subdir/file2.txt", "content2");
        CreateTestFile("subdir/nested/file3.txt", "content3");

        var result = await _service.CountFilesAsync(".", "*.txt", recursive: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task CountFilesAsync_SpecificSubdirectory_CountsCorrectly()
    {
        CreateTestFile("root.txt", "content");
        CreateTestFile("subdir/file1.txt", "content1");
        CreateTestFile("subdir/file2.txt", "content2");
        CreateTestFile("other/file3.txt", "content3");

        var result = await _service.CountFilesAsync("subdir", "*.txt", recursive: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task CountFilesAsync_NonexistentDirectory_ReturnsFailure()
    {
        var result = await _service.CountFilesAsync("nonexistent", "*.txt", recursive: true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task CountFilesAsync_MultipleExtensions_SupportsWildcard()
    {
        CreateTestFile("file1.cs", "content");
        CreateTestFile("file2.csx", "content");
        CreateTestFile("file3.txt", "content");

        var result = await _service.CountFilesAsync(".", "*.cs*", recursive: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
    }

    #endregion

    #region AggregateMatchesAsync Tests

    [Fact]
    public async Task AggregateMatchesAsync_NoMatchingFiles_ReturnsZero()
    {
        CreateTestFile("file1.cs", "no matches here");

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", "pattern", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.FilesSearched);
        Assert.Equal(0, result.Value.TotalMatches);
    }

    [Fact]
    public async Task AggregateMatchesAsync_SingleFile_ReturnsCorrectCount()
    {
        // Create file in subdirectory since **/*.txt pattern requires directory
        CreateTestFile("subdir/test.txt", "class Foo { }\nclass Bar { }\nclass Baz { }");

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", @"class\s+\w+", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FilesSearched);
        Assert.Equal(3, result.Value.TotalMatches);
    }

    [Fact]
    public async Task AggregateMatchesAsync_MultipleFiles_AggregatesCorrectly()
    {
        // Files in subdirectory
        CreateTestFile("subdir/file1.txt", "match1 match2");
        CreateTestFile("subdir/file2.txt", "match3");
        CreateTestFile("subdir/file3.txt", "match4 match5 match6");

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", @"match\d", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.FilesSearched);
        Assert.Equal(6, result.Value.TotalMatches);
    }

    [Fact]
    public async Task AggregateMatchesAsync_TotalEqualsSumOfPerFile()
    {
        CreateTestFile("subdir/a.txt", "pattern pattern");       // 2
        CreateTestFile("subdir/b.txt", "pattern");                // 1
        CreateTestFile("subdir/c.txt", "pattern pattern pattern"); // 3
        CreateTestFile("subdir/d.txt", "no match here");          // 0

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", "pattern", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // Verify sum equals total
        var sumOfPerFile = result.Value.MatchesByFile.Sum(f => f.Count);
        Assert.Equal(result.Value.TotalMatches, sumOfPerFile);
        Assert.Equal(6, result.Value.TotalMatches);
    }

    [Fact]
    public async Task AggregateMatchesAsync_MatchesByFile_ContainsCorrectDetails()
    {
        CreateTestFile("subdir/file1.txt", "pattern pattern");
        CreateTestFile("subdir/file2.txt", "other content");
        CreateTestFile("subdir/file3.txt", "pattern");

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", "pattern", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MatchesByFile.Length); // Only files with matches

        var file1Match = result.Value.MatchesByFile.FirstOrDefault(f => f.Path.Contains("file1.txt"));
        Assert.NotNull(file1Match);
        Assert.Equal(2, file1Match.Count);

        var file3Match = result.Value.MatchesByFile.FirstOrDefault(f => f.Path.Contains("file3.txt"));
        Assert.NotNull(file3Match);
        Assert.Equal(1, file3Match.Count);
    }

    [Fact]
    public async Task AggregateMatchesAsync_InvalidRegex_ReturnsFailure()
    {
        CreateTestFile("subdir/test.txt", "content");

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", "[invalid", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid regex", result.Error);
    }

    [Fact]
    public async Task AggregateMatchesAsync_SubdirectorySearch_Works()
    {
        // root/other.txt should NOT be matched when searching in subdir
        CreateTestFile("root/other.txt", "pattern");
        // Files in subdir/nested/ should be matched (due to **/*.txt pattern)
        CreateTestFile("subdir/nested/file.txt", "pattern pattern");

        var result = await _service.AggregateMatchesAsync(
            "subdir", "*.txt", "pattern", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FilesSearched);
        Assert.Equal(2, result.Value.TotalMatches);
    }

    [Fact]
    public async Task AggregateMatchesAsync_MaxFilesLimit_Respected()
    {
        // Create many files in subdirectory
        for (int i = 0; i < 10; i++)
        {
            CreateTestFile($"subdir/file{i}.txt", "pattern");
        }

        var result = await _service.AggregateMatchesAsync(
            ".", "*.txt", "pattern", maxFiles: 3, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Should process at most 3 files
        Assert.True(result.Value.FilesSearched <= 3);
    }

    #endregion

    #region Determinism Tests - Critical

    [Fact]
    public async Task CountFilesAsync_MultipleCalls_ReturnsSameResult()
    {
        CreateTestFile("a.txt", "content");
        CreateTestFile("b.txt", "content");
        CreateTestFile("sub/c.txt", "content");

        var result1 = await _service.CountFilesAsync(".", "*.txt", true, CancellationToken.None);
        var result2 = await _service.CountFilesAsync(".", "*.txt", true, CancellationToken.None);
        var result3 = await _service.CountFilesAsync(".", "*.txt", true, CancellationToken.None);

        Assert.Equal(result1.Value, result2.Value);
        Assert.Equal(result2.Value, result3.Value);
        Assert.Equal(3, result1.Value);
    }

    [Fact]
    public async Task AggregateMatchesAsync_MultipleCalls_ReturnsSameResult()
    {
        CreateTestFile("file1.txt", "class A\nclass B");
        CreateTestFile("file2.txt", "class C");

        var result1 = await _service.AggregateMatchesAsync(".", "*.txt", @"class\s+\w", 100, CancellationToken.None);
        var result2 = await _service.AggregateMatchesAsync(".", "*.txt", @"class\s+\w", 100, CancellationToken.None);
        var result3 = await _service.AggregateMatchesAsync(".", "*.txt", @"class\s+\w", 100, CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);

        Assert.Equal(result1.Value.TotalMatches, result2.Value.TotalMatches);
        Assert.Equal(result2.Value.TotalMatches, result3.Value.TotalMatches);

        Assert.Equal(result1.Value.FilesSearched, result2.Value.FilesSearched);
        Assert.Equal(result2.Value.FilesSearched, result3.Value.FilesSearched);
    }

    #endregion

    #region GuardrailService Limit Tests

    [Fact]
    public async Task AggregateMatchesAsync_ExceedsMaxFilesPerAggregation_Truncates()
    {
        // Create service with low MaxFilesPerAggregation limit
        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 5, 10_000, 500); // MaxFilesPerAggregation = 5
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        var patternService = new PatternMatchingService(pathResolver, guardrails);
        var service = new AggregationService(pathResolver, guardrails, patternService);

        // Create more files than the limit
        for (int i = 0; i < 10; i++)
        {
            CreateTestFile($"file{i}.txt", "pattern");
        }

        var result = await service.AggregateMatchesAsync(
            ".", "*.txt", "pattern", maxFiles: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Should be limited to 5 files
        Assert.True(result.Value.FilesSearched <= 5);
    }

    #endregion
}
