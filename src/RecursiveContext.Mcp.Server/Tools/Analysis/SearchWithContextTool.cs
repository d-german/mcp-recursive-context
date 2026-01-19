using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class SearchWithContextTool
{
    [McpServerTool(Name = "search_with_context")]
    [Description("Search for regex pattern matches with surrounding context lines. Uses .NET regex: '.' is wildcard (use '\\\\.' for literal period), '^'/'$' match line boundaries.")]
    public static async Task<string> SearchWithContext(
        IContentAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Regex pattern to search for (.NET syntax)")] string pattern,
        [Description("Number of context lines before and after match. Default: 2")] int contextLines = 2,
        [Description("Maximum results to return. Default: 1000")] int maxResults = 1000,
        CancellationToken ct = default)
    {
        var result = await analysisService.SearchWithContextAsync(path, pattern, contextLines, maxResults, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
