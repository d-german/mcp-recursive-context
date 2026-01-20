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
    [Description("Get diverse sample matches spread across a single file. Useful for getting examples from different sections of large documents. IMPORTANT: The 'path' must be the full path relative to workspace root, not just the filename.")]
    public static async Task<string> SampleMatchesDistributed(
        IAdvancedAnalysisService analysisService,
        [Description("Full file path relative to workspace root. Example: 'project/data/train/file.txt'. NOT just 'file.txt'.")] string path,
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
