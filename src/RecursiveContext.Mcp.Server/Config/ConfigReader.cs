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
    private const string MaxFilesPerAggregationKey = "RLM_MAX_FILES_PER_AGGREGATION";
    private const string MaxMatchesPerSearchKey = "RLM_MAX_MATCHES_PER_SEARCH";
    private const string MaxChunkSizeKey = "RLM_MAX_CHUNK_SIZE";

    public static RlmSettings ReadSettings()
    {
        return new RlmSettings(
            WorkspaceRoot: GetWorkspaceRoot(),
            MaxBytesPerRead: GetIntOrDefault(MaxBytesPerReadKey, 10_485_760),  // 10 MB
            MaxToolCallsPerSession: GetIntOrDefault(MaxToolCallsKey, 1000),
            TimeoutSeconds: GetIntOrDefault(TimeoutSecondsKey, 120),
            MaxDepth: GetIntOrDefault(MaxDepthKey, 100),
            MaxFilesPerAggregation: GetIntOrDefault(MaxFilesPerAggregationKey, 1_000_000),
            MaxMatchesPerSearch: GetIntOrDefault(MaxMatchesPerSearchKey, 50_000),
            MaxChunkSize: GetIntOrDefault(MaxChunkSizeKey, 1000)
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
