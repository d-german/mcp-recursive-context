using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class MultiPatternSearchTool
{
    [McpServerTool(Name = "multi_pattern_search")]
    [Description("Search for multiple regex patterns in one call. Returns files matching according to combineMode: 'union' (any pattern) or 'intersection' (all patterns). Useful for comprehensive searches with variations.")]
    public static async Task<string> MultiPatternSearch(
        IAggregationService aggregationService,
        [Description("Directory to search in, relative to workspace root. Use '.' for root.")] string directory,
        [Description("File pattern (glob, e.g., *.txt, *.cs, **/*.json)")] string filePattern,
        [Description("Array of regex patterns to search for (.NET syntax). Example: [\"\\\\bBERT\\\\b\", \"\\\\bGPT\\\\b\", \"transformer\"]")] string[] searchPatterns,
        [Description("How to combine results: 'union' (files matching ANY pattern) or 'intersection' (files matching ALL patterns). Default: union")] string combineMode = "union",
        [Description("Maximum files to search. Default: 1000000")] int maxFiles = 1000000,
        CancellationToken ct = default)
    {
        var result = await aggregationService.AggregateMultiPatternMatchesAsync(
                directory, filePattern, searchPatterns, combineMode, maxFiles, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
