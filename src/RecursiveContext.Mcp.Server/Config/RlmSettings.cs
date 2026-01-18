namespace RecursiveContext.Mcp.Server.Config; 
 
/// <summary> 
/// Immutable configuration settings for the RLM server. 
/// All guardrail limits and workspace configuration. 
/// </summary> 
internal sealed record RlmSettings( 
    string WorkspaceRoot, 
    int MaxBytesPerRead, 
    int MaxToolCallsPerSession, 
    int TimeoutSeconds, 
    int MaxDepth 
) 
{ 
    public static RlmSettings Default { get; } = new( 
        WorkspaceRoot: Directory.GetCurrentDirectory(), 
        MaxBytesPerRead: 1_048_576,  // 1 MB 
        MaxToolCallsPerSession: 1000, 
        TimeoutSeconds: 30, 
        MaxDepth: 20 
    ); 
}
