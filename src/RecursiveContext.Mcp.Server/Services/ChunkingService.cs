using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Models;

namespace RecursiveContext.Mcp.Server.Services;

/// <summary>
/// Service for chunking large files into manageable pieces.
/// </summary>
internal sealed class ChunkingService : IChunkingService
{
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;

    public ChunkingService(PathResolver pathResolver, IGuardrailService guardrails)
    {
        _pathResolver = pathResolver;
        _guardrails = guardrails;
    }

    public async Task<Result<ChunkInfo>> GetChunkInfoAsync(
        string path, int chunkSize, CancellationToken ct)
    {
        var sizeCheck = _guardrails.CheckChunkSize(chunkSize);
        if (sizeCheck.IsFailure)
            return Result.Failure<ChunkInfo>(sizeCheck.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<ChunkInfo>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<ChunkInfo>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var totalLines = lines.Length;

        if (totalLines == 0)
        {
            return Result.Success(new ChunkInfo(0, 0, ImmutableArray<(int, int)>.Empty));
        }

        var boundaries = CalculateChunkBoundaries(totalLines, chunkSize);

        return Result.Success(new ChunkInfo(totalLines, boundaries.Length, boundaries));
    }

    public async Task<Result<ChunkContent>> ReadChunkAsync(
        string path, int chunkIndex, int chunkSize, CancellationToken ct)
    {
        var sizeCheck = _guardrails.CheckChunkSize(chunkSize);
        if (sizeCheck.IsFailure)
            return Result.Failure<ChunkContent>(sizeCheck.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<ChunkContent>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<ChunkContent>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var totalLines = lines.Length;

        if (totalLines == 0)
        {
            return chunkIndex == 0
                ? Result.Success(new ChunkContent(0, 0, 0, string.Empty))
                : Result.Failure<ChunkContent>($"Chunk index {chunkIndex} out of range for empty file");
        }

        var chunks = lines.Chunk(chunkSize).ToArray();
        var chunkCount = chunks.Length;

        if (chunkIndex < 0 || chunkIndex >= chunkCount)
        {
            return Result.Failure<ChunkContent>(
                $"Chunk index {chunkIndex} out of range. Valid range: 0-{chunkCount - 1}");
        }

        var chunk = chunks[chunkIndex];
        var startLine = chunkIndex * chunkSize + 1;  // 1-based
        var endLine = startLine + chunk.Length - 1;
        var content = string.Join(Environment.NewLine, chunk);

        return Result.Success(new ChunkContent(chunkIndex, startLine, endLine, content));
    }

    private static ImmutableArray<(int StartLine, int EndLine)> CalculateChunkBoundaries(
        int totalLines, int chunkSize)
    {
        var boundaries = new List<(int, int)>();
        var currentLine = 1;  // 1-based

        while (currentLine <= totalLines)
        {
            var endLine = Math.Min(currentLine + chunkSize - 1, totalLines);
            boundaries.Add((currentLine, endLine));
            currentLine = endLine + 1;
        }

        return boundaries.ToImmutableArray();
    }
}
