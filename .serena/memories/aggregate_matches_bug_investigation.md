# Bug Investigation: aggregate_matches Returns 0 Matches

## Date: 2026-01-19

## Problem Summary
The `aggregate_matches` MCP tool returns 0 matches even when files clearly contain the search pattern.

## Test Results

### Test 1: Old Config (RLM_WORKSPACE_ROOT: ".")
- `aggregate_matches("C:\projects\github", "*.cs", "FullTextSearchResults")` → **0 files, 0 matches**
- `find_files_by_pattern("**/*FullTextSearchResults*")` → **0 files** (sandboxed to mcp-recursive-context folder)
- Root cause: Workspace was sandboxed to single repo

### Test 2: New Config (RLM_WORKSPACE_ROOT: "C:\projects\github")
- `aggregate_matches(".", "*", "FullTextSearchResults")` → **0 files, 0 matches**
- `aggregate_matches(".", "*.cs", "FullTextSearchResults")` → **50 files searched, 0 matches**
- `aggregate_matches(".", "*.md", "FullTextSearchResults")` → **50 files, 0 matches**
- `find_files_by_pattern("**/*FullTextSearchResults*")` → **20 files found** ✅

### PowerShell Comparison (Ground Truth)
```powershell
Get-ChildItem -Path "C:\projects\github" -Recurse -Include "*.cs" | 
  Select-String -Pattern "FullTextSearchResults" | 
  Group-Object Path | Measure-Object
```
**Result: 95 files containing "FullTextSearchResults"**

## Key Findings

1. **`find_files_by_pattern` WORKS** after config fix
   - Found 20 files with "FullTextSearchResults" in filename
   - Correctly traverses all sub-repositories

2. **`aggregate_matches` BROKEN**
   - Returns 0 matches even when searching files that definitely contain the pattern
   - Searched 50 files but found nothing
   - Bug is in content searching, not file discovery

3. **Files Found by `find_files_by_pattern` (filename search):**
   - HCC-onbase-healthcare-api/src/.../Core/FullTextSearchResults.cs
   - HHSS/src/.../Utils/FullTextSearchResultsConverter.cs
   - HHSS/src/.../Interfaces/IFullTextSearchResults.cs
   - PAX-astra-bff/src/.../Models/FullTextSearchResults.cs
   - PAX-hcw-api/src/.../Models/FullTextSearchResults.cs
   - HCC-hyland-healthcare-shared-services/src/.../FullTextSearchResults.cs
   - (and 14 more)

## Bug Hypothesis

The `aggregate_matches` tool may have an issue with:
1. **File reading** - Not actually reading file content
2. **Regex matching** - Pattern not being applied correctly
3. **File filtering** - Excluding files before searching
4. **Path handling** - Issues with Windows paths
5. **Binary file detection** - Incorrectly classifying .cs files as binary

## Files to Investigate

Look in the MCP server source code:
- `src/RecursiveContext.Mcp.Server/Tools/AggregateMatchesTool.cs` (or similar)
- Check how files are read and searched
- Check regex pattern application
- Check file filtering logic

## Reproduction Steps

1. Configure `RLM_WORKSPACE_ROOT: "C:\projects\github"` in mcp-config.json
2. Restart CLI: `copilot --allow-all-tools --allow-all-paths`
3. Run: `aggregate_matches(".", "*.cs", "FullTextSearchResults", maxFiles=100)`
4. Expected: 95 files, 200+ matches
5. Actual: 0 files, 0 matches

## Config Location
`C:\Users\dgerman\.copilot\mcp-config.json`

```json
"recursive-context": {
  "env": {
    "RLM_WORKSPACE_ROOT": "C:\\projects\\github",
    "RLM_MAX_FILES_PER_AGGREGATION": "15000",
    "RLM_MAX_DEPTH": "100",
    "RLM_MAX_MATCHES_PER_SEARCH": "50000",
    "RLM_MAX_BYTES_PER_READ": "10485760",
    "RLM_MAX_CHUNK_SIZE": "1000",
    "RLM_TIMEOUT_SECONDS": "120"
  }
}
```

## RESOLVED - 2026-01-19

### Root Cause
The `GlobToRegex` function in `PatternMatchingService.cs` was incorrectly converting `**/` patterns.

The pattern `**/*.cs` was being converted to a regex that **required** a `/` before the filename:
```
^.*\/[^/\\]*\.cs$
```

This regex would NOT match files at the root level (e.g., `Program.cs`) because they have no directory prefix.

### The Fix
Changed `GlobToRegex` to make `**/` optional by using `(.*/)?` instead of `.*\/`:

```csharp
// Before (buggy)
.Replace("\\*\\*", ".*")

// After (fixed)
.Replace("\\*\\*/", "(.*/)?")   // **/ → optional path prefix
.Replace("\\*\\*", ".*")        // ** (without trailing /) → match anything
```

Now `**/*.cs` generates `^(.*/)?[^/\\]*\.cs$` which correctly matches:
- `Program.cs` (root level) ✅
- `src/Services/MyService.cs` (nested) ✅

### Bug #2: Absolute Directory Paths Not Converted to Relative
When users passed absolute paths like `C:\projects\github` as the directory parameter, the code built
a glob pattern like `C:\projects\github/**/*.cs`. Since `FindMatchingFiles` matches against **relative paths**
from the workspace root, this regex would never match.

**Fix**: Added `ToRelativePath` method to `PathResolver` that converts absolute paths to relative paths
from the workspace root. Modified `AggregateMatchesAsync` to call `ToRelativePath` before building the glob.

### Files Modified
1. `src/RecursiveContext.Mcp.Server/Services/PatternMatchingService.cs` - Fixed `GlobToRegex`
2. `src/RecursiveContext.Mcp.Server/Config/PathResolver.cs` - Added `ToRelativePath` method
3. `src/RecursiveContext.Mcp.Server/Services/AggregationService.cs` - Use `ToRelativePath` for directory
4. `tests/.../PatternMatchingServiceTests.cs` - Updated test to expect 3 files (including root)
5. `tests/.../AggregationServiceTests.cs` - Updated test to use root-level file
6. `tests/.../PathResolverTests.cs` - Added 8 new tests for `ToRelativePath`

### Bug #3: File Enumeration Order Exhausts Limit on Bloat Directories
When searching `C:\projects\github` with 248,797 files, the first 10,000 files enumerated were 
almost entirely from `HCC-hc-core-ui` (94% - mostly node_modules), leaving no room for actual 
source repositories like HHSS, PAX-*, HCC-onbase-*.

**Fix**: Added exclusion patterns for common bloat directories in `FindMatchingFiles`:
- node_modules
- .git
- \bin\
- \obj\
- \.vs\
- \packages\
- \TestResults\

### Files Modified
1. `src/RecursiveContext.Mcp.Server/Services/PatternMatchingService.cs` - Added skip patterns in `FindMatchingFiles`

### Verification
All 249 tests pass after the fix.

## Validation Testing - 2026-01-19

### Before Bug #3 Fix (Per-Repository Search Strategy)
The Claude model worked around Bug #3 by making 18 separate `aggregate_matches` calls targeting 
specific repositories instead of one global search:

| Repository | Files Searched | Matches Found |
|------------|----------------|---------------|
| HHSS | 948 | 116 |
| HCC-hyland-healthcare-shared-services | 1,170 | 132 |
| PAX-hcw-api | 356 | 42 |
| HCC-onbase-healthcare-api | 636 | 36 |
| PAX-astra-bff | 410 | 10 |
| Others (0 matches) | ~15,600 | 0 |

**Results:**
- Files found: **85** (89.5% of ground truth)
- Total matches: **336**
- Duration: **49 seconds API / 3m 5s wall**
- Strategy: Per-repository searches avoided global enumeration issues

### Ground Truth Comparison
| Metric | MCP Result | Ground Truth | Accuracy |
|--------|-----------|--------------|----------|
| Files with matches | 85 | 95 | 89.5% |
| Total matches | 336 | 431 | 78% |
| Files in filename | 20 | 20 | 100% |

### Why Bug #3 Fix Is Still Valuable
Even though per-repository searching worked around the issue:
1. **Single global searches now work** - users don't need to know to split by repository
2. **Performance** - no time wasted enumerating node_modules, .git, bin, obj
3. **Reliability** - doesn't depend on model intelligence to work around the bug
4. **Missing files** - 10 files (10.5%) were still missed because not all repos were searched

### After Bug #3 Fix v1 (Initial skipPatterns - REGRESSION)
Initial fix with 7 skip patterns found only **59 files** (vs 85 before), despite faster execution.
Root cause: `.angular`, `dist`, `coverage` folders still consuming most of the 10,000 file limit.

### After Bug #3 Fix v2 (Expanded skipPatterns)
Added 5 more skip patterns:
- `.angular` - Angular build cache (2,453+ files)
- `dist` - Distribution/build output
- `coverage` - Test coverage reports
- `.idea` - JetBrains IDE cache
- `_dist` - Alternative dist folder

**Result:** 29 repos now covered in first 10,000 files (vs 6 repos before expansion).
PAX-* repos now represented: PAX-hcw-api (1,134), PAX-astra (695), PAX-astra-bff (459).

User needs to restart MCP server and re-run test to validate.
