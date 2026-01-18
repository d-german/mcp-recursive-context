using Microsoft.Extensions.Logging; 
 
namespace RecursiveContext.Mcp.Server.Server; 
 
internal static class LoggingConfiguration 
{ 
    private const string LogLevelEnvVar = "RLM_LOG_LEVEL"; 
 
    public static void Configure(ILoggingBuilder loggingBuilder) 
    { 
        loggingBuilder.ClearProviders(); 
        loggingBuilder.AddConsole(options => 
        { 
            // MCP servers must log to stderr, not stdout 
            options.LogToStandardErrorThreshold = LogLevel.Trace; 
        }); 
 
        var logLevel = GetLogLevelFromEnvironment(); 
        loggingBuilder.SetMinimumLevel(logLevel); 
    } 
 
    private static LogLevel GetLogLevelFromEnvironment() 
    { 
        var envValue = Environment.GetEnvironmentVariable(LogLevelEnvVar); 
        if (Enum.TryParse<LogLevel>(envValue, ignoreCase: true, out var level)) 
        { 
            return level; 
        } 
        return LogLevel.Information; 
    } 
}
