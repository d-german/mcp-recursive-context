using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

/// <summary>
/// MCP tool for getting diverse sample matches distributed across a file.
/// </summary>
[McpServerToolType]
internal static class SampleMatchesDistributedTool
{
    [McpServerTool(Name = "sample_matches_distributed")]
    [Description("Get diverse sample matches spread across a file. TIP: Run multiple times with different patterns for comprehensive coverage. Path is relative to workspace root.")]
    public static async Task<string> SampleMatchesDistributed(
        IAdvancedAnalysisService analysisService,
        [Description("Full file path relative to workspace root.")] string path,
        [Description("Regex pattern to match (.NET syntax)")] string pattern,
        [Description("Number of samples to return. Default: 5")] int sampleCount = 5,
        [Description("Distribution mode: 'even' (evenly spaced), 'random', 'first' (first N), 'last' (last N). Default: even")] string distribution = "even",
        CancellationToken ct = default)
    {
        var result = await analysisService.SampleMatchesDistributedAsync(
            path, pattern, sampleCount, distribution, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
