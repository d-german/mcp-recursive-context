# RLM-Enabled MCP Server - Implementation Prompt

You are enhancing an existing C# Model Context Protocol (MCP) server to complete its implementation as a Recursive Language Model (RLM) enablement layer.

---

## EXISTING IMPLEMENTATION STATUS

The project "RecursiveContext.Mcp" already exists with:

| Property | Value |
|----------|-------|
| Repository | mcp-recursive-context |
| Solution | RecursiveContext.Mcp.sln |
| Framework | .NET 9.0, C# latest |
| Dependencies | ModelContextProtocol 0.5.0-preview.1, CSharpFunctionalExtensions 3.4.0, Microsoft.Extensions.Hosting 10.0.1 |

### Already Implemented - Navigation Tools

| Tool | Description | Status |
|------|-------------|--------|
| `list_files` | List files in directory with pagination | ✅ |
| `list_directories` | List subdirectories | ✅ |
| `read_file` | Read entire file contents | ✅ |
| `read_file_chunk` | Read specific line range (startLine, endLine) | ✅ |
| `get_context_info` | Workspace metadata (file counts, total size) | ✅ |
| `find_files_by_pattern` | Glob pattern search with maxResults | ✅ |

### Already Implemented - Domain-Specific Tools

| Tool | Description | Status |
|------|-------------|--------|
| `enumerate_controllers` | Find ASP.NET controllers | ✅ |
| `enumerate_endpoints` | Find HTTP endpoints | ✅ |
| `get_server_info` | Server version/status | ✅ |

### Already Implemented - Infrastructure

| Component | Description | Status |
|-----------|-------------|--------|
| `GuardrailService` | MaxToolCallsPerSession, MaxBytesPerRead, CheckAndIncrementCallCount() | ✅ |
| `IFileSystemService` | Async file operations | ✅ |
| `IPatternMatchingService` | Glob-to-regex conversion | ✅ |
| `ToolResponseFormatter` | Consistent JSON output | ✅ |
| CancellationToken | Support on all async methods | ✅ |

### Project Structure

```
src/RecursiveContext.Mcp.Server/
├── Config/           # RlmSettings, ConfigReader, PathResolver
├── Models/           # Immutable domain records
├── Server/           # Host, DI, logging
├── Services/         # Business logic (FileSystemService, GuardrailService, etc.)
└── Tools/            # MCP tool implementations
    ├── DotNet/       # enumerate_controllers, enumerate_endpoints
    ├── FileSystem/   # list_files, list_directories, read_file, read_file_chunk
    ├── Metadata/     # get_context_info
    ├── Search/       # find_files_by_pattern
    └── Server/       # get_server_info
tests/RecursiveContext.Mcp.Server.Tests/
```

---

## THE PROBLEM - WHAT'S MISSING

The current implementation provides **NAVIGATION** but not **ANALYSIS**.

### Current Flow (Non-Deterministic)

```
1. Tool reads 12KB file → returns raw text
2. LLM parses text → estimates "~120 classes"
3. Different runs → different estimates
```

### Required Flow (Deterministic)

```
1. Tool reads file → counts pattern matches → returns: 127
2. Every run → exact same answer
```

### The Fundamental Principle

> **ANY question with a factual numeric answer MUST be answerable via a single tool call that returns that exact number.**

### Examples That MUST Work Deterministically

| Question | Tool Call | Returns |
|----------|-----------|---------|
| "How many classes implement IOperation?" | `count_pattern_matches` | `int` |
| "How many lines contain 'TODO'?" | `search_with_counts` | `int + positions` |
| "What are the chunk boundaries for 50-line chunks?" | `get_chunk_info` | `int[]` |

Without analysis tools, the LLM must parse raw text, hit token limits, sample/estimate, and produce **NON-DETERMINISTIC** results.

---

## CONCEPTUAL BACKGROUND (Reference Only)

You may reference "Recursive Language Models" (Zhang, Kraska, Khattab, 2025) as conceptual background ONLY.

### Key Insight from Paper

> "Long prompts should be treated as part of the environment that the LLM can **SYMBOLICALLY** interact with."

**"Symbolic" means:** structured handles (counts, indices, chunk IDs), NOT raw text blobs.

### Mapping Paper Concepts to Architecture

| Paper Concept | Maps To |
|---------------|---------|
| "REPL environment" | MCP TOOLS |
| "Recursive sub-calls" | Repeated CLIENT LLM calls |
| The MCP server | The ENVIRONMENT, not the THINKER |

### Do NOT

- ❌ Replicate the paper's Python REPL demo
- ❌ Embed an LLM inside the server
- ❌ Implement recursive reasoning loops internally

---

## NEW TOOLS TO IMPLEMENT - Deterministic Analysis

Create a new folder: `Tools/Analysis/`

### 1. CountPatternMatchesTool

```
count_pattern_matches(path: string, pattern: regex, max_results: int = 1000)
Returns: { count: int, sample_matches: string[], truncated: bool }
```

The LLM can ask "how many classes?" and get an exact integer.

### 2. SearchWithContextTool

```
search_with_context(path: string, pattern: regex, context_lines: int = 2, max_results: int = 100)
Returns: Match[] where Match = { 
    line_number: int, 
    match_text: string, 
    context_before: string[], 
    context_after: string[] 
}
```

Enables precise navigation to specific lines.

### 3. CountLinesTool

```
count_lines(path: string)
Returns: int
```

Know file size without reading entire content.

### 4. GetChunkInfoTool

```
get_chunk_info(path: string, chunk_size_lines: int = 50)
Returns: { total_lines: int, chunk_count: int, chunk_boundaries: int[] }
```

LLM can plan systematic traversal without guessing.

### 5. ReadChunkByIndexTool

```
read_chunk_by_index(path: string, chunk_index: int, chunk_size_lines: int = 50)
Returns: { chunk_index: int, start_line: int, end_line: int, content: string }
```

Companion to `get_chunk_info` for systematic iteration.

### 6. CountFilesTool

```
count_files(directory: string, pattern: glob = "*", recursive: bool = true)
Returns: int
```

Know scope before enumerating.

### 7. AggregateMatchesTool

```
aggregate_matches(directory: string, file_pattern: glob, search_pattern: regex, max_files: int = 100)
Returns: { 
    files_searched: int, 
    total_matches: int, 
    matches_by_file: { path: string, count: int }[] 
}
```

Answer questions like "how many TODOs in the codebase?" in one call.

---

## IMPLEMENTATION REQUIREMENTS

### Architecture Rules (Non-Negotiable)

#### The MCP Server MUST NOT

- ❌ Call OpenAI, Azure OpenAI, or any LLM APIs
- ❌ Contain model selection logic
- ❌ Implement recursive reasoning loops internally
- ❌ Act as an autonomous agent
- ❌ Require API keys
- ❌ Track tokens or inference cost

#### The MCP Server MUST

- ✅ Be fully model-agnostic
- ✅ Expose only deterministic tools
- ✅ Be safe, bounded, and predictable

#### All Analysis Tools MUST

- ✅ Return exact counts, not estimates
- ✅ Be reproducible (same input → same output)
- ✅ Support cancellation via CancellationToken
- ✅ Respect GuardrailService limits
- ✅ Use Railway-oriented error handling (Result&lt;T&gt;)

### Code Style Requirements

#### Modern C# / .NET 9

- Use LINQ `.Chunk()` method for batch processing (added in .NET 6)
- Use file-scoped namespaces
- Use primary constructors where appropriate
- Use collection expressions `[...]` syntax
- Use pattern matching exhaustively

#### LINQ Patterns to Prefer

| Method | Use Case | .NET Version |
|--------|----------|--------------|
| `.Chunk(n)` | Batching lines/items | .NET 6+ |
| `.Index()` | Get (index, item) tuples | .NET 9 |
| `.CountBy()` | Grouping counts | .NET 9 |
| `.AggregateBy()` | Grouped aggregations | .NET 9 |

Avoid manual `for` loops when LINQ expresses intent better.

#### Functional / Railway-Oriented

- Return `Result<T>` or `Result<T, E>` from all service methods
- Chain with `.Bind()`, `.Map()`, `.Tap()`, `.MapError()`
- No exceptions for expected failures
- Use `Maybe<T>` for optional values

#### Immutability

- Use `record` for all DTOs and tool responses
- Use `ImmutableArray<T>` or `IReadOnlyList<T>`
- No mutable service state (except `GuardrailService._callCount`)

#### Static Methods

- Any method not using instance state MUST be `static`
- Tool classes should be `static class` with `static` methods

#### Follow Existing Patterns

- Match existing tool structure in `Tools/` folder
- Use `ToolResponseFormatter.FormatResult()`
- Use `[McpServerToolType]` and `[McpServerTool]` attributes
- Use `[Description]` for all parameters

---

## SERVICE LAYER ADDITIONS

Add to `Services/`:

### 1. IContentAnalysisService / ContentAnalysisService

```csharp
Task<Result<MatchCountResult>> CountPatternMatchesAsync(
    string path, string pattern, int maxResults, CancellationToken ct);

Task<Result<IReadOnlyList<MatchResult>>> SearchWithContextAsync(
    string path, string pattern, int contextLines, int maxResults, CancellationToken ct);

Task<Result<int>> CountLinesAsync(string path, CancellationToken ct);
```

### 2. IChunkingService / ChunkingService

```csharp
Task<Result<ChunkInfo>> GetChunkInfoAsync(
    string path, int chunkSize, CancellationToken ct);

Task<Result<ChunkContent>> ReadChunkAsync(
    string path, int chunkIndex, int chunkSize, CancellationToken ct);
```

Use `.Chunk()` internally:

```csharp
var lines = await File.ReadAllLinesAsync(path, ct);
var chunks = lines.Chunk(chunkSize).ToList();
return new ChunkInfo(
    lines.Length, 
    chunks.Count, 
    chunks.Index().Select(c => c.Index * chunkSize).ToImmutableArray());
```

### 3. IAggregationService / AggregationService

```csharp
Task<Result<AggregateResult>> AggregateMatchesAsync(
    string directory, string filePattern, string searchPattern, 
    int maxFiles, CancellationToken ct);
```

### All Services Must

- Inject `IGuardrailService` and check limits
- Inject `PathResolver` for safe path resolution
- Use `CancellationToken` on all async operations
- Return `Result<T>` with meaningful error messages

---

## MODELS TO ADD

Add to `Models/`:

```csharp
public readonly record struct MatchCountResult(
    int Count,
    IReadOnlyList<string> SampleMatches,
    bool Truncated);

public readonly record struct MatchResult(
    int LineNumber,
    string MatchText,
    IReadOnlyList<string> ContextBefore,
    IReadOnlyList<string> ContextAfter);

public readonly record struct ChunkInfo(
    int TotalLines,
    int ChunkCount,
    IReadOnlyList<int> ChunkBoundaries);

public readonly record struct ChunkContent(
    int ChunkIndex,
    int StartLine,
    int EndLine,
    string Content);

public readonly record struct AggregateResult(
    int FilesSearched,
    int TotalMatches,
    IReadOnlyList<FileMatchCount> MatchesByFile);

public readonly record struct FileMatchCount(
    string Path,
    int Count);
```

---

## GUARDRAIL ENHANCEMENTS

Extend `GuardrailService` to include:

| Setting | Default | Purpose |
|---------|---------|---------|
| `MaxFilesPerAggregation` | 500 | Prevent runaway multi-file operations |
| `MaxMatchesPerSearch` | 10000 | Limit memory usage on pattern searches |
| `MaxChunkSize` | 500 lines | Prevent excessively large chunk reads |

These prevent runaway operations on massive codebases.

---

## TESTS

### Proving Determinism

- Same file + same pattern → same count, every time
- Chunk boundaries are consistent across calls
- Aggregations produce reproducible results

### Edge Cases

- Empty files
- Binary files (graceful failure)
- Files exceeding MaxBytesPerRead
- Patterns with no matches
- Very large files (ensure chunking works)

---

## DELIVERABLES

1. ✅ New tool implementations in `Tools/Analysis/`
2. ✅ New services in `Services/`
3. ✅ New models in `Models/`
4. ✅ Extended `GuardrailService` with new limits
5. ✅ Tests proving deterministic behavior
6. ✅ Updated README explaining:
   - What analysis tools are available
   - How client LLM achieves recursive reasoning via repeated tool calls
   - Example: counting classes in a 10MB file using chunk traversal

---

## SUCCESS CRITERIA

After implementation, this prompt to the client LLM:

> "How many classes implement IOperation in Operations.Generated.cs?"

### MUST Produce

```
1. Single tool call: count_pattern_matches("Operations.Generated.cs", "internal sealed partial class.*Operation")
2. Response: { "count": 127, "sample_matches": [...], "truncated": false }
3. LLM answer: "There are exactly 127 classes implementing IOperation."
```

### NOT

```
1. read_file("Operations.Generated.cs") → 12KB of raw text
2. LLM counts manually, hits limits → "approximately 120+ classes"
3. Different runs → different estimates
```

---

## Final Principle

> **If any tool requires the LLM to count items in returned text, the design has failed.**

Build this correctly.
