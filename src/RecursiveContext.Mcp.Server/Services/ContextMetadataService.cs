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
 
    private static WorkspaceStats ComputeStats(string rootPath, int maxDepth, int initialDepth)
    {
        var globalStats = new WorkspaceStats();
        
        // Get top-level directories for parallel processing
        var topLevelDirs = new List<(string path, int depth)>();
        
        try
        {
            // Process root files sequentially first
            foreach (var file in Directory.EnumerateFiles(rootPath))
            {
                var fi = new FileInfo(file);
                globalStats.FileCount++;
                globalStats.TotalBytes += fi.Length;
                var ext = fi.Extension.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "[no extension]";
                globalStats.ExtensionCounts.TryGetValue(ext, out var count);
                globalStats.ExtensionCounts[ext] = count + 1;
            }
            globalStats.MaxDepthReached = initialDepth;

            // Collect top-level directories
            foreach (var dir in Directory.EnumerateDirectories(rootPath))
            {
                globalStats.DirCount++;
                if (initialDepth + 1 <= maxDepth)
                {
                    topLevelDirs.Add((dir, initialDepth + 1));
                }
            }
        }
        catch { }

        if (topLevelDirs.Count == 0)
            return globalStats;

        // Process directories in parallel
        var lockObj = new object();
        Parallel.ForEach(topLevelDirs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, dirInfo =>
        {
            var localStats = ComputeStatsSequential(dirInfo.path, maxDepth, dirInfo.depth);
            lock (lockObj)
            {
                globalStats.Merge(localStats);
            }
        });

        return globalStats;
    }

    private static WorkspaceStats ComputeStatsSequential(string rootPath, int maxDepth, int initialDepth)
    {
        var stats = new WorkspaceStats();
        var stack = new Stack<(string path, int depth)>();
        stack.Push((rootPath, initialDepth));

        while (stack.Count > 0)
        {
            var (path, currentDepth) = stack.Pop();
            
            if (currentDepth > maxDepth) continue;

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
                    stack.Push((dir, currentDepth + 1));
                }

                stats.MaxDepthReached = Math.Max(stats.MaxDepthReached, currentDepth);
            }
            catch { }
        }

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
