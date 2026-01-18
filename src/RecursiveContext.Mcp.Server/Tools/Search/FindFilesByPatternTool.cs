using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.Search; 
 
[McpServerToolType] 
internal static class FindFilesByPatternTool 
{ 
    [McpServerTool(Name = "find_files_by_pattern")] 
    [Description("Finds files matching a glob pattern. Supports *, **, and ? wildcards.")] 
    public static async Task<string> FindFilesByPattern( 
        IPatternMatchingService patternService, 
        [Description("Glob pattern ^(e.g., *.cs, **/*.json, src/**/*Controller.cs^)")] string pattern, 
        [Description("Maximum results to return. Default: 100")] int maxResults = 100, 
        CancellationToken ct = default) 
    { 
        var result = await patternService.FindFilesAsync(pattern, maxResults, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
