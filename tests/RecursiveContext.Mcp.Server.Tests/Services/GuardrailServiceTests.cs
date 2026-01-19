using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class GuardrailServiceTests
{
    [Fact]
    public void CheckAndIncrementCallCount_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 10, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckAndIncrementCallCount();

        Assert.True(result.IsSuccess);
        Assert.Equal(9, service.RemainingCalls);
    }

    [Fact]
    public void CheckAndIncrementCallCount_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1_000_000, 2, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();
        var result = service.CheckAndIncrementCallCount();

        Assert.True(result.IsFailure);
        Assert.Contains("limit exceeded", result.Error);
    }

    [Fact]
    public void CheckBytesLimit_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckBytesLimit_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(2000);

        Assert.True(result.IsFailure);
        Assert.Contains("exceeds", result.Error);
    }

    [Fact]
    public void CheckBytesLimit_ExactlyAtLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(1000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RemainingCalls_DecreasesWithEachCall()
    {
        var settings = new RlmSettings(".", 1_000_000, 5, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        Assert.Equal(5, service.RemainingCalls);
        service.CheckAndIncrementCallCount();
        Assert.Equal(4, service.RemainingCalls);
        service.CheckAndIncrementCallCount();
        Assert.Equal(3, service.RemainingCalls);
    }

    [Fact]
    public void RemainingCalls_NeverGoesNegative()
    {
        var settings = new RlmSettings(".", 1_000_000, 1, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();

        Assert.Equal(0, service.RemainingCalls);
    }

    [Fact]
    public void MaxToolCallsPerSession_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 1_000_000, 42, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        Assert.Equal(42, service.MaxToolCallsPerSession);
    }

    [Fact]
    public void MaxBytesPerRead_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 12345, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        Assert.Equal(12345, service.MaxBytesPerRead);
    }

    [Fact]
    public void Reset_ResetsCallCount()
    {
        var settings = new RlmSettings(".", 1_000_000, 5, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();
        Assert.Equal(3, service.RemainingCalls);

        service.Reset();

        Assert.Equal(5, service.RemainingCalls);
    }

    #region CheckFilesLimit Tests

    [Fact]
    public void CheckFilesLimit_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckFilesLimit(250);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckFilesLimit_ExactlyAtLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckFilesLimit(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckFilesLimit_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckFilesLimit(501);

        Assert.True(result.IsFailure);
        Assert.Contains("500", result.Error); // Should mention the limit
    }

    [Fact]
    public void MaxFilesPerAggregation_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 777, 10_000, 500);
        var service = new GuardrailService(settings);

        Assert.Equal(777, service.MaxFilesPerAggregation);
    }

    #endregion

    #region CheckMatchesLimit Tests

    [Fact]
    public void CheckMatchesLimit_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckMatchesLimit(5000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckMatchesLimit_ExactlyAtLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckMatchesLimit(10_000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckMatchesLimit_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckMatchesLimit(10_001);

        Assert.True(result.IsFailure);
        Assert.Contains("10000", result.Error); // Should mention the limit
    }

    [Fact]
    public void MaxMatchesPerSearch_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 8888, 500);
        var service = new GuardrailService(settings);

        Assert.Equal(8888, service.MaxMatchesPerSearch);
    }

    #endregion

    #region CheckChunkSize Tests

    [Fact]
    public void CheckChunkSize_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckChunkSize(250);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckChunkSize_ExactlyAtLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckChunkSize(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckChunkSize_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckChunkSize(501);

        Assert.True(result.IsFailure);
        Assert.Contains("500", result.Error); // Should mention the limit
    }

    [Fact]
    public void CheckChunkSize_ZeroValue_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 500);
        var service = new GuardrailService(settings);

        var result = service.CheckChunkSize(0);

        // Zero is technically valid (under limit)
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void MaxChunkSize_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 1_000_000, 100, 30, 20, 500, 10_000, 999);
        var service = new GuardrailService(settings);

        Assert.Equal(999, service.MaxChunkSize);
    }

    #endregion
}
