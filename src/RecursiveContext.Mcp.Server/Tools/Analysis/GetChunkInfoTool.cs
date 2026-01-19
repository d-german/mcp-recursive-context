using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class GetChunkInfoTool
{
    [McpServerTool(Name = "get_chunk_info")]
    [Description("Get chunk information for a file. Returns total lines, chunk count, and exact boundaries for systematic traversal.")]
    public static async Task<string> GetChunkInfo(
        IChunkingService chunkingService,
        [Description("File path to analyze")] string path,
        [Description("Lines per chunk. Default: 50")] int chunkSizeLines = 50,
        CancellationToken ct = default)
    {
        var result = await chunkingService.GetChunkInfoAsync(path, chunkSizeLines, ct).ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
