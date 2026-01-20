using System.Collections.Immutable;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class MultiPatternSearchTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AggregationService _service;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrailService;
    private readonly PatternMatchingService _patternMatchingService;

    public MultiPatternSearchTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"multipattern_test_{Guid.NewGuid():N}");
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

    #region Single Pattern Tests

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_SinglePattern_ReturnsCorrectResults()
    {
        CreateTestFile("data/file1.txt", "BERT is a transformer model");
        CreateTestFile("data/file2.txt", "GPT uses transformers too");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FilesSearched);
        Assert.Single(result.Value.PatternResults);
        Assert.Equal(1, result.Value.PatternResults[0].MatchCount);
        Assert.Single(result.Value.MatchingFiles);
    }

    #endregion

    #region Union Mode Tests

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_UnionMode_ReturnsFilesMatchingAnyPattern()
    {
        CreateTestFile("data/bert.txt", "BERT model");
        CreateTestFile("data/gpt.txt", "GPT model");
        CreateTestFile("data/other.txt", "Other content");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT", "GPT" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.FilesSearched);
        Assert.Equal(2, result.Value.PatternResults.Length);
        Assert.Equal(2, result.Value.MatchingFiles.Length); // bert.txt and gpt.txt
    }

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_UnionMode_CountsPerPatternCorrectly()
    {
        CreateTestFile("data/file1.txt", "BERT BERT BERT"); // 3 matches for BERT
        CreateTestFile("data/file2.txt", "GPT GPT"); // 2 matches for GPT

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT", "GPT" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var bertResult = result.Value.PatternResults.First(p => p.Pattern == "BERT");
        var gptResult = result.Value.PatternResults.First(p => p.Pattern == "GPT");
        Assert.Equal(3, bertResult.MatchCount);
        Assert.Equal(2, gptResult.MatchCount);
    }

    #endregion

    #region Intersection Mode Tests

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_IntersectionMode_ReturnsFilesMatchingAllPatterns()
    {
        CreateTestFile("data/both.txt", "BERT and GPT together");
        CreateTestFile("data/bert_only.txt", "BERT only");
        CreateTestFile("data/gpt_only.txt", "GPT only");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT", "GPT" }, "intersection", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.MatchingFiles); // Only both.txt
        Assert.Contains("data/both.txt", result.Value.MatchingFiles[0]);
    }

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_IntersectionMode_NoCommonFiles_ReturnsEmpty()
    {
        CreateTestFile("data/bert.txt", "BERT only");
        CreateTestFile("data/gpt.txt", "GPT only");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT", "GPT" }, "intersection", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.MatchingFiles);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_InvalidRegex_ReturnsFailure()
    {
        CreateTestFile("data/file.txt", "content");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "[invalid" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid regex", result.Error);
    }

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_InvalidCombineMode_ReturnsFailure()
    {
        CreateTestFile("data/file.txt", "content");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "test" }, "invalid_mode", 100, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid combineMode", result.Error);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_EmptyDirectory_ReturnsZeroResults()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "empty"));

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "empty", "*.txt", new[] { "test" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.FilesSearched);
        Assert.Empty(result.Value.MatchingFiles);
    }

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_NoMatches_ReturnsEmptyResults()
    {
        CreateTestFile("data/file.txt", "no matching content here");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "NONEXISTENT" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FilesSearched);
        Assert.Empty(result.Value.MatchingFiles);
    }

    [Fact]
    public async Task AggregateMultiPatternMatchesAsync_FileBreakdown_ShowsWhichPatternsMatchedEachFile()
    {
        CreateTestFile("data/both.txt", "BERT and GPT");
        CreateTestFile("data/bert.txt", "BERT only");

        var result = await _service.AggregateMultiPatternMatchesAsync(
            "data", "*.txt", new[] { "BERT", "GPT" }, "union", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FileBreakdown.Length);

        var bothFile = result.Value.FileBreakdown.First(f => f.Path.Contains("both"));
        Assert.Equal(2, bothFile.MatchedPatternIndices.Length); // Both patterns matched

        var bertFile = result.Value.FileBreakdown.First(f => f.Path.Contains("bert"));
        Assert.Single(bertFile.MatchedPatternIndices); // Only BERT matched
        Assert.Equal(0, bertFile.MatchedPatternIndices[0]); // Index 0 = BERT
    }

    #endregion
}
