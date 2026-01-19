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
