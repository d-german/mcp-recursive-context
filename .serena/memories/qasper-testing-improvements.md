# QASPER Testing Results & Potential Improvements

## Test Dataset
- **Source**: QASPER dataset from Hugging Face (allenai/qasper)
- **Location**: `qasper-test-workspace/{train,validation,test}/` with 1,585 papers as .txt files
- **Ground Truth**: `qasper-dataset/questions_*.json` files

## Test Results Comparison

### Test 1: "Papers with both BERT and sentiment analysis"
| Method | Result | Correct? |
|--------|--------|----------|
| With MCP tools | 53 papers | ✅ Correct |
| Without MCP (shell) | 71 papers | ❌ Wrong |

**Winner: MCP tools**

### Test 2: "Papers with >100,000 training examples"
| Method | Result | Ground Truth ~115 |
|--------|--------|-------------------|
| With MCP tools | 17 papers | ~15% recall |
| Without MCP (grep) | 97 papers | ~84% recall |

**Winner: grep** - The LLM used better search strategy with grep (6+ patterns) vs MCP tools (fewer patterns, gave up earlier)

## Issues Identified

### 1. Path Confusion (FIXED)
- **Problem**: LLM used relative paths from shell CWD instead of workspace root
- **Solution**: Updated 7 tool descriptions with explicit path guidance
- **Files Modified**:
  - `AggregateMatchesTool.cs`
  - `SearchWithContextTool.cs`
  - `SampleMatchesDistributedTool.cs`
  - `CountFilesTool.cs`
  - `ListDirectoriesTool.cs`
  - `FindFilesByPatternTool.cs`
  - `GetServerInfoTool.cs` (now returns WorkspaceRoot)

### 2. Search Strategy Issue (NOT FIXED)
- **Problem**: With grep, LLM naturally runs multiple commands. With MCP tools, it seems to "give up" after fewer pattern variations
- **Symptom**: Only searched for limited number formats (100,000) but missed:
  - Written: "one hundred thousand", "a million"
  - Scientific: "1e5", "10^5"
  - Approximate: "~150,000", "approximately 200K"
  - Ranges: "100K-500K"

### 3. File Limits
- **Problem**: `maxFiles: 100` default may limit coverage
- **Observation**: `filesSearched: 1000` in test suggests this was increased

## Potential Improvements to Implement

### Option 1: Strategy Hints in Descriptions
Add to `AggregateMatchesTool.cs` description:
```
TIP: For comprehensive number searches, run multiple patterns:
- Comma format: \d{1,3},\d{3},\d{3}
- Plain digits: \d{6,}
- K/M suffix: \d+[KkMm]\b
- Written: (hundred thousand|million)
```

### Option 2: Multi-Pattern Tool
Create new tool that accepts array of patterns and returns combined results:
```csharp
[McpTool("multi_pattern_search")]
public async Task<Result> SearchMultiplePatterns(
    string directory,
    string filePattern,
    string[] searchPatterns,  // Array of regex patterns
    bool unionResults = true  // Combine or intersect
)
```

### Option 3: Increase Defaults
- Change `maxFiles` default from 100 to 500 or 1000
- Add guidance about running tool multiple times with different patterns

### Option 4: Example Workflow in Description
Add example showing multi-pass search strategy in tool description

## Build Status
- ✅ Build passes (0 warnings, 0 errors)
- ✅ GetServerInfoTool signature updated with RlmSettings
- ✅ SmokeTests.cs fixed for new signature

## Next Steps
1. Decide which improvement option(s) to implement
2. Re-run tests after changes
3. Compare recall rates again
