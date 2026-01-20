using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Server;
 
[McpServerToolType] 
internal static class GetServerInfoTool 
{ 
    [McpServerTool(Name = "get_server_info")] 
    [Description("Returns server metadata including the WORKSPACE ROOT path. IMPORTANT: Call this first to understand the base path - all other tool paths are relative to this workspace root.")] 
    public static string GetServerInfo( 
        ServerMetadata metadata, 
        RlmSettings settings,
        IGuardrailService guardrails) 
    { 
        var info = new 
        { 
            ServerName = metadata.Name, 
            ServerVersion = metadata.Version,
            WorkspaceRoot = settings.WorkspaceRoot,
            PathNote = "All paths in other tools are relative to WorkspaceRoot. Use list_directories with path='.' to explore.",
            MaxBytesPerRead = guardrails.MaxBytesPerRead, 
            MaxToolCallsPerSession = guardrails.MaxToolCallsPerSession, 
            RemainingToolCalls = guardrails.RemainingCalls 
        }; 
        return ToolResponseFormatter.FormatSuccess(info); 
    } 
}
