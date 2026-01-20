using System.Collections.Immutable; 
 
namespace RecursiveContext.Mcp.Server.Models; 
 
/// <summary> 
/// Information about a file in the workspace. 
/// </summary> 
public sealed record FileInfoModel( 
    string Name, 
    string RelativePath, 
    long SizeBytes, 
    DateTimeOffset LastModified, 
    bool IsReadOnly 
); 
 
/// <summary> 
/// Information about a directory in the workspace. 
/// </summary> 
public sealed record DirectoryInfoModel( 
    string Name, 
    string RelativePath, 
    int FileCount, 
    int SubdirectoryCount 
); 
 
/// <summary> 
/// A chunk/portion of file content. 
/// </summary> 
public sealed record FileChunk( 
    string RelativePath, 
    string Content, 
    int StartLine, 
    int EndLine, 
    long TotalLines, 
    long TotalBytes 
); 
 
/// <summary> 
/// High-level metadata about the workspace context. 
/// </summary> 
public sealed record ContextInfo( 
    string WorkspaceRoot, 
    int TotalFiles, 
    long TotalSizeBytes, 
    int TotalDirectories, 
    int MaxDepth, 
    ImmutableDictionary<string, int> FilesByExtension 
); 
 
/// <summary> 
/// List of files result. 
/// </summary> 
public sealed record FileListResult( 
    ImmutableArray<FileInfoModel> Files, 
    int TotalCount, 
    int Skip, 
    int Take 
); 
 
/// <summary> 
/// List of directories result. 
/// </summary> 
public sealed record DirectoryListResult( 
    ImmutableArray<DirectoryInfoModel> Directories, 
    int TotalCount 
); 
 
/// <summary> 
/// Pattern match result. 
/// </summary> 
public sealed record PatternMatchResult( 
    string Pattern, 
    ImmutableArray<string> MatchingPaths, 
    int TotalMatches 
);


// ============================================================================
// Analysis Tool Models
// ============================================================================

/// <summary>
/// A single match result with line number and surrounding context.
/// </summary>
public sealed record MatchResult(
    int LineNumber,
    string MatchText,
    ImmutableArray<string> ContextBefore,
    ImmutableArray<string> ContextAfter
);

/// <summary>
/// Result of counting pattern matches in a file.
/// </summary>
public sealed record MatchCountResult(
    int Count,
    ImmutableArray<MatchResult> SampleMatches,
    bool Truncated
);

/// <summary>
/// Information about chunk boundaries for a file.
/// </summary>
public sealed record ChunkInfo(
    int TotalLines,
    int ChunkCount,
    ImmutableArray<(int StartLine, int EndLine)> ChunkBoundaries
);

/// <summary>
/// Content of a specific chunk from a file.
/// </summary>
public sealed record ChunkContent(
    int ChunkIndex,
    int StartLine,
    int EndLine,
    string Content
);

/// <summary>
/// Count of matches in a single file.
/// </summary>
public sealed record FileMatchCount(
    string Path,
    int Count
);

/// <summary>
/// Aggregated match results across multiple files.
/// </summary>
public sealed record AggregateResult(
    int FilesSearched,
    int TotalMatches,
    ImmutableArray<FileMatchCount> MatchesByFile
);

/// <summary>
/// Result for a single pattern in a multi-pattern search.
/// </summary>
/// <param name="Pattern">The regex pattern that was searched.</param>
/// <param name="MatchCount">Total matches for this pattern.</param>
/// <param name="MatchingFileCount">Number of files that matched this pattern.</param>
public sealed record PatternMatchInfo(
    string Pattern,
    int MatchCount,
    int MatchingFileCount
);

/// <summary>
/// Per-file breakdown showing which patterns matched in each file.
/// </summary>
/// <param name="Path">Relative path to the file.</param>
/// <param name="MatchedPatternIndices">Indices of patterns that matched in this file.</param>
/// <param name="TotalMatches">Total match count across all patterns in this file.</param>
public sealed record FilePatternMatches(
    string Path,
    ImmutableArray<int> MatchedPatternIndices,
    int TotalMatches
);

/// <summary>
/// Result of a multi-pattern search across files.
/// </summary>
/// <param name="FilesSearched">Total number of files searched.</param>
/// <param name="CombineMode">How results were combined: 'union' or 'intersection'.</param>
/// <param name="PatternResults">Per-pattern match statistics.</param>
/// <param name="MatchingFiles">Files matching according to the combine mode.</param>
/// <param name="FileBreakdown">Detailed breakdown of which patterns matched in each file.</param>
public sealed record MultiPatternResult(
    int FilesSearched,
    string CombineMode,
    ImmutableArray<PatternMatchInfo> PatternResults,
    ImmutableArray<string> MatchingFiles,
    ImmutableArray<FilePatternMatches> FileBreakdown
);
