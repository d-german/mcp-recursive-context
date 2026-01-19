using System.Collections.Immutable;

namespace RecursiveContext.Mcp.Server.Models;

// ============================================================================
// Advanced Analysis Tool Models
// ============================================================================

/// <summary>
/// Result of counting lines matching multiple compound patterns.
/// </summary>
public sealed record CompoundMatchResult(
    int MatchingLineCount,
    string MatchMode,
    ImmutableArray<string> Patterns,
    ImmutableArray<CompoundMatchSample> Samples,
    bool Truncated
);

/// <summary>
/// A sample line that matched the compound pattern criteria.
/// </summary>
public sealed record CompoundMatchSample(
    int LineNumber,
    string LineText
);

/// <summary>
/// Represents a run of consecutive lines matching a pattern.
/// </summary>
public sealed record ConsecutiveRun(
    int StartLine,
    int Length,
    ImmutableArray<string> SampleLines
);

/// <summary>
/// Result of finding consecutive runs of matching lines.
/// </summary>
public sealed record ConsecutiveRunResult(
    ConsecutiveRun? LongestRun,
    ImmutableArray<ConsecutiveRun> AllRuns,
    int TotalRunsFound,
    string Pattern
);

/// <summary>
/// A group of pattern matches aggregated by a key.
/// </summary>
public sealed record AggregateGroup(
    string Key,
    int Count,
    MatchResult? Sample
);

/// <summary>
/// Result of aggregating pattern matches by groups.
/// </summary>
public sealed record PatternAggregateResult(
    ImmutableArray<AggregateGroup> Groups,
    int TotalMatches,
    int UniqueGroups,
    string Pattern,
    string GroupBy
);

/// <summary>
/// A sample match with position information for distributed sampling.
/// </summary>
public sealed record DistributedSample(
    int LineNumber,
    double PositionPercentage,
    string MatchText
);

/// <summary>
/// Result of distributed sampling of pattern matches.
/// </summary>
public sealed record DistributedSampleResult(
    ImmutableArray<DistributedSample> Samples,
    int TotalMatchesInFile,
    string Distribution,
    string Pattern
);

/// <summary>
/// Entry for a single file in a cross-file comparison.
/// </summary>
public sealed record FileComparisonEntry(
    string Path,
    int Count,
    double? Ratio
);

/// <summary>
/// Result of comparing pattern matches across multiple files.
/// </summary>
public sealed record CrossFileComparisonResult(
    ImmutableArray<FileComparisonEntry> Files,
    string Pattern,
    string ComparisonSummary
);
