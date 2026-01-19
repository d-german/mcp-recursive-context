using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.FileSystem;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for list_directories MCP tool.
/// </summary>
public class ListDirectoriesToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IFileSystemService _fileSystemService;

    public ListDirectoriesToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"listdirs_tool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
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
    public async Task ListDirectories_WithSubdirs_ReturnsDirectoryInfo()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "tests"));
        File.WriteAllText(Path.Combine(_tempDir, "src", "file.cs"), "code");

        // Act
        var result = await ListDirectoriesTool.ListDirectories(_fileSystemService, ".", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var dirs = doc.RootElement.GetProperty("directories");
        Assert.Equal(2, dirs.GetArrayLength());
    }

    [Fact]
    public async Task ListDirectories_EmptyDir_ReturnsEmptyArray()
    {
        // Act
        var result = await ListDirectoriesTool.ListDirectories(_fileSystemService, ".", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var dirs = doc.RootElement.GetProperty("directories");
        Assert.Equal(0, dirs.GetArrayLength());
    }

    [Fact]
    public async Task ListDirectories_NonexistentPath_ReturnsError()
    {
        // Act
        var result = await ListDirectoriesTool.ListDirectories(_fileSystemService, "nonexistent", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ListDirectories_IncludesMetadata()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(subDir, "file2.txt"), "b");
        Directory.CreateDirectory(Path.Combine(subDir, "nested"));

        // Act
        var result = await ListDirectoriesTool.ListDirectories(_fileSystemService, ".", CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var dir = doc.RootElement.GetProperty("directories")[0];
        
        Assert.Equal("subdir", dir.GetProperty("name").GetString());
        Assert.Equal(2, dir.GetProperty("fileCount").GetInt32());
        Assert.Equal(1, dir.GetProperty("subdirectoryCount").GetInt32());
    }
}
