using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace RecursiveContext.Mcp.Server.Services.Caching;

/// <summary>
/// Thread-safe cache for compiled regex patterns.
/// </summary>
public sealed class CompiledRegexCache : ICompiledRegexCache
{
    private readonly ConcurrentDictionary<string, Result<Regex>> _cache = new();
    
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public Result<Regex> GetOrCompile(string pattern)
    {
        return _cache.GetOrAdd(pattern, CompileRegex);
    }

    /// <summary>
    /// Compiles a regex pattern with standard options.
    /// </summary>
    /// <param name="pattern">The regex pattern string.</param>
    /// <returns>Success with compiled Regex, or Failure if pattern is invalid.</returns>
    private static Result<Regex> CompileRegex(string pattern)
    {
        try
        {
            var regex = new Regex(
                pattern, 
                RegexOptions.Compiled | RegexOptions.Multiline, 
                RegexTimeout);
            return Result.Success(regex);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Regex>($"Invalid regex pattern: {ex.Message}");
        }
    }
}
