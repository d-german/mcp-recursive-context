using System.Collections.Immutable; 
using CSharpFunctionalExtensions; 
using RecursiveContext.Mcp.Server.Config; 
using RecursiveContext.Mcp.Server.Models; 
 
namespace RecursiveContext.Mcp.Server.Services; 
 
internal sealed class ContextMetadataService : IContextMetadataService 
{ 
    private readonly PathResolver _pathResolver; 
    private readonly IGuardrailService _guardrails; 
 
    public ContextMetadataService(PathResolver pathResolver, IGuardrailService guardrails) 
    { 
        _pathResolver = pathResolver; 
        _guardrails = guardrails; 
    } 
 
    public Task<Result<ContextInfo>> GetContextInfoAsync(int maxDepth, CancellationToken ct) 
    { 
        var callCheck = _guardrails.CheckAndIncrementCallCount(); 
        if (callCheck.IsFailure) 
            return Task.FromResult(Result.Failure<ContextInfo>(callCheck.Error)); 
 
        ct.ThrowIfCancellationRequested(); 
 
        var rootPath = _pathResolver.WorkspaceRoot; 
        var stats = ComputeStats(rootPath, maxDepth, 0); 
 
        var contextInfo = new ContextInfo( 
            WorkspaceRoot: rootPath, 
            TotalFiles: stats.FileCount, 
            TotalSizeBytes: stats.TotalBytes, 
            TotalDirectories: stats.DirCount, 
            MaxDepth: stats.MaxDepthReached, 
            FilesByExtension: stats.ExtensionCounts.ToImmutableDictionary() 
        ); 
 
        return Task.FromResult(Result.Success(contextInfo)); 
    } 
 
    private static WorkspaceStats ComputeStats(string path, int maxDepth, int currentDepth) 
    { 
        var stats = new WorkspaceStats(); 
 
        if (currentDepth > maxDepth) return stats; 
 
        try 
        { 
            foreach (var file in Directory.EnumerateFiles(path)) 
            { 
                var fi = new FileInfo(file); 
                stats.FileCount++; 
                stats.TotalBytes += fi.Length; 
                var ext = fi.Extension.ToLowerInvariant(); 
                if (string.IsNullOrEmpty(ext)) ext = "[no extension]"; 
                stats.ExtensionCounts.TryGetValue(ext, out var count); 
                stats.ExtensionCounts[ext] = count + 1; 
            } 
 
            foreach (var dir in Directory.EnumerateDirectories(path)) 
            { 
                stats.DirCount++; 
                var subStats = ComputeStats(dir, maxDepth, currentDepth + 1); 
                stats.Merge(subStats); 
            } 
 
            stats.MaxDepthReached = Math.Max(stats.MaxDepthReached, currentDepth); 
        } 
        catch { } 
 
        return stats; 
    } 
 
    private sealed class WorkspaceStats 
    { 
        public int FileCount { get; set; } 
        public long TotalBytes { get; set; } 
        public int DirCount { get; set; } 
        public int MaxDepthReached { get; set; } 
        public Dictionary<string, int> ExtensionCounts { get; } = new(); 
 
        public void Merge(WorkspaceStats other) 
        { 
            FileCount += other.FileCount; 
            TotalBytes += other.TotalBytes; 
            DirCount += other.DirCount; 
            MaxDepthReached = Math.Max(MaxDepthReached, other.MaxDepthReached); 
            foreach (var (ext, count) in other.ExtensionCounts) 
            { 
                ExtensionCounts.TryGetValue(ext, out var existing); 
                ExtensionCounts[ext] = existing + count; 
            } 
        } 
    } 
}
