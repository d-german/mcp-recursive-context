using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Models;

namespace RecursiveContext.Mcp.Server.Services;

/// <summary>
/// Service for advanced pattern analysis operations.
/// </summary>
internal sealed class AdvancedAnalysisService : IAdvancedAnalysisService
{
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;
    private readonly ICompiledRegexCache _regexCache;
    private readonly IFileStreamingService _streamingService;

    public AdvancedAnalysisService(PathResolver pathResolver, IGuardrailService guardrails, ICompiledRegexCache regexCache, IFileStreamingService streamingService)
    {
        _pathResolver = pathResolver;
        _guardrails = guardrails;
        _regexCache = regexCache;
        _streamingService = streamingService;
    }

    public async Task<Result<CompoundMatchResult>> CountCompoundPatternAsync(
        string path, string[] patterns, string matchMode,
        bool includeSamples, int maxSamples, CancellationToken ct)
    {
        var regexResults = CompileMultipleRegex(patterns);
        if (regexResults.IsFailure)
            return Result.Failure<CompoundMatchResult>(regexResults.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<CompoundMatchResult>(callCheck.Error);

        var streamResult = _streamingService.ReadLinesAsync(path, ct);
        if (streamResult.IsFailure)
            return Result.Failure<CompoundMatchResult>(streamResult.Error);

        var regexList = regexResults.Value;
        var matchingLines = new List<(int LineNumber, string Text)>();
        var lineNumber = 0;

        await foreach (var line in streamResult.Value.ConfigureAwait(false))
        {
            lineNumber++;
            if (LineMatchesCompound(line, regexList, matchMode))
            {
                matchingLines.Add((lineNumber, line));
            }
        }

        var samples = includeSamples
            ? matchingLines.Take(maxSamples)
                .Select(m => new CompoundMatchSample(m.LineNumber, m.Text))
                .ToImmutableArray()
            : ImmutableArray<CompoundMatchSample>.Empty;

        return Result.Success(new CompoundMatchResult(
            matchingLines.Count,
            matchMode,
            patterns.ToImmutableArray(),
            samples,
            matchingLines.Count > maxSamples));
    }

    public async Task<Result<ConsecutiveRunResult>> FindConsecutiveRunsAsync(
        string path, string pattern, int minRunLength,
        bool returnLongestOnly, int maxRuns, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<ConsecutiveRunResult>(regexResult.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<ConsecutiveRunResult>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<ConsecutiveRunResult>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var regex = regexResult.Value;

        var runs = FindRuns(lines, regex, minRunLength);
        var longest = runs.OrderByDescending(r => r.Length).FirstOrDefault();

        var resultRuns = returnLongestOnly && longest != null
            ? ImmutableArray.Create(longest)
            : runs.Take(maxRuns).ToImmutableArray();

        return Result.Success(new ConsecutiveRunResult(
            longest,
            resultRuns,
            runs.Count,
            pattern));
    }

    public async Task<Result<PatternAggregateResult>> AggregatePatternMatchesAsync(
        string path, string pattern, string groupBy,
        int topN, bool includeSamples, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<PatternAggregateResult>(regexResult.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<PatternAggregateResult>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<PatternAggregateResult>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var regex = regexResult.Value;

        var groups = new Dictionary<string, (int Count, MatchResult? Sample)>();

        foreach (var (index, line) in lines.Index())
        {
            var matches = regex.Matches(line);
            foreach (Match match in matches)
            {
                var key = ExtractGroupKey(match, groupBy);
                if (string.IsNullOrEmpty(key)) continue;

                if (groups.TryGetValue(key, out var existing))
                {
                    groups[key] = (existing.Count + 1, existing.Sample);
                }
                else
                {
                    var sample = includeSamples
                        ? new MatchResult(index + 1, match.Value, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty)
                        : null;
                    groups[key] = (1, sample);
                }
            }
        }

        var topGroups = groups
            .OrderByDescending(g => g.Value.Count)
            .Take(topN)
            .Select(g => new AggregateGroup(g.Key, g.Value.Count, g.Value.Sample))
            .ToImmutableArray();

        var totalMatches = groups.Values.Sum(g => g.Count);

        return Result.Success(new PatternAggregateResult(
            topGroups,
            totalMatches,
            groups.Count,
            pattern,
            groupBy));
    }

    public async Task<Result<DistributedSampleResult>> SampleMatchesDistributedAsync(
        string path, string pattern, int sampleCount,
        string distribution, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<DistributedSampleResult>(regexResult.Error);

        var pathResult = _pathResolver.ResolveAndValidateExists(path);
        if (pathResult.IsFailure)
            return Result.Failure<DistributedSampleResult>(pathResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<DistributedSampleResult>(callCheck.Error);

        var lines = await File.ReadAllLinesAsync(pathResult.Value, ct).ConfigureAwait(false);
        var regex = regexResult.Value;

        var allMatches = new List<(int LineNumber, string MatchText)>();
        foreach (var (index, line) in lines.Index())
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                allMatches.Add((index + 1, match.Value));
            }
        }

        var selectedIndices = SelectDistributedIndices(allMatches.Count, sampleCount, distribution);
        var samples = selectedIndices
            .Where(i => i < allMatches.Count)
            .Select(i => new DistributedSample(
                allMatches[i].LineNumber,
                lines.Length > 0 ? (double)allMatches[i].LineNumber / lines.Length * 100 : 0,
                allMatches[i].MatchText))
            .ToImmutableArray();

        return Result.Success(new DistributedSampleResult(
            samples,
            allMatches.Count,
            distribution,
            pattern));
    }

    public async Task<Result<CrossFileComparisonResult>> ComparePatternAcrossFilesAsync(
        string[] paths, string pattern, bool computeRatio, CancellationToken ct)
    {
        var regexResult = CompileRegex(pattern);
        if (regexResult.IsFailure)
            return Result.Failure<CrossFileComparisonResult>(regexResult.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<CrossFileComparisonResult>(callCheck.Error);

        var regex = regexResult.Value;
        var entriesBag = new System.Collections.Concurrent.ConcurrentBag<FileComparisonEntry>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(paths, parallelOptions, async (path, token) =>
        {
            var streamResult = _streamingService.ReadLinesAsync(path, token);
            if (streamResult.IsFailure)
            {
                entriesBag.Add(new FileComparisonEntry(path, -1, null));
                return;
            }

            var count = 0;
            await foreach (var line in streamResult.Value.ConfigureAwait(false))
            {
                if (regex.IsMatch(line))
                    count++;
            }
            entriesBag.Add(new FileComparisonEntry(path, count, null));
        }).ConfigureAwait(false);

        // Sort results for deterministic output
        var sortedEntries = entriesBag.OrderBy(e => e.Path).ToList();

        var entriesWithRatio = computeRatio
            ? ComputeRatios(sortedEntries)
            : sortedEntries.ToImmutableArray();

        var summary = GenerateComparisonSummary(entriesWithRatio, pattern);

        return Result.Success(new CrossFileComparisonResult(
            entriesWithRatio,
            pattern,
            summary));
    }


    public async Task<Result<BatchPatternResult>> CountMultiplePatternsAsync(
        string path, string[] patterns, CancellationToken ct)
    {
        var regexResults = CompileMultipleRegex(patterns);
        if (regexResults.IsFailure)
            return Result.Failure<BatchPatternResult>(regexResults.Error);

        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<BatchPatternResult>(callCheck.Error);

        var streamResult = _streamingService.ReadLinesAsync(path, ct);
        if (streamResult.IsFailure)
            return Result.Failure<BatchPatternResult>(streamResult.Error);

        var regexList = regexResults.Value;
        var counts = new int[patterns.Length];
        var totalLines = 0;

        await foreach (var line in streamResult.Value.ConfigureAwait(false))
        {
            totalLines++;
            for (var i = 0; i < regexList.Count; i++)
            {
                if (regexList[i].IsMatch(line))
                    counts[i]++;
            }
        }

        var patternCounts = patterns
            .Select((p, i) => new PatternCount(p, counts[i]))
            .ToImmutableArray();

        return Result.Success(new BatchPatternResult(patternCounts, totalLines, path));
    }

    // ============================================================================
    // Private Static Helper Methods
    // ============================================================================

    private Result<Regex> CompileRegex(string pattern)
    {
        return _regexCache.GetOrCompile(pattern);
    }

    private Result<List<Regex>> CompileMultipleRegex(string[] patterns)
    {
        var regexList = new List<Regex>();
        foreach (var pattern in patterns)
        {
            var result = CompileRegex(pattern);
            if (result.IsFailure)
                return Result.Failure<List<Regex>>(result.Error);
            regexList.Add(result.Value);
        }
        return Result.Success(regexList);
    }

    private static bool LineMatchesCompound(string line, List<Regex> regexList, string matchMode)
    {
        return matchMode.ToLowerInvariant() switch
        {
            "all" => regexList.All(r => r.IsMatch(line)),
            "any" => regexList.Any(r => r.IsMatch(line)),
            "sequence" => MatchesSequence(line, regexList),
            _ => regexList.All(r => r.IsMatch(line)) // default to "all"
        };
    }

    private static bool MatchesSequence(string line, List<Regex> regexList)
    {
        var lastIndex = 0;
        foreach (var regex in regexList)
        {
            var match = regex.Match(line, lastIndex);
            if (!match.Success)
                return false;
            lastIndex = match.Index + match.Length;
        }
        return true;
    }

    private static List<ConsecutiveRun> FindRuns(string[] lines, Regex regex, int minRunLength)
    {
        var runs = new List<ConsecutiveRun>();
        var currentRunStart = -1;
        var currentRunLines = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (regex.IsMatch(lines[i]))
            {
                if (currentRunStart == -1)
                {
                    currentRunStart = i + 1;
                    currentRunLines = new List<string>();
                }
                currentRunLines.Add(lines[i]);
            }
            else
            {
                if (currentRunStart != -1 && currentRunLines.Count >= minRunLength)
                {
                    runs.Add(new ConsecutiveRun(
                        currentRunStart,
                        currentRunLines.Count,
                        currentRunLines.Take(3).ToImmutableArray()));
                }
                currentRunStart = -1;
                currentRunLines.Clear();
            }
        }

        // Handle run at end of file
        if (currentRunStart != -1 && currentRunLines.Count >= minRunLength)
        {
            runs.Add(new ConsecutiveRun(
                currentRunStart,
                currentRunLines.Count,
                currentRunLines.Take(3).ToImmutableArray()));
        }

        return runs;
    }

    private static string ExtractGroupKey(Match match, string groupBy)
    {
        return groupBy.ToLowerInvariant() switch
        {
            "capturegroup1" => match.Groups.Count > 1 ? match.Groups[1].Value : match.Value,
            "firstword" => ExtractFirstWord(match.Value),
            _ => match.Value // "fullMatch" or default
        };
    }

    private static string ExtractFirstWord(string text)
    {
        var trimmed = text.TrimStart();
        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
    }

    private static List<int> SelectDistributedIndices(int totalCount, int sampleCount, string distribution)
    {
        if (totalCount == 0 || sampleCount == 0)
            return new List<int>();

        var count = Math.Min(sampleCount, totalCount);

        return distribution.ToLowerInvariant() switch
        {
            "even" => Enumerable.Range(0, count)
                .Select(i => (int)((double)i / count * totalCount))
                .Distinct()
                .ToList(),
            "random" => Enumerable.Range(0, totalCount)
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .OrderBy(i => i)
                .ToList(),
            "last" => Enumerable.Range(totalCount - count, count).ToList(),
            _ => Enumerable.Range(0, count).ToList() // "first" or default
        };
    }

    private static ImmutableArray<FileComparisonEntry> ComputeRatios(List<FileComparisonEntry> entries)
    {
        var validEntries = entries.Where(e => e.Count >= 0).ToList();
        var avgCount = validEntries.Count > 0 ? validEntries.Average(e => e.Count) : 0;

        return entries
            .Select(e => e.Count >= 0 && avgCount > 0
                ? new FileComparisonEntry(e.Path, e.Count, Math.Round(e.Count / avgCount, 2))
                : e)
            .ToImmutableArray();
    }

    private static string GenerateComparisonSummary(ImmutableArray<FileComparisonEntry> entries, string pattern)
    {
        var validEntries = entries.Where(e => e.Count >= 0).ToList();
        if (validEntries.Count == 0)
            return "No valid files to compare.";

        var max = validEntries.MaxBy(e => e.Count);
        var min = validEntries.MinBy(e => e.Count);

        if (max == null || min == null)
            return $"Pattern '{pattern}' analyzed across {entries.Length} files.";

        if (min.Count > 0)
        {
            var ratio = (double)max.Count / min.Count;
            return $"'{max.Path}' has {ratio:F1}x more matches ({max.Count}) than '{min.Path}' ({min.Count}).";
        }

        return $"'{max.Path}' has the most matches ({max.Count}). '{min.Path}' has {min.Count} matches.";
    }
}
