using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.FileSystem; 
 
[McpServerToolType] 
internal static class ListDirectoriesTool 
{ 
    [McpServerTool(Name = "list_directories")] 
    [Description("Lists subdirectories in a path with counts of files and subdirectories. RECOMMENDED: Call this first with path='.' to understand the workspace structure before using other tools.")] 
    public static async Task<string> ListDirectories( 
        IFileSystemService fileSystem, 
        [Description("Relative path to directory from workspace root. Use '.' for workspace root. This reveals the directory structure to use with other tools.")] string path, 
        CancellationToken ct = default) 
    { 
        var result = await fileSystem.ListDirectoriesAsync(path, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
