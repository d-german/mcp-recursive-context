using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Services;

namespace RecursiveContext.Mcp.Server.Tests.Services;

public class GuardrailServiceTests
{
    [Fact]
    public void CheckAndIncrementCallCount_UnderLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1_000_000, 10, 30, 20);
        var service = new GuardrailService(settings);

        var result = service.CheckAndIncrementCallCount();

        Assert.True(result.IsSuccess);
        Assert.Equal(9, service.RemainingCalls);
    }

    [Fact]
    public void CheckAndIncrementCallCount_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1_000_000, 2, 30, 20);
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
        var settings = new RlmSettings(".", 1000, 100, 30, 20);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckBytesLimit_ExceedsLimit_ReturnsFailure()
    {
        var settings = new RlmSettings(".", 1000, 100, 30, 20);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(2000);

        Assert.True(result.IsFailure);
        Assert.Contains("exceeds", result.Error);
    }

    [Fact]
    public void CheckBytesLimit_ExactlyAtLimit_ReturnsSuccess()
    {
        var settings = new RlmSettings(".", 1000, 100, 30, 20);
        var service = new GuardrailService(settings);

        var result = service.CheckBytesLimit(1000);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RemainingCalls_DecreasesWithEachCall()
    {
        var settings = new RlmSettings(".", 1_000_000, 5, 30, 20);
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
        var settings = new RlmSettings(".", 1_000_000, 1, 30, 20);
        var service = new GuardrailService(settings);

        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();

        Assert.Equal(0, service.RemainingCalls);
    }

    [Fact]
    public void MaxToolCallsPerSession_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 1_000_000, 42, 30, 20);
        var service = new GuardrailService(settings);

        Assert.Equal(42, service.MaxToolCallsPerSession);
    }

    [Fact]
    public void MaxBytesPerRead_ReturnsConfiguredValue()
    {
        var settings = new RlmSettings(".", 12345, 100, 30, 20);
        var service = new GuardrailService(settings);

        Assert.Equal(12345, service.MaxBytesPerRead);
    }

    [Fact]
    public void Reset_ResetsCallCount()
    {
        var settings = new RlmSettings(".", 1_000_000, 5, 30, 20);
        var service = new GuardrailService(settings);

        service.CheckAndIncrementCallCount();
        service.CheckAndIncrementCallCount();
        Assert.Equal(3, service.RemainingCalls);

        service.Reset();

        Assert.Equal(5, service.RemainingCalls);
    }
}
