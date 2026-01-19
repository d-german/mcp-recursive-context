using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

/// <summary>
/// MCP tool for grouping and counting pattern matches within a single file.
/// </summary>
[McpServerToolType]
internal static class AggregatePatternMatchesTool
{
    [McpServerTool(Name = "aggregate_pattern_matches")]
    [Description("Group and count pattern matches in a file, returning top N groups. Useful for word frequency, categorization, or breakdown analysis. Uses .NET regex syntax.")]
    public static async Task<string> AggregatePatternMatches(
        IAdvancedAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Regex pattern to match (.NET syntax). Use capture groups like (\\\\w+) to extract grouping keys.")] string pattern,
        [Description("How to group matches: 'captureGroup1' (first capture group), 'firstWord' (first word of match), 'fullMatch' (entire match). Default: fullMatch")] string groupBy = "fullMatch",
        [Description("Number of top groups to return. Default: 10")] int topN = 10,
        [Description("When true (default), include a sample match per group")] bool includeSamples = true,
        CancellationToken ct = default)
    {
        var result = await analysisService.AggregatePatternMatchesAsync(
            path, pattern, groupBy, topN, includeSamples, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
