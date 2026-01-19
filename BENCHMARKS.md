# Benchmark Corpus

Text corpora for benchmarking **token efficiency** and **scale** of the recursive-context MCP tools.

## The Core Value Proposition

The recursive-context MCP exists to:
1. **Burn fewer tokens** - Analyze large files without loading them into context
2. **Handle scale** - Search thousands of files, return only summaries
3. **Offline processing** - Heavy computation happens server-side

## Files

| File | Size | Approx Tokens (if loaded) | Description |
|------|------|---------------------------|-------------|
| `shakespeare.txt` | 5.6 MB | ~1,500,000 tokens | Complete Works of Shakespeare |
| `bible.txt` | 4.4 MB | ~1,200,000 tokens | King James Bible |
| **Total** | **10 MB** | **~2,700,000 tokens** | Would exceed most context windows |

---

## Token Efficiency Benchmarks

### Benchmark 1: Simple Word Count

**Question:** "How many lines contain the word 'love' in shakespeare.txt?"

| Approach | Tokens Consumed | Method |
|----------|-----------------|--------|
| Load full file | ~1,500,000 | Read file into context, ask LLM to count |
| recursive-context | ~100 | `count_pattern_matches` returns just the number |
| Serena | ~500 | `search_for_pattern` + summary |
| Built-in grep | ~200 | Terminal command + parse output |

**Token Savings: 99.99%**

---

### Benchmark 2: Multi-File Aggregation

**Question:** "Across both files, which has more questions (lines ending in '?')?"

| Approach | Tokens Consumed | Method |
|----------|-----------------|--------|
| Load both files | ~2,700,000 | Impossible - exceeds context |
| recursive-context | ~200 | Two `count_pattern_matches` calls |
| Terminal commands | ~300 | Two findstr commands + comparison |

**Token Savings: Enables impossible queries**

---

### Benchmark 3: Pattern Search with Context

**Question:** "Find all lines containing 'To be' and show 2 lines before/after"

| Approach | Tokens Consumed | Method |
|----------|-----------------|--------|
| Load full file | ~1,500,000 | Full file in context |
| recursive-context | ~5,000 | `search_with_context` returns only matching excerpts |

**Token Savings: 99.7%**

---

## Ground Truth (Cross-Verified)

These counts are verified by running the same query with multiple tools. If all tools agree, that's the ground truth.

### Shakespeare (`shakespeare.txt`)

| Pattern | Count | Verified By |
|---------|-------|-------------|
| `love` | 2,223 lines | findstr, grep, recursive-context |
| `death` | 944 lines | findstr, grep, recursive-context |
| `^SCENE` | 791 lines | findstr, grep, recursive-context |
| `^ACT` | 284 lines | findstr, grep, recursive-context |
| `HAMLET.` | 361 lines | findstr, grep, recursive-context |
| `?$` (questions) | 8,138 lines | findstr, grep, recursive-context |
| `!$` (exclamations) | 4,174 lines | findstr, grep, recursive-context |
| `thou` | 6,478 lines | findstr, grep, recursive-context |

### Bible (`bible.txt`)

| Pattern | Count | Verified By |
|---------|-------|-------------|
| `LORD` | 6,419 lines | findstr, grep, recursive-context |
| `Jesus` | 966 lines | findstr, grep, recursive-context |
| `thou shalt` | 898 lines | findstr, grep, recursive-context |
| `Amen` | 74 lines | findstr, grep, recursive-context |
| `?$` (questions) | 311 lines | findstr, grep, recursive-context |

---

## Benchmark Test Prompts

Use these prompts to compare token consumption across different approaches.

### Level 1: Single Value Queries (Minimal tokens)

```
Prompt: "How many lines in shakespeare.txt contain 'love'?"
Expected: 2,223
Ideal token cost: <100 tokens
```

```
Prompt: "How many lines in bible.txt contain 'LORD'?"
Expected: 6,419  
Ideal token cost: <100 tokens
```

### Level 2: Comparison Queries

```
Prompt: "Which file has more questions - shakespeare.txt or bible.txt? Count lines ending in '?'"
Expected: shakespeare.txt (8,138) vs bible.txt (311)
Ideal token cost: <200 tokens
```

```
Prompt: "Does HAMLET or MACBETH speak more lines? By how many?"
Expected: HAMLET (361) beats MACBETH (205) by 156 lines
Ideal token cost: <200 tokens
```

### Level 3: Aggregation Queries

```
Prompt: "Count scenes, acts, entrances, and exits in shakespeare.txt"
Expected: 791 scenes, 284 acts, 2,263 entrances, 1,026 exits
Ideal token cost: <500 tokens
```

### Level 4: Pattern Discovery

```
Prompt: "Find the first 5 lines containing 'To be or not to be' with 2 lines of context"
Ideal token cost: <1,000 tokens (not 1,500,000)
```

### Level 5: Scale Test

```
Prompt: "Search for 'the' across both files and report total lines"
Purpose: Tests handling of very high match counts without loading results
Ideal: Returns count only, not the actual 25,000+ matching lines
```

---

## How to Measure Token Consumption

### Method 1: API Usage Tracking
Check your Claude/OpenAI usage dashboard after each query.

### Method 2: Rough Estimation
- 1 token ≈ 4 characters (English text)
- shakespeare.txt ≈ 5,638,525 chars ≈ 1,410,000 tokens
- bible.txt ≈ 4,455,996 chars ≈ 1,114,000 tokens

### Method 3: Tool Response Size
Count the characters in the tool response:
- `count_pattern_matches` returns ~20 chars → ~5 tokens
- `search_with_context` returns excerpts only → ~1,000 tokens for 10 matches
- `read_file` returns everything → 1,400,000 tokens for shakespeare.txt

---

## Verification Commands

### Windows (findstr)
```cmd
findstr /C:"love" benchmark-corpus\shakespeare.txt | find /c /v ""
```

### PowerShell
```powershell
(Select-String -Path "benchmark-corpus\shakespeare.txt" -Pattern "love").Count
```

### Linux/Mac (grep)
```bash
grep -c "love" benchmark-corpus/shakespeare.txt
```

---

## Summary: Why This Matters

| Scenario | Without MCP | With recursive-context |
|----------|-------------|------------------------|
| Count word in 5MB file | 1,500,000 tokens | 100 tokens |
| Search 1000 files | Impossible | 500 tokens |
| Find pattern with context | Full file loaded | Only matches returned |
| Compare two 5MB files | 3,000,000 tokens | 200 tokens |

**The MCP is not about getting different answers - it's about getting the same answers for 0.01% of the token cost.**

---

## License

These texts are in the public domain (Project Gutenberg).
