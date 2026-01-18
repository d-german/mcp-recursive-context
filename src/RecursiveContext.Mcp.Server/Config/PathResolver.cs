using CSharpFunctionalExtensions; 
 
namespace RecursiveContext.Mcp.Server.Config; 
 
/// <summary> 
/// Resolves and validates file paths, ensuring all operations stay within workspace. 
/// Prevents directory traversal attacks and normalizes paths cross-platform. 
/// </summary> 
internal sealed class PathResolver 
{ 
    private readonly string _workspaceRoot; 
 
    public PathResolver(RlmSettings settings) 
    { 
        _workspaceRoot = Path.GetFullPath(settings.WorkspaceRoot); 
    } 
 
    public string WorkspaceRoot => _workspaceRoot; 
 
    public Result<string> ResolvePath(string relativePath) 
    { 
        if (string.IsNullOrWhiteSpace(relativePath)) 
            return Result.Failure<string>("Path cannot be empty"); 
 
        var normalized = NormalizePath(relativePath); 
        var fullPath = Path.IsPathRooted(normalized) 
            ? Path.GetFullPath(normalized) 
            : Path.GetFullPath(Path.Combine(_workspaceRoot, normalized)); 
 
        if (!IsWithinWorkspace(fullPath)) 
            return Result.Failure<string>($"Path '{relativePath}' is outside workspace"); 
 
        return Result.Success(fullPath); 
    } 
 
    public Result<string> ResolveAndValidateExists(string relativePath) 
    { 
        return ResolvePath(relativePath) 
            .Bind(path => 
            { 
                if (File.Exists(path) || Directory.Exists(path)) 
                    return Result.Success(path); 
                return Result.Failure<string>($"Path does not exist: {relativePath}"); 
            }); 
    } 
 
    private bool IsWithinWorkspace(string fullPath) 
    { 
        var normalizedFull = fullPath.Replace('/', Path.DirectorySeparatorChar); 
        var normalizedRoot = _workspaceRoot.Replace('/', Path.DirectorySeparatorChar); 
        return normalizedFull.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase); 
    } 
 
    private static string NormalizePath(string path) 
    { 
        return path 
            .Replace('\\', Path.DirectorySeparatorChar) 
            .Replace('/', Path.DirectorySeparatorChar) 
            .TrimStart(Path.DirectorySeparatorChar); 
    } 
}
