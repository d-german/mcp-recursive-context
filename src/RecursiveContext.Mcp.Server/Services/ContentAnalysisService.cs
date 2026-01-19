using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Models;

namespace RecursiveContext.Mcp.Server.Services;

/// <summary>
/// Service for content analysis operations (pattern matching, line counting).
/// </summary>
internal sealed class ContentAnalysisService : IContentAnalysisService
{
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;

    public ContentAnalysisService(PathResolver pathResolver, IGuardrailService guardrails)
    {
        _pathResolver = pathResolver;
        _guardrails = guardrails;
    }

    public async Task<Result<MatchCountResult>> CountPatternMatchesAsync(
        string path, string pattern, int maxResults,
        bool countUniqueLinesOnly, bool includeSamples, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<MatchCountResult>(regexResult.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<MatchCountResult>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<MatchCountResult>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var regex = regexResult.Value;

        // Count unique lines containing pattern (grep -c behavior) vs total match occurrences
        if (countUniqueLinesOnly)
        {
            var lineCount = lines.Count(line => regex.IsMatch(line));
            return Result.Success(new MatchCountResult(lineCount, ImmutableArray<MatchResult>.Empty, false));
        }

        // Count all match occurrences (original behavior)
        var matches = lines
            .Index()
            .SelectMany(item => regex.Matches(item.Item2)
                .Cast<Match>()
                .Select(m => new MatchResult(
                    item.Item1 + 1,  // 1-based line numbers
                    m.Value,
                    ImmutableArray<string>.Empty,
                    ImmutableArray<string>.Empty)))
            .ToList();

        var matchCheck = _guardrails.CheckMatchesLimit(matches.Count);
        var truncated = matchCheck.IsFailure;
        var effectiveMax = Math.Min(maxResults, _guardrails.MaxMatchesPerSearch);

        var samples = includeSamples
            ? matches.Take(effectiveMax).ToImmutableArray()
            : ImmutableArray<MatchResult>.Empty;

        return Result.Success(new MatchCountResult(matches.Count, samples, truncated));
    }

    public async Task<Result<IReadOnlyList<MatchResult>>> SearchWithContextAsync(
        string path, string pattern, int contextLines, int maxResults, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<IReadOnlyList<MatchResult>>(regexResult.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<IReadOnlyList<MatchResult>>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<IReadOnlyList<MatchResult>>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var regex = regexResult.Value;
        var results = new List<MatchResult>();

        foreach (var (index, line) in lines.Index())
        {
            if (results.Count >= maxResults)
                break;

            var match = regex.Match(line);
            if (!match.Success)
                continue;

            var contextBefore = GetContextLines(lines, index, -contextLines);
            var contextAfter = GetContextLines(lines, index, contextLines);

            results.Add(new MatchResult(
                index + 1,  // 1-based line numbers
                match.Value,
                contextBefore,
                contextAfter));
        }

        return Result.Success<IReadOnlyList<MatchResult>>(results);
    }

    public async Task<Result<int>> CountLinesAsync(string path, CancellationToken ct)
    {
        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<int>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<int>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        return Result.Success(lines.Length);
    }

    private static Result<Regex> CompileRegex(string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline, TimeSpan.FromSeconds(5));
            return Result.Success(regex);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Regex>($"Invalid regex pattern: {ex.Message}");
        }
    }

    private static ImmutableArray<string> GetContextLines(string[] lines, int currentIndex, int count)
    {
        if (count == 0)
            return ImmutableArray<string>.Empty;

        var start = count < 0 ? Math.Max(0, currentIndex + count) : currentIndex + 1;
        var end = count < 0 ? currentIndex : Math.Min(lines.Length, currentIndex + 1 + count);
        var length = end - start;

        if (length <= 0)
            return ImmutableArray<string>.Empty;

        return lines.Skip(start).Take(length).ToImmutableArray();
    }
}
