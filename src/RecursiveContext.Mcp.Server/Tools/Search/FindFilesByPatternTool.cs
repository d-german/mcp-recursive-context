using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.Search; 
 
[McpServerToolType] 
internal static class FindFilesByPatternTool 
{ 
    [McpServerTool(Name = "find_files_by_pattern")] 
    [Description("Finds files matching a glob pattern from workspace root. Returns full relative paths that can be used directly with other tools. Supports *, **, and ? wildcards.")] 
    public static async Task<string> FindFilesByPattern( 
        IPatternMatchingService patternService, 
        [Description("Glob pattern relative to workspace root. Examples: '**/*.txt' (all txt files), 'src/**/*.cs' (C# files in src), 'data/train/*.json'. The returned paths can be used directly with search_with_context and other tools.")] string pattern, 
        [Description("Maximum results to return. Default: 100")] int maxResults = 100, 
        CancellationToken ct = default) 
    { 
        var result = await patternService.FindFilesAsync(pattern, maxResults, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
