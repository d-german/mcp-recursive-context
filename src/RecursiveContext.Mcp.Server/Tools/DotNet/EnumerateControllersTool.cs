using System.ComponentModel; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.DotNet; 
 
[McpServerToolType] 
internal static class EnumerateControllersTool 
{ 
    [McpServerTool(Name = "enumerate_controllers")] 
    [Description("Finds ASP.NET controller files by scanning for *Controller.cs files.")] 
    public static async Task<string> EnumerateControllers( 
        IPatternMatchingService patternService, 
        [Description("Maximum results. Default: 50")] int maxResults = 50, 
        CancellationToken ct = default) 
    { 
        var result = await patternService.FindFilesAsync("**/*Controller.cs", maxResults, ct).ConfigureAwait(false); 
        return ToolResponseFormatter.FormatResult(result); 
    } 
}
