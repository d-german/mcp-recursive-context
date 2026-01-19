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
    private readonly ICompiledRegexCache _regexCache;
    private readonly IFileStreamingService _streamingService;

    public ContentAnalysisService(PathResolver pathResolver, IGuardrailService guardrails, ICompiledRegexCache regexCache, IFileStreamingService streamingService)
    {
        _pathResolver = pathResolver;
        _guardrails = guardrails;
        _regexCache = regexCache;
        _streamingService = streamingService;
    }

    public async Task<Result<MatchCountResult>> CountPatternMatchesAsync(
        string path, string pattern, int maxResults,
        bool countUniqueLinesOnly, bool includeSamples, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<MatchCountResult>(regexResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<MatchCountResult>(callCheck.Error);

        var regex = regexResult.Value;

        // Use streaming for unique line counting (most efficient path)
        if (countUniqueLinesOnly)
        {
            var streamResult = _streamingService.ReadLinesAsync(path, ct);
            if (streamResult.IsFailure)
                return Result.Failure<MatchCountResult>(streamResult.Error);

            var lineCount = 0;
            await foreach (var line in streamResult.Value.ConfigureAwait(false))
            {
                if (regex.IsMatch(line))
                    lineCount++;
            }
            return Result.Success(new MatchCountResult(lineCount, ImmutableArray<MatchResult>.Empty, false));
        }

        // For sample collection, need to load file (requires random access for context)
        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<MatchCountResult>(pathResult.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);

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
        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<int>(callCheck.Error);

        var streamResult = _streamingService.ReadLinesAsync(path, ct);
        if (streamResult.IsFailure)
            return Result.Failure<int>(streamResult.Error);

        var count = 0;
        await foreach (var _ in streamResult.Value.ConfigureAwait(false))
        {
            count++;
        }
        
        return Result.Success(count);
    }

    private Result<Regex> CompileRegex(string pattern)
    {
        return _regexCache.GetOrCompile(pattern);
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
