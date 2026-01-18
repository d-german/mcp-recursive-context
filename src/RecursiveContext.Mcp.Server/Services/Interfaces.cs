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
/// Service for enforcing rate limits and guardrails. 
/// </summary> 
public interface IGuardrailService 
{ 
    Result CheckAndIncrementCallCount(); 
    Result CheckBytesLimit(long bytes); 
    int RemainingCalls { get; } 
    int MaxToolCallsPerSession { get; } 
    int MaxBytesPerRead { get; } 
}
