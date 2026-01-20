using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.Search; 
 
[McpServerToolType] 
internal static class FindFilesByPatternTool 
{ 
    [McpServerTool(Name = "find_files_by_pattern")] 
    [Description("Find files matching a glob pattern. TIP: Use returned paths directly with search tools. Supports *, **, and ? wildcards.")] 
    public static async Task<string> FindFilesByPattern( 
        IPatternMatchingService patternService, 
        [Description("Glob pattern relative to workspace root. Examples: '**/*.txt', 'src/**/*.cs'.")] string pattern, 
        [Description("Maximum results to return. Default: 100")] int maxResults = 100, 
        CancellationToken ct = default) 
    { 
        var result = await patternService.FindFilesAsync(pattern, maxResults, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
