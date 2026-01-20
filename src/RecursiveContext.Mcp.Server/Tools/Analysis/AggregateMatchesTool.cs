using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class AggregateMatchesTool
{
    [McpServerTool(Name = "aggregate_matches")]
    [Description("Count regex pattern matches across files. TIP: For thorough searches, try pattern variations or use multi_pattern_search. Paths are relative to workspace root.")]
    public static async Task<string> AggregateMatches(
        IAggregationService aggregationService,
        [Description("Directory to search in, relative to workspace root. Example: 'src/components' or 'qasper-test-workspace/train'. Use '.' for workspace root.")] string directory,
        [Description("File pattern (glob, e.g., *.txt, *.cs, **/*.json)")] string filePattern,
        [Description("Regex pattern to search for (.NET syntax). Use \\b for word boundaries, (?i) for case-insensitive.")] string searchPattern,
        [Description("Maximum files to search. Default: 1000000")] int maxFiles = 1000000,
        CancellationToken ct = default)
    {
        var result = await aggregationService.AggregateMatchesAsync(
                directory, filePattern, searchPattern, maxFiles, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
