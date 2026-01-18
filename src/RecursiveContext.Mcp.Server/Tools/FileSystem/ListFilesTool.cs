using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.FileSystem; 
 
[McpServerToolType] 
internal static class ListFilesTool 
{ 
    [McpServerTool(Name = "list_files")] 
    [Description("Lists files in a directory with metadata. Returns name, size, and last modified date.")] 
    public static async Task<string> ListFiles( 
        IFileSystemService fileSystem, 
        [Description("Relative path to directory. Use '.' for workspace root.")] string path, 
        [Description("Number of items to skip ^(pagination^). Default: 0")] int skip = 0, 
        [Description("Number of items to take ^(pagination^). Default: 100")] int take = 100, 
        CancellationToken ct = default) 
    { 
        var result = await fileSystem.ListFilesAsync(path, skip, take, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
