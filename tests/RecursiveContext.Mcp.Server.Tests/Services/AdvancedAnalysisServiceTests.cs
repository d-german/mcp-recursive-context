using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class AdvancedAnalysisServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AdvancedAnalysisService _service;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrailService;

    public AdvancedAnalysisServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"advanced_analysis_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrailService = new GuardrailService(settings);
        _service = new AdvancedAnalysisService(_pathResolver, _guardrailService);
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
        return name;
    }

    #region CountCompoundPatternAsync Tests

    [Fact]
    public async Task CountCompoundPatternAsync_AllMode_MatchesOnlyLinesWithAllPatterns()
    {
        var content = "HAMLET. To be or not to be?\nHORATIO. Indeed my lord.\nHAMLET. What sayeth you?";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.CountCompoundPatternAsync(
            relativePath, new[] { "^HAMLET", @"\?$" }, "all", false, 5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MatchingLineCount); // Lines 1 and 3 match both patterns
    }

    [Fact]
    public async Task CountCompoundPatternAsync_AnyMode_MatchesLinesWithAnyPattern()
    {
        var content = "HAMLET. Hello\nSomething else\nWorld?";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.CountCompoundPatternAsync(
            relativePath, new[] { "^HAMLET", @"\?$" }, "any", false, 5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MatchingLineCount); // Lines 1 and 3
    }

    [Fact]
    public async Task CountCompoundPatternAsync_SequenceMode_MatchesPatternsInOrder()
    {
        // Sequence mode: patterns must appear in order (abc then def, with def after abc)
        var content = "abc def ghi\ndef abc ghi\nghi only";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.CountCompoundPatternAsync(
            relativePath, new[] { "abc", "def" }, "sequence", false, 5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MatchingLineCount); // Only line 1 has abc followed by def
    }

    [Fact]
    public async Task CountCompoundPatternAsync_IncludeSamples_ReturnsSamples()
    {
        var content = "match1\nmatch2\nmatch3";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.CountCompoundPatternAsync(
            relativePath, new[] { "match" }, "all", true, 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Samples.Length);
    }

    [Fact]
    public async Task CountCompoundPatternAsync_InvalidPattern_ReturnsFailure()
    {
        var relativePath = CreateTestFile("test.txt", "content");

        var result = await _service.CountCompoundPatternAsync(
            relativePath, new[] { "[invalid" }, "all", false, 5, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid regex", result.Error);
    }

    #endregion

    #region FindConsecutiveRunsAsync Tests

    [Fact]
    public async Task FindConsecutiveRunsAsync_FindsSingleRun()
    {
        var content = "no match\nHAMLET. Line 1\nHAMLET. Line 2\nHAMLET. Line 3\nno match";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.FindConsecutiveRunsAsync(
            relativePath, "^HAMLET", 2, true, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.LongestRun);
        Assert.Equal(3, result.Value.LongestRun.Length);
        Assert.Equal(2, result.Value.LongestRun.StartLine);
    }

    [Fact]
    public async Task FindConsecutiveRunsAsync_FindsMultipleRuns()
    {
        var content = "A\nA\nB\nA\nA\nA";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.FindConsecutiveRunsAsync(
            relativePath, "A", 2, false, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalRunsFound); // Run of 2 and run of 3
    }

    [Fact]
    public async Task FindConsecutiveRunsAsync_MinRunLength_FiltersShortRuns()
    {
        var content = "A\nB\nA\nA\nB\nA\nA\nA";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.FindConsecutiveRunsAsync(
            relativePath, "A", 3, false, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRunsFound); // Only the run of 3
    }

    [Fact]
    public async Task FindConsecutiveRunsAsync_ReturnLongestOnly_ReturnsOnlyLongest()
    {
        var content = "A\nA\nB\nA\nA\nA\nB";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.FindConsecutiveRunsAsync(
            relativePath, "A", 2, true, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllRuns);
        Assert.Equal(3, result.Value.AllRuns[0].Length);
    }

    #endregion

    #region AggregatePatternMatchesAsync Tests

    [Fact]
    public async Task AggregatePatternMatchesAsync_GroupByFullMatch_CountsCorrectly()
    {
        var content = "apple\nbanana\napple\ncherry\napple\nbanana";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.AggregatePatternMatchesAsync(
            relativePath, @"\w+", "fullMatch", 10, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Groups[0].Count); // apple is most common
        Assert.Equal("apple", result.Value.Groups[0].Key);
    }

    [Fact]
    public async Task AggregatePatternMatchesAsync_GroupByCaptureGroup1_ExtractsGroup()
    {
        var content = "name:John\nname:Jane\nname:John\nname:Bob";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.AggregatePatternMatchesAsync(
            relativePath, @"name:(\w+)", "captureGroup1", 10, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Groups[0].Count); // John appears twice
        Assert.Equal("John", result.Value.Groups[0].Key);
    }

    [Fact]
    public async Task AggregatePatternMatchesAsync_TopN_LimitsResults()
    {
        var content = "a\nb\nc\nd\ne";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.AggregatePatternMatchesAsync(
            relativePath, @"\w", "fullMatch", 2, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Groups.Length);
        Assert.Equal(5, result.Value.UniqueGroups);
    }

    [Fact]
    public async Task AggregatePatternMatchesAsync_IncludeSamples_ReturnsSamples()
    {
        var content = "test1\ntest2";
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.AggregatePatternMatchesAsync(
            relativePath, @"test\d", "fullMatch", 10, true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Groups[0].Sample);
    }

    #endregion

    #region SampleMatchesDistributedAsync Tests

    [Fact]
    public async Task SampleMatchesDistributedAsync_EvenDistribution_SpreadsSamples()
    {
        var lines = Enumerable.Range(1, 100).Select(i => $"match{i}").ToArray();
        var content = string.Join("\n", lines);
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SampleMatchesDistributedAsync(
            relativePath, "match", 5, "even", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Samples.Length);
        // Check samples are spread across the file
        Assert.True(result.Value.Samples[0].LineNumber < 30);
        Assert.True(result.Value.Samples[^1].LineNumber > 70);
    }

    [Fact]
    public async Task SampleMatchesDistributedAsync_FirstDistribution_ReturnsFirstMatches()
    {
        var lines = Enumerable.Range(1, 100).Select(i => $"match{i}").ToArray();
        var content = string.Join("\n", lines);
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SampleMatchesDistributedAsync(
            relativePath, "match", 5, "first", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Samples.Length);
        Assert.Equal(1, result.Value.Samples[0].LineNumber);
        Assert.Equal(5, result.Value.Samples[^1].LineNumber);
    }

    [Fact]
    public async Task SampleMatchesDistributedAsync_LastDistribution_ReturnsLastMatches()
    {
        var lines = Enumerable.Range(1, 100).Select(i => $"match{i}").ToArray();
        var content = string.Join("\n", lines);
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SampleMatchesDistributedAsync(
            relativePath, "match", 5, "last", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Samples.Length);
        Assert.Equal(96, result.Value.Samples[0].LineNumber);
        Assert.Equal(100, result.Value.Samples[^1].LineNumber);
    }

    [Fact]
    public async Task SampleMatchesDistributedAsync_RandomDistribution_ReturnsSamples()
    {
        var lines = Enumerable.Range(1, 100).Select(i => $"match{i}").ToArray();
        var content = string.Join("\n", lines);
        var relativePath = CreateTestFile("test.txt", content);

        var result = await _service.SampleMatchesDistributedAsync(
            relativePath, "match", 5, "random", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Samples.Length);
    }

    #endregion

    #region ComparePatternAcrossFilesAsync Tests

    [Fact]
    public async Task ComparePatternAcrossFilesAsync_ComparesMultipleFiles()
    {
        var file1 = CreateTestFile("file1.txt", "love\nlove\nlove");
        var file2 = CreateTestFile("file2.txt", "love");

        var result = await _service.ComparePatternAcrossFilesAsync(
            new[] { file1, file2 }, "love", false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Files.Length);
        Assert.Equal(3, result.Value.Files[0].Count);
        Assert.Equal(1, result.Value.Files[1].Count);
    }

    [Fact]
    public async Task ComparePatternAcrossFilesAsync_ComputeRatio_CalculatesRatios()
    {
        var file1 = CreateTestFile("file1.txt", "word\nword\nword\nword"); // 4 matches
        var file2 = CreateTestFile("file2.txt", "word\nword"); // 2 matches, avg = 3

        var result = await _service.ComparePatternAcrossFilesAsync(
            new[] { file1, file2 }, "word", true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Files[0].Ratio);
        Assert.NotNull(result.Value.Files[1].Ratio);
    }

    [Fact]
    public async Task ComparePatternAcrossFilesAsync_MissingFile_ReturnsNegativeCount()
    {
        var file1 = CreateTestFile("file1.txt", "match");

        var result = await _service.ComparePatternAcrossFilesAsync(
            new[] { file1, "nonexistent.txt" }, "match", false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Files[0].Count);
        Assert.Equal(-1, result.Value.Files[1].Count); // Missing file
    }

    [Fact]
    public async Task ComparePatternAcrossFilesAsync_GeneratesSummary()
    {
        var file1 = CreateTestFile("file1.txt", "a\na\na");
        var file2 = CreateTestFile("file2.txt", "a");

        var result = await _service.ComparePatternAcrossFilesAsync(
            new[] { file1, file2 }, "a", false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Value.ComparisonSummary));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AllMethods_NonexistentFile_ReturnFailure()
    {
        var result1 = await _service.CountCompoundPatternAsync("nope.txt", new[] { "a" }, "all", false, 5, CancellationToken.None);
        var result2 = await _service.FindConsecutiveRunsAsync("nope.txt", "a", 2, true, 10, CancellationToken.None);
        var result3 = await _service.AggregatePatternMatchesAsync("nope.txt", "a", "fullMatch", 10, false, CancellationToken.None);
        var result4 = await _service.SampleMatchesDistributedAsync("nope.txt", "a", 5, "even", CancellationToken.None);

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.True(result3.IsFailure);
        Assert.True(result4.IsFailure);
    }

    #endregion
}
