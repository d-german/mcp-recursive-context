using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class CountPatternMatchesTool
{
    [McpServerTool(Name = "count_pattern_matches")]
    [Description("Count regex pattern matches in a file. Returns exact count, not estimates.")]
    public static async Task<string> CountPatternMatches(
        IContentAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Regex pattern to count")] string pattern,
        [Description("Max matches to count. Default: 1000")] int maxResults = 1000,
        CancellationToken ct = default)
    {
        var result = await analysisService.CountPatternMatchesAsync(path, pattern, maxResults, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
