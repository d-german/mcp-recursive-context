using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

/// <summary>
/// MCP tool for counting lines matching multiple compound patterns.
/// </summary>
[McpServerToolType]
internal static class CountCompoundPatternTool
{
    [McpServerTool(Name = "count_compound_pattern")]
    [Description("Count lines matching multiple regex patterns. Supports AND (all patterns match), OR (any pattern matches), or SEQUENCE (patterns appear in order) modes. Uses .NET regex syntax.")]
    public static async Task<string> CountCompoundPattern(
        IAdvancedAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Array of regex patterns to match (.NET syntax)")] string[] patterns,
        [Description("Match mode: 'all' (AND - all patterns must match), 'any' (OR - any pattern matches), 'sequence' (patterns appear in order on line)")] string matchMode = "all",
        [Description("When true, include sample matching lines in response. Default: false")] bool includeSamples = false,
        [Description("Maximum number of sample lines to return. Default: 5")] int maxSamples = 5,
        CancellationToken ct = default)
    {
        var result = await analysisService.CountCompoundPatternAsync(
            path, patterns, matchMode, includeSamples, maxSamples, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
