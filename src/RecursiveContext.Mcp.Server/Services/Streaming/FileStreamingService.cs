using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using RecursiveContext.Mcp.Server.Config;

namespace RecursiveContext.Mcp.Server.Services.Streaming;

/// <summary>
/// Service for streaming file content line by line without loading entire file into memory.
/// </summary>
internal sealed class FileStreamingService : IFileStreamingService
{
    private readonly PathResolver _pathResolver;

    public FileStreamingService(PathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    /// <inheritdoc />
    public Result<IAsyncEnumerable<string>> ReadLinesAsync(string relativePath, CancellationToken ct)
    {
        var pathResult = _pathResolver.ResolveAndValidateExists(relativePath);
        if (pathResult.IsFailure)
            return Result.Failure<IAsyncEnumerable<string>>(pathResult.Error);

        return Result.Success(ReadLinesCore(pathResult.Value, ct));
    }

    /// <summary>
    /// Core streaming implementation using File.ReadLinesAsync.
    /// </summary>
    private static async IAsyncEnumerable<string> ReadLinesCore(
        string fullPath, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var line in File.ReadLinesAsync(fullPath, ct).ConfigureAwait(false))
        {
            yield return line;
        }
    }
}
