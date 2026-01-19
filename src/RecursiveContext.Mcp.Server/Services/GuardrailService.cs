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
    public int MaxFilesPerAggregation => _settings.MaxFilesPerAggregation;
    public int MaxMatchesPerSearch => _settings.MaxMatchesPerSearch;
    public int MaxChunkSize => _settings.MaxChunkSize;
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

    public Result CheckFilesLimit(int fileCount)
    {
        if (fileCount > _settings.MaxFilesPerAggregation)
        {
            return Result.Failure(
                $"File count {fileCount} exceeds max files per aggregation {_settings.MaxFilesPerAggregation}");
        }
        return Result.Success();
    }

    public Result CheckMatchesLimit(int matchCount)
    {
        if (matchCount > _settings.MaxMatchesPerSearch)
        {
            return Result.Failure(
                $"Match count {matchCount} exceeds max matches per search {_settings.MaxMatchesPerSearch}");
        }
        return Result.Success();
    }

    public Result CheckChunkSize(int chunkSize)
    {
        if (chunkSize > _settings.MaxChunkSize)
        {
            return Result.Failure(
                $"Chunk size {chunkSize} exceeds max chunk size {_settings.MaxChunkSize}");
        }
        return Result.Success();
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _callCount, 0);
    }
}
