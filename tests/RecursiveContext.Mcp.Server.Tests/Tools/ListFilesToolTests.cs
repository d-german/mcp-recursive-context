using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.FileSystem;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for list_files MCP tool.
/// Tests the full stack from tool → service → file system.
/// </summary>
public class ListFilesToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IFileSystemService _fileSystemService;

    public ListFilesToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"listfiles_tool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20);
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        _fileSystemService = new FileSystemService(pathResolver, guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ListFiles_WithFiles_ReturnsJsonWithFileInfo()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "Hello World");
        File.WriteAllText(Path.Combine(_tempDir, "data.json"), "{}");

        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, ".", 0, 100, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("error", result.ToLowerInvariant());
        
        using var doc = JsonDocument.Parse(result);
        var files = doc.RootElement.GetProperty("files");
        Assert.Equal(2, files.GetArrayLength());
    }

    [Fact]
    public async Task ListFiles_EmptyDirectory_ReturnsEmptyArray()
    {
        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, ".", 0, 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var files = doc.RootElement.GetProperty("files");
        Assert.Equal(0, files.GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task ListFiles_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.txt"), "content");

        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, ".", 1, 2, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var files = doc.RootElement.GetProperty("files");
        Assert.Equal(2, files.GetArrayLength());
        Assert.Equal(5, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("skip").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task ListFiles_NonexistentPath_ReturnsError()
    {
        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, "nonexistent", 0, 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ListFiles_PathTraversal_ReturnsError()
    {
        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, "../../../", 0, 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("outside workspace", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task ListFiles_IncludesFileMetadata()
    {
        // Arrange
        var content = "Test content with some bytes";
        File.WriteAllText(Path.Combine(_tempDir, "metadata.txt"), content);

        // Act
        var result = await ListFilesTool.ListFiles(_fileSystemService, ".", 0, 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var file = doc.RootElement.GetProperty("files")[0];
        
        Assert.True(file.TryGetProperty("name", out var name));
        Assert.Equal("metadata.txt", name.GetString());
        
        Assert.True(file.TryGetProperty("sizeBytes", out var size));
        Assert.True(size.GetInt64() > 0);
    }
}
