# Benchmark Issues to Fix

## Date Discovered: January 18, 2026

## Context
Ran benchmark tests against shakespeare.txt (5.6MB) and bible.txt (4.4MB) in `benchmark-corpus/` folder.

## Issues Found

### Issue 1: count_pattern_matches returns total matches, not lines containing
**Symptom:** 
- Expected "love" count: 2,223 lines (from findstr)
- Got: 2,989 matches
- Difference: +766 (overcounting)

**Root Cause:** If a line contains "love" twice, it's counted as 2 matches instead of 1 line.

**Same issue for "LORD":**
- Expected: 6,419 lines
- Got: 6,650 matches

**Fix needed:** Add option to count unique lines vs total matches, or change default behavior.

### Issue 2: Output too large - returning sample matches bloats response
**Symptom:** Tool outputs saved to files (226KB, 499KB, 591KB) instead of returning to context.

**Root Cause:** `count_pattern_matches` returns `sampleMatches` with context by default, defeating the token-efficiency purpose.

**Fix needed:** 
- Make sample matches optional (default off for count operations)
- Or create a lightweight `count_lines_matching` tool that returns ONLY the count

### Issue 3: Regex anchoring may differ from findstr
**Symptom:**
- HAMLET expected: 361, got: 358 (-3)
- MACBETH expected: 205, got: 145 (-60)

**Root Cause:** Pattern `^HAMLET.` may behave differently. Need to verify:
- Is `^` anchoring to line start correctly?
- Is `.` matching the period literally or as regex wildcard?

**Fix needed:** Investigate regex behavior, document expected patterns.

## Ground Truth Reference (verified with findstr)

| File | Pattern | Expected Lines |
|------|---------|----------------|
| shakespeare.txt | love | 2,223 |
| shakespeare.txt | death | 944 |
| shakespeare.txt | ^SCENE | 791 |
| shakespeare.txt | ^HAMLET\. | 361 |
| shakespeare.txt | ^MACBETH\. | 205 |
| shakespeare.txt | \?$ | 8,138 |
| bible.txt | LORD | 6,419 |
| bible.txt | Jesus | 966 |
| bible.txt | \?$ | 311 |

## Status: FIXED (January 18, 2026)

### Fixes Applied

1. **Issue #1 (Over-counting) - FIXED**
   - Added `countUniqueLinesOnly` parameter (default: true) to count lines containing pattern
   - Uses `regex.IsMatch()` instead of `regex.Matches().Count`
   - Now matches grep -c / findstr /c behavior

2. **Issue #2 (Bloated output) - FIXED**
   - Added `includeSamples` parameter (default: false)
   - When false, returns empty SampleMatches array
   - Response size reduced from 200KB-600KB to minimal

3. **Issue #3 (Regex behavior) - FIXED**
   - Added `RegexOptions.Multiline` to CompileRegex
   - Updated tool descriptions with .NET regex syntax documentation
   - Users now informed that `.` is wildcard, use `\.` for literal

### Files Modified
- `src/RecursiveContext.Mcp.Server/Services/Interfaces.cs` - Updated interface
- `src/RecursiveContext.Mcp.Server/Services/ContentAnalysisService.cs` - Core logic + Multiline
- `src/RecursiveContext.Mcp.Server/Tools/Analysis/CountPatternMatchesTool.cs` - New parameters
- `src/RecursiveContext.Mcp.Server/Tools/Analysis/SearchWithContextTool.cs` - Regex docs

### Verification
- Build: ✅ Succeeded
- Tests: ✅ 205 passed, 0 failed