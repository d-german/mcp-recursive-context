using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class CountFilesTool
{
    [McpServerTool(Name = "count_files")]
    [Description("Count files matching a pattern. Use to check scope before enumerating files. IMPORTANT: Directory path is relative to workspace root (RLM_WORKSPACE_ROOT), not the current shell directory.")]
    public static async Task<string> CountFiles(
        IAggregationService aggregationService,
        [Description("Directory to search in, relative to workspace root. Example: 'src/data' or 'qasper-test-workspace/train'. Use '.' for workspace root.")] string directory,
        [Description("File pattern (e.g., *.cs, *.json, *.txt). Default: *")] string pattern = "*",
        [Description("Search subdirectories recursively. Default: true")] bool recursive = true,
        CancellationToken ct = default)
    {
        var result = await aggregationService.CountFilesAsync(directory, pattern, recursive, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
