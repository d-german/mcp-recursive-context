using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Services;
using RecursiveContext.Mcp.Server.Tools;

namespace RecursiveContext.Mcp.Server.Tools.Analysis;

[McpServerToolType]
internal static class ReadChunkByIndexTool
{
    [McpServerTool(Name = "read_chunk_by_index")]
    [Description("Read a specific chunk from a file by index. Use with get_chunk_info for systematic file traversal.")]
    public static async Task<string> ReadChunkByIndex(
        IChunkingService chunkingService,
        [Description("File path to read from")] string path,
        [Description("Zero-based chunk index")] int chunkIndex,
        [Description("Lines per chunk. Must match get_chunk_info. Default: 50")] int chunkSizeLines = 50,
        CancellationToken ct = default)
    {
        var result = await chunkingService.ReadChunkAsync(path, chunkIndex, chunkSizeLines, ct)
            .ConfigureAwait(false);
        return ToolResponseFormatter.FormatResult(result);
    }
}
