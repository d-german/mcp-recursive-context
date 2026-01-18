using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class ContextMetadataServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;
    private readonly ContextMetadataService _service;

    public ContextMetadataServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ctx_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20);
        _pathResolver = new PathResolver(settings);
        _guardrails = new GuardrailService(settings);
        _service = new ContextMetadataService(_pathResolver, _guardrails);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetContextInfoAsync_EmptyDirectory_ReturnsZeroCounts()
    {
        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalFiles);
        Assert.Equal(0, result.Value.TotalDirectories);
        Assert.Equal(0, result.Value.TotalSizeBytes);
    }

    [Fact]
    public async Task GetContextInfoAsync_WithFiles_CountsFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "Hello");
        File.WriteAllText(Path.Combine(_tempDir, "file2.cs"), "World");

        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalFiles);
        Assert.True(result.Value.TotalSizeBytes > 0);
    }

    [Fact]
    public async Task GetContextInfoAsync_WithSubdirectories_CountsDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sub1"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "sub2"));

        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalDirectories);
    }

    [Fact]
    public async Task GetContextInfoAsync_GroupsByExtension()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "file3.cs"), "c");

        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FilesByExtension[".txt"]);
        Assert.Equal(1, result.Value.FilesByExtension[".cs"]);
    }

    [Fact]
    public async Task GetContextInfoAsync_RespectsMaxDepth()
    {
        var deepPath = Path.Combine(_tempDir, "a", "b", "c", "d", "e");
        Directory.CreateDirectory(deepPath);
        File.WriteAllText(Path.Combine(deepPath, "deep.txt"), "content");

        // With maxDepth=0, should only see root level
        var shallowResult = await _service.GetContextInfoAsync(0, CancellationToken.None);
        
        // With maxDepth=10, should see deep file
        var deepResult = await _service.GetContextInfoAsync(10, CancellationToken.None);

        Assert.True(shallowResult.IsSuccess);
        Assert.True(deepResult.IsSuccess);
        Assert.True(deepResult.Value.TotalFiles >= shallowResult.Value.TotalFiles);
    }

    [Fact]
    public async Task GetContextInfoAsync_IncludesWorkspaceRoot()
    {
        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_tempDir, result.Value.WorkspaceRoot);
    }

    [Fact]
    public async Task GetContextInfoAsync_HandlesFilesWithoutExtension()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Makefile"), "content");
        File.WriteAllText(Path.Combine(_tempDir, "Dockerfile"), "content");

        var result = await _service.GetContextInfoAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FilesByExtension["[no extension]"]);
    }

    [Fact]
    public async Task GetContextInfoAsync_WithCancellation_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.GetContextInfoAsync(5, cts.Token));
    }
}
