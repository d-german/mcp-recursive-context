using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.FileSystem; 
 
[McpServerToolType] 
internal static class ListDirectoriesTool 
{ 
    [McpServerTool(Name = "list_directories")] 
    [Description("Lists subdirectories in a path with counts of files and subdirectories.")] 
    public static async Task<string> ListDirectories( 
        IFileSystemService fileSystem, 
        [Description("Relative path to directory. Use '.' for workspace root.")] string path, 
        CancellationToken ct = default) 
    { 
        var result = await fileSystem.ListDirectoriesAsync(path, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
