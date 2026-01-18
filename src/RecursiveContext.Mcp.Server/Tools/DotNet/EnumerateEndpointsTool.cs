using System.Collections.Immutable; 
using System.ComponentModel; 
using System.Text.RegularExpressions; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Config; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Tools.DotNet; 
 
[McpServerToolType] 
internal static class EnumerateEndpointsTool 
{ 
    private static readonly Regex HttpAttributeRegex = new( 
        @"\[Http^(Get^|Post^|Put^|Delete^|Patch^)\s*^(\(.*?\)^)?\]", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase); 
 
    [McpServerTool(Name = "enumerate_endpoints")] 
    [Description("Finds HTTP endpoint attributes in controller files.")] 
    public static async Task<string> EnumerateEndpoints( 
        IPatternMatchingService patternService, 
        IFileSystemService fileService, 
        [Description("Maximum controllers to scan. Default: 20")] int maxControllers = 20, 
        CancellationToken ct = default) 
    { 
        var controllersResult = await patternService.FindFilesAsync("**/*Controller.cs", maxControllers, ct); 
        if (controllersResult.IsFailure) 
            return ToolResponseFormatter.FormatError(controllersResult.Error); 
 
        var endpoints = new List<EndpointInfo>(); 
        foreach (var path in controllersResult.Value.MatchingPaths) 
        { 
            var contentResult = await fileService.ReadFileAsync(path, ct); 
            if (contentResult.IsFailure) continue; 
 
            var matches = HttpAttributeRegex.Matches(contentResult.Value); 
            foreach (Match match in matches) 
            { 
                endpoints.Add(new EndpointInfo(path, match.Value)); 
            } 
        } 
 
        return ToolResponseFormatter.FormatSuccess(endpoints.ToImmutableArray()); 
    } 
 
    private sealed record EndpointInfo(string FilePath, string Attribute); 
}
