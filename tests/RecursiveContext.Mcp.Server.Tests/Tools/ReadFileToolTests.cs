using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.FileSystem;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for read_file MCP tool.
/// Tests the full stack from tool → service → file system.
/// </summary>
public class ReadFileToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IFileSystemService _fileSystemService;
    private readonly IFileSystemService _smallLimitService;

    public ReadFileToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"readfile_tool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        _fileSystemService = new FileSystemService(pathResolver, guardrails);

        // Service with very small byte limit for testing guardrails
        var smallSettings = new RlmSettings(_tempDir, 50, 100, 30, 20, 500, 10_000, 500);
        var smallGuardrails = new GuardrailService(smallSettings);
        _smallLimitService = new FileSystemService(pathResolver, smallGuardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// Helper to deserialize the JSON-serialized string returned by the tool.
    /// Tool returns JSON-encoded string (e.g., "\"content\"" for content).
    /// </summary>
    private static string DeserializeContent(string jsonResult)
    {
        return JsonSerializer.Deserialize<string>(jsonResult) ?? string.Empty;
    }

    [Fact]
    public async Task ReadFile_ValidFile_ReturnsContent()
    {
        // Arrange
        var expectedContent = "Hello, World!\nThis is a test file.";
        File.WriteAllText(Path.Combine(_tempDir, "hello.txt"), expectedContent);

        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "hello.txt", CancellationToken.None);

        // Assert - result is JSON-serialized string
        var actualContent = DeserializeContent(result);
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task ReadFile_NonexistentFile_ReturnsError()
    {
        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "missing.txt", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("does not exist", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadFile_PathTraversal_ReturnsError()
    {
        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "../../etc/passwd", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("outside workspace", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadFile_FileTooLarge_ReturnsError()
    {
        // Arrange
        var largeContent = new string('x', 100);
        File.WriteAllText(Path.Combine(_tempDir, "large.txt"), largeContent);

        // Act
        var result = await ReadFileTool.ReadFile(_smallLimitService, "large.txt", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("exceeds", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadFile_InSubdirectory_ReturnsContent()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var content = "Nested file content";
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), content);

        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "subdir/nested.txt", CancellationToken.None);

        // Assert - result is JSON-serialized string
        var actualContent = DeserializeContent(result);
        Assert.Equal(content, actualContent);
    }

    [Fact]
    public async Task ReadFile_EmptyFile_ReturnsEmptyString()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "empty.txt"), "");

        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "empty.txt", CancellationToken.None);

        // Assert - empty string serialized as JSON is "\"\""
        var actualContent = DeserializeContent(result);
        Assert.Equal("", actualContent);
    }

    [Fact]
    public async Task ReadFile_JsonFile_ReturnsJsonAsString()
    {
        // Arrange
        var jsonContent = """{"name": "test", "value": 42}""";
        File.WriteAllText(Path.Combine(_tempDir, "data.json"), jsonContent);

        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "data.json", CancellationToken.None);

        // Assert - JSON file content is returned as a serialized string, not as parsed JSON
        var actualContent = DeserializeContent(result);
        Assert.Contains("\"name\":", actualContent);
        Assert.Contains("\"test\"", actualContent);
    }

    [Fact]
    public async Task ReadFile_SpecialCharactersInContent_PreservesContent()
    {
        // Arrange
        var specialContent = "Unicode: ñ é ü\nTabs:\t\t\nQuotes: \"test\"";
        File.WriteAllText(Path.Combine(_tempDir, "special.txt"), specialContent);

        // Act
        var result = await ReadFileTool.ReadFile(_fileSystemService, "special.txt", CancellationToken.None);

        // Assert - JSON deserialization should restore special characters
        var actualContent = DeserializeContent(result);
        Assert.Equal(specialContent, actualContent);
    }
}
