using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.Search;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for find_files_by_pattern MCP tool.
/// </summary>
public class FindFilesByPatternToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IPatternMatchingService _patternService;

    public FindFilesByPatternToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pattern_tool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20);
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        _patternService = new PatternMatchingService(pathResolver, guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task FindFiles_StarPattern_ReturnsMatches()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "data.json"), "c");

        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "*.txt", 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task FindFiles_NestedFiles_ReturnsDeepMatches()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_tempDir, "src", "nested"));
        File.WriteAllText(Path.Combine(_tempDir, "root.cs"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "src", "mid.cs"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "src", "nested", "deep.cs"), "c");

        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "**.cs", 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("totalMatches").GetInt32() >= 2);
    }

    [Fact]
    public async Task FindFiles_NoMatches_ReturnsEmptyArray()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "a");

        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "*.xyz", 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetProperty("totalMatches").GetInt32());
    }

    [Fact]
    public async Task FindFiles_EmptyPattern_ReturnsError()
    {
        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "", 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task FindFiles_ReturnsPatternInResult()
    {
        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "*.cs", 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("*.cs", doc.RootElement.GetProperty("pattern").GetString());
    }

    [Fact]
    public async Task FindFiles_MaxResults_LimitsOutput()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.txt"), "content");

        // Act
        var result = await FindFilesByPatternTool.FindFilesByPattern(_patternService, "*.txt", 3, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var paths = doc.RootElement.GetProperty("matchingPaths");
        Assert.Equal(3, paths.GetArrayLength());
    }
}
