using System.Text.RegularExpressions;
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
    /// Counts pattern matches in a file, returning exact count and optionally sample matches.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="pattern">Regex pattern to count.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="countUniqueLinesOnly">When true, count lines containing pattern instead of total matches.</param>
    /// <param name="includeSamples">When true, include sample matches in response.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<MatchCountResult>> CountPatternMatchesAsync(
        string path, string pattern, int maxResults,
        bool countUniqueLinesOnly, bool includeSamples, CancellationToken ct);

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

/// <summary>
/// Service for advanced pattern analysis operations (compound patterns, consecutive runs, aggregation, distributed sampling).
/// </summary>
public interface IAdvancedAnalysisService
{
    /// <summary>
    /// Counts lines matching multiple compound patterns.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="patterns">Array of regex patterns to match.</param>
    /// <param name="matchMode">Match mode: "all" (AND), "any" (OR), or "sequence" (patterns in order).</param>
    /// <param name="includeSamples">When true, include sample matches in response.</param>
    /// <param name="maxSamples">Maximum number of samples to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<CompoundMatchResult>> CountCompoundPatternAsync(
        string path, string[] patterns, string matchMode,
        bool includeSamples, int maxSamples, CancellationToken ct);

    /// <summary>
    /// Finds runs of consecutive lines matching a pattern.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="pattern">Regex pattern to match.</param>
    /// <param name="minRunLength">Minimum consecutive matches to form a run.</param>
    /// <param name="returnLongestOnly">When true, only return the longest run.</param>
    /// <param name="maxRuns">Maximum number of runs to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<ConsecutiveRunResult>> FindConsecutiveRunsAsync(
        string path, string pattern, int minRunLength,
        bool returnLongestOnly, int maxRuns, CancellationToken ct);

    /// <summary>
    /// Aggregates pattern matches by groups and returns top N.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="pattern">Regex pattern with optional capture group.</param>
    /// <param name="groupBy">Grouping mode: "captureGroup1", "firstWord", or "fullMatch".</param>
    /// <param name="topN">Number of top groups to return.</param>
    /// <param name="includeSamples">When true, include sample match per group.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<PatternAggregateResult>> AggregatePatternMatchesAsync(
        string path, string pattern, string groupBy,
        int topN, bool includeSamples, CancellationToken ct);

    /// <summary>
    /// Gets distributed sample matches spread across the file.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="pattern">Regex pattern to match.</param>
    /// <param name="sampleCount">Number of samples to return.</param>
    /// <param name="distribution">Distribution mode: "even", "random", "first", or "last".</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DistributedSampleResult>> SampleMatchesDistributedAsync(
        string path, string pattern, int sampleCount,
        string distribution, CancellationToken ct);

    /// <summary>
    /// Compares pattern match counts across multiple files.
    /// </summary>
    /// <param name="paths">Array of file paths to compare.</param>
    /// <param name="pattern">Regex pattern to count.</param>
    /// <param name="computeRatio">When true, compute relative ratios.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<CrossFileComparisonResult>> ComparePatternAcrossFilesAsync(
        string[] paths, string pattern, bool computeRatio, CancellationToken ct);

    /// <summary>
    /// Counts multiple patterns in a single file pass for efficiency.
    /// </summary>
    /// <param name="path">File path to search.</param>
    /// <param name="patterns">Array of regex patterns to count.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BatchPatternResult>> CountMultiplePatternsAsync(
        string path, string[] patterns, CancellationToken ct);
}


/// <summary>
/// Service for caching compiled regex patterns to avoid redundant compilation overhead.
/// </summary>

/// <summary>
/// Service for streaming file content line by line without loading entire file into memory.
/// </summary>
public interface IFileStreamingService
{
    /// <summary>
    /// Reads file lines as an async enumerable, streaming line by line.
    /// </summary>
    /// <param name="relativePath">Relative path to the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success with async enumerable of lines, or Failure if file not found/accessible.</returns>
    Result<IAsyncEnumerable<string>> ReadLinesAsync(string relativePath, CancellationToken ct);
}

public interface ICompiledRegexCache
{
    /// <summary>
    /// Gets a compiled regex from cache or compiles and caches a new one.
    /// </summary>
    /// <param name="pattern">The regex pattern string.</param>
    /// <returns>Success with compiled Regex, or Failure if pattern is invalid.</returns>
    Result<Regex> GetOrCompile(string pattern);
}

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
