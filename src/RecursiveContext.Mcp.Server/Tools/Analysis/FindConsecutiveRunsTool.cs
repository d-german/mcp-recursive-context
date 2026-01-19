using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

/// <summary>
/// MCP tool for finding runs of consecutive lines matching a pattern.
/// </summary>
[McpServerToolType]
internal static class FindConsecutiveRunsTool
{
    [McpServerTool(Name = "find_consecutive_runs")]
    [Description("Find runs of consecutive lines matching a regex pattern. Useful for finding longest speeches, repeated sections, or blocks of similar content. Uses .NET regex syntax.")]
    public static async Task<string> FindConsecutiveRuns(
        IAdvancedAnalysisService analysisService,
        [Description("File path to search")] string path,
        [Description("Regex pattern to match (.NET syntax)")] string pattern,
        [Description("Minimum consecutive matching lines to form a run. Default: 2")] int minRunLength = 2,
        [Description("When true (default), only return the longest run. When false, return all runs up to maxRuns.")] bool returnLongestOnly = true,
        [Description("Maximum number of runs to return when returnLongestOnly is false. Default: 10")] int maxRuns = 10,
        CancellationToken ct = default)
    {
        var result = await analysisService.FindConsecutiveRunsAsync(
            path, pattern, minRunLength, returnLongestOnly, maxRuns, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
