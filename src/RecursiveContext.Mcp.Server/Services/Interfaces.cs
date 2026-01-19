using CSharpFunctionalExtensions; 
using RecursiveContext.Mcp.Server.Models; 
 
namespace RecursiveContext.Mcp.Server.Services; 
 
/// <summary> 
/// Service for file system read operations. All operations are read-only. 
/// </summary> 
public interface IFileSystemService 
{ 
    Task<Result<FileListResult>> ListFilesAsync(string relativePath, int skip, int take, CancellationToken ct); 
    Task<Result<DirectoryListResult>> ListDirectoriesAsync(string relativePath, CancellationToken ct); 
    Task<Result<string>> ReadFileAsync(string relativePath, CancellationToken ct); 
    Task<Result<FileChunk>> ReadFileChunkAsync(string relativePath, int startLine, int endLine, CancellationToken ct); 
} 
 
/// <summary> 
/// Service for workspace context metadata. 
/// </summary> 
public interface IContextMetadataService 
{ 
    Task<Result<ContextInfo>> GetContextInfoAsync(int maxDepth, CancellationToken ct); 
} 
 
/// <summary> 
/// Service for pattern-based file discovery. 
/// </summary> 
public interface IPatternMatchingService 
{ 
    Task<Result<PatternMatchResult>> FindFilesAsync(string globPattern, int maxResults, CancellationToken ct); 
} 


/// <summary>
/// Service for content analysis operations (pattern matching, line counting).
/// </summary>
public interface IContentAnalysisService
{
    /// <summary>
    /// Counts pattern matches in a file, returning exact count and sample matches.
    /// </summary>
    Task<Result<MatchCountResult>> CountPatternMatchesAsync(
        string path, string pattern, int maxResults, CancellationToken ct);

    /// <summary>
    /// Searches for pattern matches with surrounding context lines.
    /// </summary>
    Task<Result<IReadOnlyList<MatchResult>>> SearchWithContextAsync(
        string path, string pattern, int contextLines, int maxResults, CancellationToken ct);

    /// <summary>
    /// Counts the total number of lines in a file.
    /// </summary>
    Task<Result<int>> CountLinesAsync(string path, CancellationToken ct);
}


/// <summary>
/// Service for chunking large files into manageable pieces.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Gets chunk information for a file (total lines, chunk count, boundaries).
    /// </summary>
    Task<Result<ChunkInfo>> GetChunkInfoAsync(
        string path, int chunkSize, CancellationToken ct);

    /// <summary>
    /// Reads a specific chunk from a file by index.
    /// </summary>
    Task<Result<ChunkContent>> ReadChunkAsync(
        string path, int chunkIndex, int chunkSize, CancellationToken ct);
}


/// <summary>
/// Service for aggregating analysis results across multiple files.
/// </summary>
public interface IAggregationService
{
    /// <summary>
    /// Aggregates pattern matches across multiple files in a directory.
    /// </summary>
    Task<Result<AggregateResult>> AggregateMatchesAsync(
        string directory, string filePattern, string searchPattern,
        int maxFiles, CancellationToken ct);

    /// <summary>
    /// Counts files matching a pattern in a directory.
    /// </summary>
    Task<Result<int>> CountFilesAsync(
        string directory, string pattern, bool recursive, CancellationToken ct);
}
 
/// <summary> 
/// Service for enforcing rate limits and guardrails. 
/// </summary> 
public interface IGuardrailService
{
    Result CheckAndIncrementCallCount();
    Result CheckBytesLimit(long bytes);
    Result CheckFilesLimit(int fileCount);
    Result CheckMatchesLimit(int matchCount);
    Result CheckChunkSize(int chunkSize);
    int RemainingCalls { get; }
    int MaxToolCallsPerSession { get; }
    int MaxBytesPerRead { get; }
    int MaxFilesPerAggregation { get; }
    int MaxMatchesPerSearch { get; }
    int MaxChunkSize { get; }
}
