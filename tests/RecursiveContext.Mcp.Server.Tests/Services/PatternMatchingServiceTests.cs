using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class PatternMatchingServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;
    private readonly PatternMatchingService _service;

    public PatternMatchingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pattern_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrails = new GuardrailService(settings);
        _service = new PatternMatchingService(_pathResolver, _guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task FindFilesAsync_StarPattern_MatchesAllFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "file3.cs"), "c");

        var result = await _service.FindFilesAsync("*", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalMatches);
    }

    [Fact]
    public async Task FindFilesAsync_ExtensionPattern_MatchesOnlyMatchingExtension()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "file3.cs"), "c");

        var result = await _service.FindFilesAsync("*.txt", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalMatches);
        Assert.All(result.Value.MatchingPaths, p => Assert.EndsWith(".txt", p));
    }

    [Fact]
    public async Task FindFilesAsync_DoubleStarPattern_MatchesNestedFiles()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sub", "nested"));
        File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "sub", "middle.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "sub", "nested", "deep.txt"), "c");

        var result = await _service.FindFilesAsync("**/*.txt", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TotalMatches >= 2); // At least the nested ones
    }

    [Fact]
    public async Task FindFilesAsync_QuestionMarkPattern_MatchesSingleCharacter()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "file10.txt"), "c");

        var result = await _service.FindFilesAsync("file?.txt", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalMatches);
    }

    [Fact]
    public async Task FindFilesAsync_ExactFileName_MatchesSingleFile()
    {
        File.WriteAllText(Path.Combine(_tempDir, "target.txt"), "found");
        File.WriteAllText(Path.Combine(_tempDir, "other.txt"), "not this");

        var result = await _service.FindFilesAsync("target.txt", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalMatches);
        Assert.Equal("target.txt", result.Value.MatchingPaths[0]);
    }

    [Fact]
    public async Task FindFilesAsync_NoMatches_ReturnsEmptyList()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "a");

        var result = await _service.FindFilesAsync("*.xyz", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalMatches);
        Assert.Empty(result.Value.MatchingPaths);
    }

    [Fact]
    public async Task FindFilesAsync_EmptyPattern_ReturnsFailure()
    {
        var result = await _service.FindFilesAsync("", 100, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.Error);
    }

    [Fact]
    public async Task FindFilesAsync_WhitespacePattern_ReturnsFailure()
    {
        var result = await _service.FindFilesAsync("   ", 100, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task FindFilesAsync_MaxResultsLimit_TruncatesResults()
    {
        for (int i = 0; i < 10; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.txt"), "content");

        var result = await _service.FindFilesAsync("*.txt", 3, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.MatchingPaths.Length);
    }

    [Fact]
    public async Task FindFilesAsync_ReturnsRelativePaths()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));
        File.WriteAllText(Path.Combine(_tempDir, "subdir", "file.txt"), "a");

        var result = await _service.FindFilesAsync("**/*", 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value.MatchingPaths, p =>
        {
            Assert.False(Path.IsPathRooted(p));
            Assert.DoesNotContain(_tempDir, p);
        });
    }

    [Fact]
    public async Task FindFilesAsync_PatternIncluded_InResult()
    {
        var pattern = "*.txt";
        var result = await _service.FindFilesAsync(pattern, 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(pattern, result.Value.Pattern);
    }
}
