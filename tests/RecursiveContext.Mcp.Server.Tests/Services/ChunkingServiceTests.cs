using System.Collections.Immutable;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class ChunkingServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ChunkingService _service;
    private readonly PathResolver _pathResolver;
    private readonly GuardrailService _guardrailService;

    public ChunkingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"chunking_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 500);
        _pathResolver = new PathResolver(settings);
        _guardrailService = new GuardrailService(settings);
        _service = new ChunkingService(_pathResolver, _guardrailService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTestFile(string name, int lineCount)
    {
        var filePath = Path.Combine(_tempDir, name);
        var lines = Enumerable.Range(1, lineCount).Select(i => $"Line {i}");
        File.WriteAllLines(filePath, lines);
        return name; // Return relative path
    }

    private string CreateTestFileWithContent(string name, string content)
    {
        var filePath = Path.Combine(_tempDir, name);
        File.WriteAllText(filePath, content);
        return name;
    }

    #region GetChunkInfoAsync Tests

    [Fact]
    public async Task GetChunkInfoAsync_EmptyFile_ReturnsZeroChunks()
    {
        var relativePath = CreateTestFileWithContent("empty.txt", "");

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalLines);
        Assert.Equal(0, result.Value.ChunkCount);
        Assert.Empty(result.Value.ChunkBoundaries);
    }

    [Fact]
    public async Task GetChunkInfoAsync_SingleLine_ReturnsOneChunk()
    {
        var relativePath = CreateTestFile("single.txt", 1);

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalLines);
        Assert.Equal(1, result.Value.ChunkCount);
        Assert.Single(result.Value.ChunkBoundaries);
        Assert.Equal((1, 1), result.Value.ChunkBoundaries[0]);
    }

    [Fact]
    public async Task GetChunkInfoAsync_ExactMultiple_CalculatesCorrectBoundaries()
    {
        var relativePath = CreateTestFile("exact.txt", 30);

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.TotalLines);
        Assert.Equal(3, result.Value.ChunkCount);
        Assert.Equal((1, 10), result.Value.ChunkBoundaries[0]);
        Assert.Equal((11, 20), result.Value.ChunkBoundaries[1]);
        Assert.Equal((21, 30), result.Value.ChunkBoundaries[2]);
    }

    [Fact]
    public async Task GetChunkInfoAsync_NotExactMultiple_CalculatesCorrectBoundaries()
    {
        var relativePath = CreateTestFile("partial.txt", 25);

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value.TotalLines);
        Assert.Equal(3, result.Value.ChunkCount);
        Assert.Equal((1, 10), result.Value.ChunkBoundaries[0]);
        Assert.Equal((11, 20), result.Value.ChunkBoundaries[1]);
        Assert.Equal((21, 25), result.Value.ChunkBoundaries[2]); // Last chunk is smaller
    }

    [Fact]
    public async Task GetChunkInfoAsync_ChunkSizeLargerThanFile_ReturnsOneChunk()
    {
        var relativePath = CreateTestFile("small.txt", 5);

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalLines);
        Assert.Equal(1, result.Value.ChunkCount);
        Assert.Single(result.Value.ChunkBoundaries);
        Assert.Equal((1, 5), result.Value.ChunkBoundaries[0]);
    }

    [Fact]
    public async Task GetChunkInfoAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.GetChunkInfoAsync("nonexistent.txt", chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task GetChunkInfoAsync_ChunkSizeTooLarge_ReturnsFailure()
    {
        var relativePath = CreateTestFile("test.txt", 10);
        var settings = new RlmSettings(_tempDir, 1_000_000, 100, 30, 20, 500, 10_000, 50); // MaxChunkSize = 50
        var pathResolver = new PathResolver(settings);
        var guardrails = new GuardrailService(settings);
        var service = new ChunkingService(pathResolver, guardrails);

        var result = await service.GetChunkInfoAsync(relativePath, chunkSize: 100, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("exceeds max chunk size", result.Error);
    }

    #endregion

    #region ReadChunkAsync Tests

    [Fact]
    public async Task ReadChunkAsync_FirstChunk_ReturnsCorrectContent()
    {
        var relativePath = CreateTestFile("test.txt", 25);

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 0, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.ChunkIndex);
        Assert.Equal(1, result.Value.StartLine);
        Assert.Equal(10, result.Value.EndLine);
        Assert.Contains("Line 1", result.Value.Content);
        Assert.Contains("Line 10", result.Value.Content);
        Assert.DoesNotContain("Line 11", result.Value.Content);
    }

    [Fact]
    public async Task ReadChunkAsync_MiddleChunk_ReturnsCorrectContent()
    {
        var relativePath = CreateTestFile("test.txt", 25);

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 1, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ChunkIndex);
        Assert.Equal(11, result.Value.StartLine);
        Assert.Equal(20, result.Value.EndLine);
        Assert.Contains("Line 11", result.Value.Content);
        Assert.Contains("Line 20", result.Value.Content);
    }

    [Fact]
    public async Task ReadChunkAsync_LastChunk_ReturnsCorrectContent()
    {
        var relativePath = CreateTestFile("test.txt", 25);

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 2, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.ChunkIndex);
        Assert.Equal(21, result.Value.StartLine);
        Assert.Equal(25, result.Value.EndLine);
        Assert.Contains("Line 21", result.Value.Content);
        Assert.Contains("Line 25", result.Value.Content);
    }

    [Fact]
    public async Task ReadChunkAsync_EmptyFileIndex0_ReturnsEmptyContent()
    {
        var relativePath = CreateTestFileWithContent("empty.txt", "");

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 0, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.ChunkIndex);
        Assert.Equal(string.Empty, result.Value.Content);
    }

    [Fact]
    public async Task ReadChunkAsync_EmptyFileInvalidIndex_ReturnsFailure()
    {
        var relativePath = CreateTestFileWithContent("empty.txt", "");

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 1, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("out of range", result.Error);
    }

    [Fact]
    public async Task ReadChunkAsync_NegativeIndex_ReturnsFailure()
    {
        var relativePath = CreateTestFile("test.txt", 20);

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: -1, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("out of range", result.Error);
    }

    [Fact]
    public async Task ReadChunkAsync_IndexBeyondChunkCount_ReturnsFailure()
    {
        var relativePath = CreateTestFile("test.txt", 20);

        var result = await _service.ReadChunkAsync(relativePath, chunkIndex: 5, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("out of range", result.Error);
    }

    [Fact]
    public async Task ReadChunkAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _service.ReadChunkAsync("nonexistent.txt", chunkIndex: 0, chunkSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("does not exist", result.Error);
    }

    #endregion

    #region Determinism Tests - Critical

    [Fact]
    public async Task GetChunkInfoAsync_SameInputsMultipleCalls_ReturnsSameBoundaries()
    {
        var relativePath = CreateTestFile("test.txt", 100);

        var result1 = await _service.GetChunkInfoAsync(relativePath, chunkSize: 15, CancellationToken.None);
        var result2 = await _service.GetChunkInfoAsync(relativePath, chunkSize: 15, CancellationToken.None);
        var result3 = await _service.GetChunkInfoAsync(relativePath, chunkSize: 15, CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);

        // Same total lines
        Assert.Equal(result1.Value.TotalLines, result2.Value.TotalLines);
        Assert.Equal(result2.Value.TotalLines, result3.Value.TotalLines);

        // Same chunk count
        Assert.Equal(result1.Value.ChunkCount, result2.Value.ChunkCount);
        Assert.Equal(result2.Value.ChunkCount, result3.Value.ChunkCount);

        // Same boundaries
        Assert.Equal(result1.Value.ChunkBoundaries.Length, result2.Value.ChunkBoundaries.Length);
        for (int i = 0; i < result1.Value.ChunkBoundaries.Length; i++)
        {
            Assert.Equal(result1.Value.ChunkBoundaries[i], result2.Value.ChunkBoundaries[i]);
            Assert.Equal(result2.Value.ChunkBoundaries[i], result3.Value.ChunkBoundaries[i]);
        }
    }

    [Fact]
    public async Task ReadChunkAsync_SameInputsMultipleCalls_ReturnsSameContent()
    {
        var relativePath = CreateTestFile("test.txt", 50);

        var result1 = await _service.ReadChunkAsync(relativePath, chunkIndex: 2, chunkSize: 10, CancellationToken.None);
        var result2 = await _service.ReadChunkAsync(relativePath, chunkIndex: 2, chunkSize: 10, CancellationToken.None);
        var result3 = await _service.ReadChunkAsync(relativePath, chunkIndex: 2, chunkSize: 10, CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);

        Assert.Equal(result1.Value.ChunkIndex, result2.Value.ChunkIndex);
        Assert.Equal(result1.Value.StartLine, result2.Value.StartLine);
        Assert.Equal(result1.Value.EndLine, result2.Value.EndLine);
        Assert.Equal(result1.Value.Content, result2.Value.Content);

        Assert.Equal(result2.Value.ChunkIndex, result3.Value.ChunkIndex);
        Assert.Equal(result2.Value.StartLine, result3.Value.StartLine);
        Assert.Equal(result2.Value.EndLine, result3.Value.EndLine);
        Assert.Equal(result2.Value.Content, result3.Value.Content);
    }

    [Fact]
    public async Task ChunkBoundaries_MathematicallyCorrect()
    {
        // Verify: StartLine of chunk[i+1] = EndLine of chunk[i] + 1
        var relativePath = CreateTestFile("test.txt", 57);

        var result = await _service.GetChunkInfoAsync(relativePath, chunkSize: 13, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var boundaries = result.Value.ChunkBoundaries;

        // First chunk starts at 1
        Assert.Equal(1, boundaries[0].StartLine);

        // Each subsequent chunk starts right after the previous ends
        for (int i = 1; i < boundaries.Length; i++)
        {
            Assert.Equal(boundaries[i - 1].EndLine + 1, boundaries[i].StartLine);
        }

        // Last chunk ends at total lines
        Assert.Equal(result.Value.TotalLines, boundaries[^1].EndLine);

        // Each chunk size <= chunkSize
        foreach (var (start, end) in boundaries)
        {
            Assert.True(end - start + 1 <= 13);
        }
    }

    [Fact]
    public async Task GetChunkInfo_And_ReadChunk_BoundariesMatch()
    {
        var relativePath = CreateTestFile("test.txt", 45);
        const int chunkSize = 10;

        var infoResult = await _service.GetChunkInfoAsync(relativePath, chunkSize, CancellationToken.None);
        Assert.True(infoResult.IsSuccess);

        // Read each chunk and verify boundaries match
        for (int i = 0; i < infoResult.Value.ChunkCount; i++)
        {
            var chunkResult = await _service.ReadChunkAsync(relativePath, i, chunkSize, CancellationToken.None);
            Assert.True(chunkResult.IsSuccess);

            var expectedBoundary = infoResult.Value.ChunkBoundaries[i];
            Assert.Equal(expectedBoundary.StartLine, chunkResult.Value.StartLine);
            Assert.Equal(expectedBoundary.EndLine, chunkResult.Value.EndLine);
        }
    }

    #endregion
}
