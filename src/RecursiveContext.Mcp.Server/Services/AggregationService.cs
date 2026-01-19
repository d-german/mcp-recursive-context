using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using RecursiveContext.Mcp.Server.Config;
using RecursiveContext.Mcp.Server.Models;

namespace RecursiveContext.Mcp.Server.Services;

/// <summary>
/// Service for aggregating analysis results across multiple files.
/// </summary>
internal sealed class AggregationService : IAggregationService
{
    private readonly PathResolver _pathResolver;
    private readonly IGuardrailService _guardrails;
    private readonly IPatternMatchingService _patternService;

    public AggregationService(
        PathResolver pathResolver,
        IGuardrailService guardrails,
        IPatternMatchingService patternService)
    {
        _pathResolver = pathResolver;
        _guardrails = guardrails;
        _patternService = patternService;
    }

    public async Task<Result<AggregateResult>> AggregateMatchesAsync(
        string directory, string filePattern, string searchPattern,
        int maxFiles, CancellationToken ct)
    {
        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Result.Failure<AggregateResult>(callCheck.Error);

        // Compile regex first to fail fast
        Regex regex;
        try
        {
            regex = new Regex(searchPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AggregateResult>($"Invalid regex pattern: {ex.Message}");
        }

        // Build glob pattern with directory
        var globPattern = string.IsNullOrEmpty(directory) || directory == "."
            ? $"**/{filePattern}"
            : $"{directory}/**/{filePattern}";

        // Find matching files
        var filesResult = await _patternService.FindFilesAsync(globPattern, maxFiles, ct)
            .ConfigureAwait(false);
        if (filesResult.IsFailure)
            return Result.Failure<AggregateResult>(filesResult.Error);

        var filePaths = filesResult.Value.MatchingPaths;
        var effectiveMax = Math.Min(maxFiles, _guardrails.MaxFilesPerAggregation);

        var filesCheck = _guardrails.CheckFilesLimit(filePaths.Length);
        if (filesCheck.IsFailure)
        {
            filePaths = filePaths.Take(effectiveMax).ToImmutableArray();
        }

        // Count matches in each file using parallel processing
        var matchesByFile = new System.Collections.Concurrent.ConcurrentBag<FileMatchCount>();
        var totalMatches = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(filePaths, parallelOptions, async (relativePath, token) =>
        {
            var pathResult = _pathResolver.ResolveAndValidateExists(relativePath);
            if (pathResult.IsFailure)
                return;

            try
            {
                var content = await File.ReadAllTextAsync(pathResult.Value, token).ConfigureAwait(false);
                var matches = regex.Matches(content);
                var count = matches.Count;

                if (count > 0)
                {
                    matchesByFile.Add(new FileMatchCount(relativePath, count));
                    Interlocked.Add(ref totalMatches, count);
                }
            }
            catch (IOException)
            {
                // Skip files that can't be read
            }
        }).ConfigureAwait(false);

        // Sort results for deterministic output
        var sortedMatches = matchesByFile.OrderBy(m => m.Path).ToImmutableArray();

        return Result.Success(new AggregateResult(
            filePaths.Length,
            totalMatches,
            sortedMatches));
    }

    public Task<Result<int>> CountFilesAsync(
        string directory, string pattern, bool recursive, CancellationToken ct)
    {
        var callCheck = _guardrails.CheckAndIncrementCallCount();
        if (callCheck.IsFailure)
            return Task.FromResult(Result.Failure<int>(callCheck.Error));

        var pathResult = _pathResolver.ResolveAndValidateExists(directory);
        if (pathResult.IsFailure)
            return Task.FromResult(Result.Failure<int>(pathResult.Error));

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        try
        {
            var count = Directory.EnumerateFiles(pathResult.Value, pattern, searchOption).Count();
            return Task.FromResult(Result.Success(count));
        }
        catch (IOException ex)
        {
            return Task.FromResult(Result.Failure<int>($"Error counting files: {ex.Message}"));
        }
    }
}
