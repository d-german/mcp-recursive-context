using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class AggregateMatchesTool
{
    [McpServerTool(Name = "aggregate_matches")]
    [Description("Count regex pattern matches across multiple files. Returns total count plus per-file breakdown.")]
    public static async Task<string> AggregateMatches(
        IAggregationService aggregationService,
        [Description("Directory to search in")] string directory,
        [Description("File pattern (glob, e.g., *.cs)")] string filePattern,
        [Description("Regex pattern to search for")] string searchPattern,
        [Description("Maximum files to search. Default: 100")] int maxFiles = 100,
        CancellationToken ct = default)
    {
        var result = await aggregationService.AggregateMatchesAsync(
                directory, filePattern, searchPattern, maxFiles, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
