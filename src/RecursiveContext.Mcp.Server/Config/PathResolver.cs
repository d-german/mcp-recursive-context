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

    /// <summary>
    /// Converts an absolute or relative path to a relative path from the workspace root.
    /// If the path equals the workspace root, returns ".".
    /// If the path is already relative, returns it normalized.
    /// </summary>
    public Result<string> ToRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result.Failure<string>("Path cannot be empty");

        if (path == ".")
            return Result.Success(".");

        var normalized = path
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // If not rooted, it's already relative
        if (!Path.IsPathRooted(normalized))
            return Result.Success(normalized.Replace(Path.DirectorySeparatorChar, '/'));

        var fullPath = Path.GetFullPath(normalized);
        var normalizedRoot = Path.GetFullPath(_workspaceRoot);

        // Ensure consistent trailing separator handling
        if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar))
            normalizedRoot += Path.DirectorySeparatorChar;

        // Check if path equals workspace root
        if (string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar), 
                         _workspaceRoot.TrimEnd(Path.DirectorySeparatorChar), 
                         StringComparison.OrdinalIgnoreCase))
            return Result.Success(".");

        // Check if path is within workspace
        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<string>($"Path '{path}' is outside workspace");

        // Extract relative portion
        var relativePath = fullPath.Substring(normalizedRoot.Length);
        return Result.Success(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
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
