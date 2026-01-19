using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services.Streaming;

namespace RecursiveContext.Mcp.Server.Tests.Services.Streaming;

/// <summary>
/// Unit tests for FileStreamingService.
/// </summary>
public class FileStreamingServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileStreamingService _service;
    private readonly PathResolver _pathResolver;

    public FileStreamingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"streaming_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _service = new FileStreamingService(_pathResolver);
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

    [Fact]
    public async Task ReadLinesAsync_ValidFile_ReturnsAllLines()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";
        var fileName = CreateTestFile("test.txt", content);

        // Act
        var result = _service.ReadLinesAsync(fileName, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var lines = new List<string>();
        await foreach (var line in result.Value)
        {
            lines.Add(line);
        }

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public void ReadLinesAsync_NonExistentFile_ReturnsFailure()
    {
        // Act
        var result = _service.ReadLinesAsync("nonexistent.txt", CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadLinesAsync_EmptyFile_ReturnsNoLines()
    {
        // Arrange
        var fileName = CreateTestFile("empty.txt", "");

        // Act
        var result = _service.ReadLinesAsync(fileName, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var lines = new List<string>();
        await foreach (var line in result.Value)
        {
            lines.Add(line);
        }

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task ReadLinesAsync_LargeFile_StreamsWithoutFullLoad()
    {
        // Arrange - create a file with many lines
        var content = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}"));
        var fileName = CreateTestFile("large.txt", content);

        // Act
        var result = _service.ReadLinesAsync(fileName, CancellationToken.None);
        Assert.True(result.IsSuccess);

        var count = 0;
        await foreach (var _ in result.Value)
        {
            count++;
            if (count == 10) break; // Early termination - only read first 10
        }

        // Assert - we only read 10 lines, streaming allows early termination
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task ReadLinesAsync_CancellationToken_RespectsCancellation()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        var fileName = CreateTestFile("cancel.txt", content);
        var cts = new CancellationTokenSource();

        // Act
        var result = _service.ReadLinesAsync(fileName, cts.Token);
        Assert.True(result.IsSuccess);

        var count = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in result.Value.WithCancellation(cts.Token))
            {
                count++;
                if (count == 5)
                    cts.Cancel();
            }
        });

        // Assert - should have been cancelled at or after count 5
        Assert.True(count >= 5);
    }
}
