using System.Collections.Immutable; 
using System.Text.RegularExpressions; 
using CSharpFunctionalExtensions; 
using RecursiveContext.Mcp.Server.Config; 
using RecursiveContext.Mcp.Server.Models; 
 
namespace RecursiveContext.Mcp.Server.Services; 
 
internal sealed class PatternMatchingService : IPatternMatchingService 
{ 
    private readonly PathResolver _pathResolver; 
    private readonly IGuardrailService _guardrails; 
 
    public PatternMatchingService(PathResolver pathResolver, IGuardrailService guardrails) 
    { 
        _pathResolver = pathResolver; 
        _guardrails = guardrails; 
    } 
 
    public Task<Result<PatternMatchResult>> FindFilesAsync(string globPattern, int maxResults, CancellationToken ct) 
    { 
        var callCheck = _guardrails.CheckAndIncrementCallCount(); 
        if (callCheck.IsFailure) 
            return Task.FromResult(Result.Failure<PatternMatchResult>(callCheck.Error)); 
 
        if (string.IsNullOrWhiteSpace(globPattern)) 
            return Task.FromResult(Result.Failure<PatternMatchResult>("Pattern cannot be empty")); 
 
        try 
        { 
            var regex = GlobToRegex(globPattern); 
            var root = _pathResolver.WorkspaceRoot; 
            var matches = FindMatchingFiles(root, regex, maxResults, ct); 
 
            var result = new PatternMatchResult( 
                Pattern: globPattern, 
                MatchingPaths: matches.ToImmutableArray(), 
                TotalMatches: matches.Count 
            ); 
            return Task.FromResult(Result.Success(result)); 
        } 
        catch (Exception ex) 
        { 
            return Task.FromResult(Result.Failure<PatternMatchResult>($"Invalid pattern: {ex.Message}")); 
        } 
    } 
 
    private static Regex GlobToRegex(string glob) 
    { 
        var pattern = "^" + Regex.Escape(glob) 
            .Replace("\\*\\*/", "(.*/)?")   // **/ → optional path prefix (matches "foo/" or "")
            .Replace("\\*\\*", ".*")        // ** (without trailing /) → match anything
            .Replace("\\*", "[^/\\\\]*") 
            .Replace("\\?", ".") + "$"; 
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled); 
    } 
 
    private static List<string> FindMatchingFiles(string root, Regex regex, int maxResults, CancellationToken ct) 
    { 
        var results = new List<string>(); 
        var rootLen = root.Length + 1;
        
        // Directories to skip for performance (common bloat directories)
        // These are build outputs, caches, and dependencies that shouldn't contain source code
        var skipPatterns = new[] 
        { 
            "node_modules",     // npm packages
            ".git",             // Git repository data
            "\\bin\\",          // .NET build output
            "\\obj\\",          // .NET intermediate output
            "\\.vs\\",          // Visual Studio cache
            "\\packages\\",     // NuGet packages (old style)
            "\\TestResults\\",  // Test output
            "\\.angular\\",     // Angular build cache
            "\\dist\\",         // Distribution/build output
            "\\coverage\\",     // Test coverage reports
            "\\.idea\\",        // JetBrains IDE cache
            "\\_dist\\"         // Alternative dist folder
        };
 
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) 
        { 
            ct.ThrowIfCancellationRequested(); 
            if (results.Count >= maxResults) break;
            
            // Skip files in bloat directories
            if (skipPatterns.Any(p => file.Contains(p, StringComparison.OrdinalIgnoreCase)))
                continue;
 
            var relativePath = file.Substring(rootLen).Replace('\\', '/'); 
            if (regex.IsMatch(relativePath)) 
                results.Add(relativePath); 
        } 
 
        return results; 
    } 
}
