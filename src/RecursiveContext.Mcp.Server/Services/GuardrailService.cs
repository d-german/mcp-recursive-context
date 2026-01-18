using CSharpFunctionalExtensions; 
using RecursiveContext.Mcp.Server.Config; 
 
namespace RecursiveContext.Mcp.Server.Services; 
 
/// <summary> 
/// Enforces rate limits and guardrails for the MCP session. 
/// Thread-safe using Interlocked operations. 
/// </summary> 
internal sealed class GuardrailService : IGuardrailService 
{ 
    private readonly RlmSettings _settings; 
    private int _callCount; 
 
    public GuardrailService(RlmSettings settings) 
    { 
        _settings = settings; 
    } 
 
    public int MaxToolCallsPerSession => _settings.MaxToolCallsPerSession; 
    public int MaxBytesPerRead => _settings.MaxBytesPerRead; 
    public int RemainingCalls => Math.Max(0, _settings.MaxToolCallsPerSession - _callCount); 
 
    public Result CheckAndIncrementCallCount() 
    { 
        var newCount = Interlocked.Increment(ref _callCount); 
        if (newCount > _settings.MaxToolCallsPerSession) 
        { 
            return Result.Failure( 
                $"Tool call limit exceeded. Max: {_settings.MaxToolCallsPerSession}"); 
        } 
        return Result.Success(); 
    } 
 
    public Result CheckBytesLimit(long bytes) 
    { 
        if (bytes > _settings.MaxBytesPerRead) 
        { 
            return Result.Failure( 
                $"File size {bytes} exceeds max bytes per read {_settings.MaxBytesPerRead}"); 
        } 
        return Result.Success(); 
    } 
 
    public void Reset() 
    { 
        Interlocked.Exchange(ref _callCount, 0); 
    } 
}
