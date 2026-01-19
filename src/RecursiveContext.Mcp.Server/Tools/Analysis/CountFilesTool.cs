using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class CountFilesTool
{
    [McpServerTool(Name = "count_files")]
    [Description("Count files matching a pattern. Use to check scope before enumerating files.")]
    public static async Task<string> CountFiles(
        IAggregationService aggregationService,
        [Description("Directory to search in")] string directory,
        [Description("File pattern (e.g., *.cs, *.json). Default: *")] string pattern = "*",
        [Description("Search subdirectories recursively. Default: true")] bool recursive = true,
        CancellationToken ct = default)
    {
        var result = await aggregationService.CountFilesAsync(directory, pattern, recursive, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
