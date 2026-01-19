using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class CountLinesTool
{
    [McpServerTool(Name = "count_lines")]
    [Description("Count total lines in a file. Use to determine file size before deciding on processing strategy.")]
    public static async Task<string> CountLines(
        IContentAnalysisService analysisService,
        [Description("File path to count lines in")] string path,
        CancellationToken ct = default)
    {
        var result = await analysisService.CountLinesAsync(path, ct).ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
