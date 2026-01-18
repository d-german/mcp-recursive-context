using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.FileSystem; 
 
[McpServerToolType] 
internal static class ReadFileChunkTool 
{ 
    [McpServerTool(Name = "read_file_chunk")] 
    [Description("Reads a portion of a file by line range. Useful for large files.")] 
    public static async Task<string> ReadFileChunk( 
        IFileSystemService fileSystem, 
        [Description("Relative path to file.")] string path, 
        [Description("Start line ^(0-based^). Default: 0")] int startLine = 0, 
        [Description("End line ^(0-based, inclusive^). Default: 100")] int endLine = 100, 
        CancellationToken ct = default) 
    { 
        var result = await fileSystem.ReadFileChunkAsync(path, startLine, endLine, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
