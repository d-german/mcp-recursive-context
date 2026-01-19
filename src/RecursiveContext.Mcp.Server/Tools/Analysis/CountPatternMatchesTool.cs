using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class CountPatternMatchesTool
{
    [McpServerTool(Name = "count_pattern_matches")]
    [Description("Count regex pattern matches in a file. By default counts lines containing pattern (like grep -c). Uses .NET regex: '.' is wildcard (use '\\\\.' for literal period), '^'/'$' match line boundaries.")]
    public static async Task<string> CountPatternMatches(
        IContentAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Regex pattern to count (.NET syntax)")] string pattern,
        [Description("Max matches to return. Default: 1000")] int maxResults = 1000,
        [Description("When true (default), count lines containing pattern. When false, count total occurrences.")] bool countUniqueLinesOnly = true,
        [Description("When true, include sample matches in response. Default: false for lightweight responses.")] bool includeSamples = false,
        CancellationToken ct = default)
    {
        var result = await analysisService.CountPatternMatchesAsync(
            path, pattern, maxResults, countUniqueLinesOnly, includeSamples, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
