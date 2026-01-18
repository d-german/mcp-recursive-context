using System.Text.Json;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools.FileSystem;

namespace RecursiveContext.Mcp.Server.Tests.Tools;

/// <summary>
/// Integration tests for read_file_chunk MCP tool.
/// Tests the full stack from tool → service → file system.
/// </summary>
public class ReadFileChunkToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IFileSystemService _fileSystemService;

    public ReadFileChunkToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"readchunk_tool_{Guid.NewGuid():N}");
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
    public async Task ReadFileChunk_ValidRange_ReturnsChunk()
    {
        // Arrange
        var lines = Enumerable.Range(0, 10).Select(i => $"Line {i}");
        File.WriteAllLines(Path.Combine(_tempDir, "lines.txt"), lines);

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "lines.txt", 2, 4, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var content = doc.RootElement.GetProperty("content").GetString();
        
        Assert.Contains("Line 2", content);
        Assert.Contains("Line 3", content);
        Assert.Contains("Line 4", content);
        Assert.DoesNotContain("Line 1", content);
        Assert.DoesNotContain("Line 5", content);
    }

    [Fact]
    public async Task ReadFileChunk_ReturnsMetadata()
    {
        // Arrange
        var lines = Enumerable.Range(0, 10).Select(i => $"Line {i}");
        File.WriteAllLines(Path.Combine(_tempDir, "meta.txt"), lines);

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "meta.txt", 0, 2, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        
        Assert.Equal(0, doc.RootElement.GetProperty("startLine").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("endLine").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("totalLines").GetInt64());
    }

    [Fact]
    public async Task ReadFileChunk_RangeExceedsFile_ClampsToBounds()
    {
        // Arrange
        var lines = new[] { "Line 0", "Line 1", "Line 2" };
        File.WriteAllLines(Path.Combine(_tempDir, "small.txt"), lines);

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "small.txt", 0, 100, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("endLine").GetInt32()); // Clamped to max
    }

    [Fact]
    public async Task ReadFileChunk_NegativeStart_ClampsToZero()
    {
        // Arrange
        var lines = new[] { "Line 0", "Line 1" };
        File.WriteAllLines(Path.Combine(_tempDir, "neg.txt"), lines);

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "neg.txt", -5, 1, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(0, doc.RootElement.GetProperty("startLine").GetInt32());
    }

    [Fact]
    public async Task ReadFileChunk_NonexistentFile_ReturnsError()
    {
        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "missing.txt", 0, 10, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("does not exist", error.GetString()?.ToLowerInvariant());
    }

    [Fact]
    public async Task ReadFileChunk_PathTraversal_ReturnsError()
    {
        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "../../../etc/passwd", 0, 10, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ReadFileChunk_SingleLine_ReturnsSingleLine()
    {
        // Arrange
        var lines = Enumerable.Range(0, 10).Select(i => $"Line {i}");
        File.WriteAllLines(Path.Combine(_tempDir, "single.txt"), lines);

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "single.txt", 5, 5, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        var content = doc.RootElement.GetProperty("content").GetString();
        Assert.Equal("Line 5", content);
    }

    [Fact]
    public async Task ReadFileChunk_IncludesRelativePath()
    {
        // Arrange
        File.WriteAllLines(Path.Combine(_tempDir, "path.txt"), new[] { "content" });

        // Act
        var result = await ReadFileChunkTool.ReadFileChunk(
            _fileSystemService, "path.txt", 0, 0, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("path.txt", doc.RootElement.GetProperty("relativePath").GetString());
    }
}
