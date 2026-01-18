namespace RecursiveContext.Mcp.Server.Config; 
 
/// <summary> 
/// Reads configuration from environment variables with sensible defaults. 
/// All methods are static and pure - no side effects. 
/// </summary> 
internal static class ConfigReader 
{ 
    private const string WorkspaceRootKey = "RLM_WORKSPACE_ROOT"; 
    private const string MaxBytesPerReadKey = "RLM_MAX_BYTES_PER_READ"; 
    private const string MaxToolCallsKey = "RLM_MAX_TOOL_CALLS"; 
    private const string TimeoutSecondsKey = "RLM_TIMEOUT_SECONDS"; 
    private const string MaxDepthKey = "RLM_MAX_DEPTH"; 
 
    public static RlmSettings ReadSettings() 
    { 
        return new RlmSettings( 
            WorkspaceRoot: GetWorkspaceRoot(), 
            MaxBytesPerRead: GetIntOrDefault(MaxBytesPerReadKey, 1_048_576), 
            MaxToolCallsPerSession: GetIntOrDefault(MaxToolCallsKey, 1000), 
            TimeoutSeconds: GetIntOrDefault(TimeoutSecondsKey, 30), 
            MaxDepth: GetIntOrDefault(MaxDepthKey, 20) 
        ); 
    } 
 
    private static string GetWorkspaceRoot() 
    { 
        var envValue = Environment.GetEnvironmentVariable(WorkspaceRootKey); 
        return string.IsNullOrWhiteSpace(envValue) 
            ? Directory.GetCurrentDirectory() 
            : envValue; 
    } 
 
    private static int GetIntOrDefault(string key, int defaultValue) 
    { 
        var envValue = Environment.GetEnvironmentVariable(key); 
        return int.TryParse(envValue, out var value) && value > 0 
            ? value 
            : defaultValue; 
    } 
}
