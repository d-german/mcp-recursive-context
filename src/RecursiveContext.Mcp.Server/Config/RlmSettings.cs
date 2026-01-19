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
    int MaxDepth,
    int MaxFilesPerAggregation,
    int MaxMatchesPerSearch,
    int MaxChunkSize
)
{
    public static RlmSettings Default { get; } = new(
        WorkspaceRoot: Directory.GetCurrentDirectory(),
        MaxBytesPerRead: 10_485_760,  // 10 MB
        MaxToolCallsPerSession: 1000,
        TimeoutSeconds: 120,
        MaxDepth: 100,
        MaxFilesPerAggregation: 10_000,
        MaxMatchesPerSearch: 50_000,
        MaxChunkSize: 1000  // lines
    );
}
