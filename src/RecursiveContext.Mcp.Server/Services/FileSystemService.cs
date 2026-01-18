using System.Collections.Immutable; 
using CSharpFunctionalExtensions; 
using RecursiveContext.Mcp.Server.Config; 
using RecursiveContext.Mcp.Server.Models; 
 
namespace RecursiveContext.Mcp.Server.Services; 
 
internal sealed class FileSystemService : IFileSystemService 
{ 
    private readonly PathResolver _pathResolver; 
    private readonly IGuardrailService _guardrails; 
 
    public FileSystemService(PathResolver pathResolver, IGuardrailService guardrails) 
    { 
        _pathResolver = pathResolver; 
        _guardrails = guardrails; 
    } 
 
    public Task<Result<FileListResult>> ListFilesAsync(string relativePath, int skip, int take, CancellationToken ct) 
    { 
        var result = _guardrails.CheckAndIncrementCallCount() 
            .Bind(() => _pathResolver.ResolveAndValidateExists(relativePath)) 
            .Map(fullPath => ListFilesInternal(fullPath, skip, take)); 
        return Task.FromResult(result); 
    } 
 
    private static FileListResult ListFilesInternal(string dirPath, int skip, int take) 
    { 
        var files = Directory.GetFiles(dirPath) 
            .Select(f => new FileInfo(f)) 
            .Select(fi => new FileInfoModel( 
                fi.Name, 
                fi.FullName, 
                fi.Length, 
                fi.LastWriteTimeUtc, 
                fi.IsReadOnly)) 
            .ToArray(); 
        var paged = files.Skip(skip).Take(take).ToImmutableArray(); 
        return new FileListResult(paged, files.Length, skip, take); 
    } 
 
    public Task<Result<DirectoryListResult>> ListDirectoriesAsync(string relativePath, CancellationToken ct) 
    { 
        var result = _guardrails.CheckAndIncrementCallCount() 
            .Bind(() => _pathResolver.ResolveAndValidateExists(relativePath)) 
            .Map(ListDirectoriesInternal); 
        return Task.FromResult(result); 
    } 
 
    private static DirectoryListResult ListDirectoriesInternal(string dirPath) 
    { 
        var dirs = Directory.GetDirectories(dirPath) 
            .Select(d => new DirectoryInfo(d)) 
            .Select(di => new DirectoryInfoModel( 
                di.Name, 
                di.FullName, 
                Directory.GetFiles(di.FullName).Length, 
                Directory.GetDirectories(di.FullName).Length)) 
            .ToImmutableArray(); 
        return new DirectoryListResult(dirs, dirs.Length); 
    } 
 
    public async Task<Result<string>> ReadFileAsync(string relativePath, CancellationToken ct) 
    { 
        var pathResult = _pathResolver.ResolveAndValidateExists(relativePath); 
        if (pathResult.IsFailure) return Result.Failure<string>(pathResult.Error); 
 
        var fileInfo = new FileInfo(pathResult.Value); 
        var sizeCheck = _guardrails.CheckBytesLimit(fileInfo.Length); 
        if (sizeCheck.IsFailure) return Result.Failure<string>(sizeCheck.Error); 
 
        var callCheck = _guardrails.CheckAndIncrementCallCount(); 
        if (callCheck.IsFailure) return Result.Failure<string>(callCheck.Error); 
 
        var content = await File.ReadAllTextAsync(pathResult.Value, ct).ConfigureAwait(false); 
        return Result.Success(content); 
    } 
 
    public async Task<Result<FileChunk>> ReadFileChunkAsync(string relativePath, int startLine, int endLine, CancellationToken ct) 
    { 
        var pathResult = _pathResolver.ResolveAndValidateExists(relativePath); 
        if (pathResult.IsFailure) return Result.Failure<FileChunk>(pathResult.Error); 
 
        var callCheck = _guardrails.CheckAndIncrementCallCount(); 
        if (callCheck.IsFailure) return Result.Failure<FileChunk>(callCheck.Error); 
 
        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false); 
        var totalLines = lines.Length; 
        var start = Math.Max(0, startLine); 
        var end = Math.Min(totalLines - 1, endLine); 
        var chunk = string.Join(Environment.NewLine, lines.Skip(start).Take(end - start + 1)); 
 
        var fi = new FileInfo(pathResult.Value); 
        return Result.Success(new FileChunk(relativePath, chunk, start, end, totalLines, fi.Length)); 
    } 
}
