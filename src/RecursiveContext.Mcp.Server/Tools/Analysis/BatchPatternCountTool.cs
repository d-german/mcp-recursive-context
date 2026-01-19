using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class BatchPatternCountTool
{
    [McpServerTool(Name = "batch_pattern_count")]
    [Description("Count multiple regex patterns in a single file pass. Much more efficient than calling count_pattern_matches multiple times. Returns count for each pattern.")]
    public static async Task<string> BatchPatternCount(
        IAdvancedAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Array of regex patterns to count")] string[] patterns,
        CancellationToken ct = default)
    {
        var result = await analysisService.CountMultiplePatternsAsync(path, patterns, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
