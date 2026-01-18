using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.FileSystem; 
 
[McpServerToolType] 
internal static class ReadFileTool 
{ 
    [McpServerTool(Name = "read_file")] 
    [Description("Reads the full content of a file. Subject to size limits.")] 
    public static async Task<string> ReadFile( 
        IFileSystemService fileSystem, 
        [Description("Relative path to file.")] string path, 
        CancellationToken ct = default) 
    { 
        var result = await fileSystem.ReadFileAsync(path, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
