using RecursiveContext.Mcp.Server.Services.Caching;

namespace RecursiveContext.Mcp.Server.Tests.Services.Caching;

/// <summary>
/// Unit tests for CompiledRegexCache.
/// </summary>
public class CompiledRegexCacheTests
{
    private readonly CompiledRegexCache _cache = new();

    [Fact]
    public void GetOrCompile_ValidPattern_ReturnsSuccess()
    {
        // Act
        var result = _cache.GetOrCompile(@"\w+");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void GetOrCompile_InvalidPattern_ReturnsFailure()
    {
        // Act
        var result = _cache.GetOrCompile(@"[invalid");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid regex pattern", result.Error);
    }

    [Fact]
    public void GetOrCompile_SamePattern_ReturnsCachedInstance()
    {
        // Arrange
        const string pattern = @"test\d+";

        // Act
        var result1 = _cache.GetOrCompile(pattern);
        var result2 = _cache.GetOrCompile(pattern);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Same(result1.Value, result2.Value); // Same instance = cached
    }

    [Fact]
    public void GetOrCompile_DifferentPatterns_ReturnsDifferentInstances()
    {
        // Arrange
        const string pattern1 = @"pattern1";
        const string pattern2 = @"pattern2";

        // Act
        var result1 = _cache.GetOrCompile(pattern1);
        var result2 = _cache.GetOrCompile(pattern2);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotSame(result1.Value, result2.Value);
    }

    [Fact]
    public void GetOrCompile_InvalidPatternCached_ReturnsSameFailure()
    {
        // Arrange
        const string pattern = @"[invalid";

        // Act
        var result1 = _cache.GetOrCompile(pattern);
        var result2 = _cache.GetOrCompile(pattern);

        // Assert
        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.Equal(result1.Error, result2.Error);
    }

    [Fact]
    public void GetOrCompile_ThreadSafe_ConcurrentAccess()
    {
        // Arrange
        const string pattern = @"concurrent\d+";
        const int threadCount = 10;
        var results = new CSharpFunctionalExtensions.Result<System.Text.RegularExpressions.Regex>[threadCount];

        // Act
        Parallel.For(0, threadCount, i =>
        {
            results[i] = _cache.GetOrCompile(pattern);
        });

        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccess));
        
        // All should return the same cached instance
        var firstRegex = results[0].Value;
        Assert.All(results, r => Assert.Same(firstRegex, r.Value));
    }
}
