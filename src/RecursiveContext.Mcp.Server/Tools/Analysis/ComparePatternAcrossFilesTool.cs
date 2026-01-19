using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

/// <summary>
/// MCP tool for comparing pattern match counts across multiple files.
/// </summary>
[McpServerToolType]
internal static class ComparePatternAcrossFilesTool
{
    [McpServerTool(Name = "compare_pattern_across_files")]
    [Description("Compare pattern match counts across multiple files. Useful for comparing word usage or content patterns between documents. Returns counts, optional ratios, and a comparison summary.")]
    public static async Task<string> ComparePatternAcrossFiles(
        IAdvancedAnalysisService analysisService,
        [Description("Array of file paths to compare")] string[] paths,
        [Description("Regex pattern to count (.NET syntax)")] string pattern,
        [Description("When true, compute relative ratios (each file's count divided by average). Default: false")] bool computeRatio = false,
        CancellationToken ct = default)
    {
        var result = await analysisService.ComparePatternAcrossFilesAsync(
            paths, pattern, computeRatio, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
