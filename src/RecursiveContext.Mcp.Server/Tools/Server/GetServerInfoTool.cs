using System.ComponentModel;
using ModelContextProtocol.Server;
using RecursiveContext.Mcp.Server.Server;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tools.Server;
 
[McpServerToolType] 
internal static class GetServerInfoTool 
{ 
    [McpServerTool(Name = "get_server_info")] 
    [Description("Returns server metadata, capabilities, and current guardrail status.")] 
    public static string GetServerInfo( 
        ServerMetadata metadata, 
        IGuardrailService guardrails) 
    { 
        var info = new 
        { 
            ServerName = metadata.Name, 
            ServerVersion = metadata.Version, 
            MaxBytesPerRead = guardrails.MaxBytesPerRead, 
            MaxToolCallsPerSession = guardrails.MaxToolCallsPerSession, 
            RemainingToolCalls = guardrails.RemainingCalls 
        }; 
        return ToolResponseFormatter.FormatSuccess(info); 
    } 
}
