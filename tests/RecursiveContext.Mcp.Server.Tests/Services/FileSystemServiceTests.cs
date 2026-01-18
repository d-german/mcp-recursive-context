using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class FileSystemServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;
    private readonly FileSystemService _service;

    public FileSystemServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fs_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20);
        _pathResolver = new PathResolver(settings);
        _guardrails = new GuardrailService(settings);
        _service = new FileSystemService(_pathResolver, _guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ListFilesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var result = await _service.ListFilesAsync(".", 0, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Files);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task ListFilesAsync_WithFiles_ReturnsFileList()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "content2");

        var result = await _service.ListFilesAsync(".", 0, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Files.Length);
    }

    [Fact]
    public async Task ListFilesAsync_WithPagination_ReturnsPagedResults()
    {
        for (int i = 0; i < 5; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.txt"), "content");

        var result = await _service.ListFilesAsync(".", 1, 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Files.Length);
        Assert.Equal(1, result.Value.Skip);
        Assert.Equal(2, result.Value.Take);
    }

    [Fact]
    public async Task ListFilesAsync_PathOutsideWorkspace_ReturnsFailure()
    {
        var result = await _service.ListFilesAsync("../../outside", 0, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("outside workspace", result.Error);
    }

    [Fact]
    public async Task ListFilesAsync_NonexistentPath_ReturnsFailure()
    {
        var result = await _service.ListFilesAsync("nonexistent", 0, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task ListDirectoriesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var result = await _service.ListDirectoriesAsync(".", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Directories);
    }

    [Fact]
    public async Task ListDirectoriesAsync_WithSubdirectories_ReturnsList()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir1"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir2"));

        var result = await _service.ListDirectoriesAsync(".", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task ReadFileAsync_ValidFile_ReturnsContent()
    {
        var content = "Hello, World!";
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), content);

        var result = await _service.ReadFileAsync("test.txt", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Value);
    }

    [Fact]
    public async Task ReadFileAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.ReadFileAsync("missing.txt", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task ReadFileAsync_FileTooLarge_ReturnsFailure()
    {
        var smallSettings = new RlmSettings(_tempDir, 10, 100, 30, 20);
        var smallGuardrails = new GuardrailService(smallSettings);
        var service = new FileSystemService(_pathResolver, smallGuardrails);

        File.WriteAllText(Path.Combine(_tempDir, "large.txt"), new string('x', 100));

        var result = await service.ReadFileAsync("large.txt", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("exceeds max bytes", result.Error);
    }

    [Fact]
    public async Task ReadFileChunkAsync_ValidRange_ReturnsChunk()
    {
        var lines = Enumerable.Range(1, 10).Select(i => $"Line {i}");
        File.WriteAllLines(Path.Combine(_tempDir, "lines.txt"), lines);

        var result = await _service.ReadFileChunkAsync("lines.txt", 2, 4, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.StartLine);
        Assert.Equal(4, result.Value.EndLine);
        Assert.Equal(10, result.Value.TotalLines);
        Assert.Contains("Line 3", result.Value.Content);
        Assert.Contains("Line 4", result.Value.Content);
        Assert.Contains("Line 5", result.Value.Content);
    }

    [Fact]
    public async Task ReadFileChunkAsync_InvalidRange_ClampsToBounds()
    {
        var lines = Enumerable.Range(1, 5).Select(i => $"Line {i}");
        File.WriteAllLines(Path.Combine(_tempDir, "small.txt"), lines);

        var result = await _service.ReadFileChunkAsync("small.txt", -5, 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.StartLine);
        Assert.Equal(4, result.Value.EndLine);
    }

    [Fact]
    public async Task ReadFileChunkAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.ReadFileChunkAsync("missing.txt", 0, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }
}
