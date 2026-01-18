using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.Metadata; 
 
[McpServerToolType] 
internal static class GetContextInfoTool 
{ 
    [McpServerTool(Name = "get_context_info")] 
    [Description("Returns workspace metadata: file counts, total size, extension distribution.")] 
    public static async Task<string> GetContextInfo( 
        IContextMetadataService metadataService, 
        [Description("Maximum recursion depth. Default: 10")] int maxDepth = 10, 
        CancellationToken ct = default) 
    { 
        var result = await metadataService.GetContextInfoAsync(maxDepth, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
