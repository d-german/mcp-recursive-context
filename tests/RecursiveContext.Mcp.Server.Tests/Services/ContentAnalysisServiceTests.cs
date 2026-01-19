using System.Collections.Immutable;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Services.Caching;
using RecursiveContext.Mcp.Server.Services.Streaming;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class ContentAnalysisServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ContentAnalysisService _service;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrailService;
    private readonly CompiledRegexCache _regexCache;
    private readonly FileStreamingService _streamingService;

    public ContentAnalysisServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"content_analysis_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrailService = new GuardrailService(settings);
        _regexCache = new CompiledRegexCache();
        _streamingService = new FileStreamingService(_pathResolver);
        _service = new ContentAnalysisService(_pathResolver, _guardrailService, _regexCache, _streamingService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTestFile(string name, string content)
    {
        var filePath = Path.Combine(_tempDir, name);
        File.WriteAllText(filePath, content);
        return name; // Return relative path for service calls
    }

    #region CountPatternMatchesAsync Tests

    [Fact]
    public async Task CountPatternMatchesAsync_SimplePattern_ReturnsCorrectCount()
    {
        var relativePath = CreateTestFile("test.txt", "hello world hello there hello again");

        var result = await _service.CountPatternMatchesAsync(relativePath, "hello", 100, false, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_NoMatches_ReturnsZero()
    {
        var relativePath = CreateTestFile("test.txt", "hello world");

        var result = await _service.CountPatternMatchesAsync(relativePath, "goodbye", 100, false, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Count);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_RegexPattern_Works()
    {
        var relativePath = CreateTestFile("test.txt", "test1\ntest2\ntest99\ntesting");

        var result = await _service.CountPatternMatchesAsync(relativePath, @"test\d+", 100, false, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_InvalidRegex_ReturnsFailure()
    {
        var relativePath = CreateTestFile("test.txt", "content");

        var result = await _service.CountPatternMatchesAsync(relativePath, "[invalid", 100, false, true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid regex", result.Error);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.CountPatternMatchesAsync("nonexistent.txt", "pattern", 100, false, true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_CaseInsensitive_MatchesAll()
    {
        var relativePath = CreateTestFile("test.txt", "Hello\nHELLO\nhello\nHeLLo");

        var result = await _service.CountPatternMatchesAsync(relativePath, "(?i)hello", 100, false, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Count);
    }

    [Fact]
    public async Task CountPatternMatchesAsync_MaxResults_LimitsSamples()
    {
        var relativePath = CreateTestFile("test.txt", "match\nmatch\nmatch\nmatch\nmatch");

        var result = await _service.CountPatternMatchesAsync(relativePath, "match", 2, false, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.Equal(2, result.Value.SampleMatches.Length);
    }

    #endregion

    #region SearchWithContextAsync Tests

    [Fact]
    public async Task SearchWithContextAsync_ReturnsMatchesWithContext()
    {
        var content = "line1\nline2 match here\nline3\nline4 match again\nline5";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SearchWithContextAsync(relativePath, "match", contextLines: 1, maxResults: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task SearchWithContextAsync_NoMatches_ReturnsEmptyArray()
    {
        var relativePath = CreateTestFile("test.txt", "no matches here");

        var result = await _service.SearchWithContextAsync(relativePath, "xyz", contextLines: 1, maxResults: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchWithContextAsync_ContextLines_IncludesCorrectLines()
    {
        var content = "line1\nline2\nline3 MATCH\nline4\nline5";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SearchWithContextAsync(relativePath, "MATCH", contextLines: 2, maxResults: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var match = result.Value[0];
        Assert.Equal(3, match.LineNumber);
        Assert.Contains("line1", match.ContextBefore);
        Assert.Contains("line2", match.ContextBefore);
        Assert.Contains("line4", match.ContextAfter);
        Assert.Contains("line5", match.ContextAfter);
    }

    [Fact]
    public async Task SearchWithContextAsync_AtFileStart_HandlesEdge()
    {
        var content = "MATCH here\nline2\nline3";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SearchWithContextAsync(relativePath, "MATCH", contextLines: 5, maxResults: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(1, result.Value[0].LineNumber);
        Assert.Empty(result.Value[0].ContextBefore);
    }

    [Fact]
    public async Task SearchWithContextAsync_AtFileEnd_HandlesEdge()
    {
        var content = "line1\nline2\nMATCH here";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SearchWithContextAsync(relativePath, "MATCH", contextLines: 5, maxResults: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(3, result.Value[0].LineNumber);
        Assert.Empty(result.Value[0].ContextAfter);
    }

    [Fact]
    public async Task SearchWithContextAsync_MaxResults_LimitsMatches()
    {
        var content = "match1\nmatch2\nmatch3\nmatch4\nmatch5";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SearchWithContextAsync(relativePath, "match", contextLines: 0, maxResults: 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    #endregion

    #region CountLinesAsync Tests

    [Fact]
    public async Task CountLinesAsync_EmptyFile_ReturnsZero()
    {
        var relativePath = CreateTestFile("empty.txt", "");

        var result = await _service.CountLinesAsync(relativePath, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task CountLinesAsync_SingleLine_ReturnsOne()
    {
        var relativePath = CreateTestFile("single.txt", "one line");

        var result = await _service.CountLinesAsync(relativePath, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task CountLinesAsync_MultipleLines_ReturnsCorrectCount()
    {
        var relativePath = CreateTestFile("multi.txt", "line1\nline2\nline3\nline4\nline5");

        var result = await _service.CountLinesAsync(relativePath, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public async Task CountLinesAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.CountLinesAsync("nonexistent.txt", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public async Task CountPatternMatchesAsync_MultipleCalls_ReturnsSameResult()
    {
        var relativePath = CreateTestFile("test.txt", "pattern\npattern\npattern");

        var result1 = await _service.CountPatternMatchesAsync(relativePath, "pattern", 100, false, true, CancellationToken.None);
        var result2 = await _service.CountPatternMatchesAsync(relativePath, "pattern", 100, false, true, CancellationToken.None);
        var result3 = await _service.CountPatternMatchesAsync(relativePath, "pattern", 100, false, true, CancellationToken.None);

        Assert.Equal(result1.Value.Count, result2.Value.Count);
        Assert.Equal(result2.Value.Count, result3.Value.Count);
    }

    [Fact]
    public async Task SearchWithContextAsync_MultipleCalls_ReturnsSameResult()
    {
        var relativePath = CreateTestFile("test.txt", "line1\nmatch line\nline3");

        var result1 = await _service.SearchWithContextAsync(relativePath, "match", 1, 100, CancellationToken.None);
        var result2 = await _service.SearchWithContextAsync(relativePath, "match", 1, 100, CancellationToken.None);

        Assert.Equal(result1.Value.Count, result2.Value.Count);
        Assert.Equal(result1.Value[0].LineNumber, result2.Value[0].LineNumber);
    }

    [Fact]
    public async Task CountLinesAsync_MultipleCalls_ReturnsSameResult()
    {
        var relativePath = CreateTestFile("test.txt", "a\nb\nc\nd\ne");

        var result1 = await _service.CountLinesAsync(relativePath, CancellationToken.None);
        var result2 = await _service.CountLinesAsync(relativePath, CancellationToken.None);
        var result3 = await _service.CountLinesAsync(relativePath, CancellationToken.None);

        Assert.Equal(result1.Value, result2.Value);
        Assert.Equal(result2.Value, result3.Value);
    }

    #endregion
}
