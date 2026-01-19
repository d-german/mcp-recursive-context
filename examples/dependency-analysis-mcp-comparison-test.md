# Cross-Repository Dependency Analysis - MCP vs No-MCP Comparison Test

## Purpose
Compare the results of dependency analysis using recursive-context MCP tools vs. standard PowerShell/grep commands to validate MCP tool accuracy.

---

## TEST A: Using MCP Tools ONLY

### Instructions
Use ONLY recursive-context MCP tools. Do NOT use PowerShell, grep, or terminal commands.

### Prompt
```
Using ONLY the recursive-context MCP tools (aggregate_matches, find_files_by_pattern, search_with_context), perform a complete dependency analysis of "FullTextSearchResults" across C:\projects\github.

Required deliverables:
1. Total files containing "FullTextSearchResults" (content search)
2. Total match count
3. Files with "FullTextSearchResults" in the filename
4. Breakdown by repository:
   - HCC-onbase-healthcare-api
   - HHSS
   - PAX-hcw-api
   - PAX-astra-bff
   - HCC-hyland-healthcare-shared-services
5. Top 10 files with most matches
6. Search for interface usage: "IFullTextSearchResults"
7. Search for converter classes: "FullTextSearchResultsConverter"

Use these specific tool calls:
- aggregate_matches with directory "C:\projects\github", filePattern "*", searchPattern "FullTextSearchResults"
- find_files_by_pattern with pattern "**/*FullTextSearchResults*"
- aggregate_matches for "IFullTextSearchResults" in *.cs files
- aggregate_matches for "FullTextSearchResultsConverter" in *.cs files

Report all results in a structured table format.
```

---

## TEST B: Using PowerShell/Terminal ONLY

### Instructions
Use ONLY PowerShell commands. Do NOT use any MCP tools.

### Prompt
```
Using ONLY PowerShell commands (no MCP tools), perform a complete dependency analysis of "FullTextSearchResults" across C:\projects\github.

Required deliverables:
1. Total files containing "FullTextSearchResults" (content search)
2. Total match count
3. Files with "FullTextSearchResults" in the filename
4. Breakdown by repository:
   - HCC-onbase-healthcare-api
   - HHSS
   - PAX-hcw-api
   - PAX-astra-bff
   - HCC-hyland-healthcare-shared-services
5. Top 10 files with most matches
6. Search for interface usage: "IFullTextSearchResults"
7. Search for converter classes: "FullTextSearchResultsConverter"

Use these PowerShell commands:
- Get-ChildItem -Recurse | Select-String -Pattern "FullTextSearchResults"
- Get-ChildItem -Recurse -Filter "*FullTextSearchResults*"
- Group results by directory/repository

Report all results in a structured table format.
```

---

## Expected Results (Both Should Match)

| Metric | Expected Value |
|--------|---------------|
| Files with pattern in content | 90-100 |
| Total matches | 300-400 |
| Files with pattern in filename | 20-25 |
| .cs source files | 12-15 |
| Test files (*Tests.cs, *UnitTests.cs) | 8-10 |
| Interface files (IFullTextSearchResults) | 2-3 |
| Converter files | 2-3 |

---

## Comparison Checklist

After running both tests, compare:

| Metric | MCP Result | PowerShell Result | Match? |
|--------|-----------|------------------|--------|
| Total files with content match | | | ☐ |
| Total match count | | | ☐ |
| Files by filename pattern | | | ☐ |
| HCC-onbase-healthcare-api files | | | ☐ |
| HHSS files | | | ☐ |
| PAX-hcw-api files | | | ☐ |
| PAX-astra-bff files | | | ☐ |
| HCC-hyland-healthcare-shared-services files | | | ☐ |
| IFullTextSearchResults matches | | | ☐ |
| FullTextSearchResultsConverter matches | | | ☐ |

---

## Bug Detection

If results don't match, investigate:
1. Are files at root level being missed? (GlobToRegex bug)
2. Are absolute paths working? (ToRelativePath bug)
3. Is maxFiles limit too low? (Default parameter bug)
4. Are certain file extensions being skipped?
5. Are binary files being incorrectly processed?

---

## Quick Validation Commands

### PowerShell Ground Truth
```powershell
# Total files with content match
(Get-ChildItem -Path "C:\projects\github" -Recurse -File | Select-String -Pattern "FullTextSearchResults" -List).Count

# Total matches
(Get-ChildItem -Path "C:\projects\github" -Recurse -File | Select-String -Pattern "FullTextSearchResults").Count

# Files by filename
(Get-ChildItem -Path "C:\projects\github" -Recurse -Filter "*FullTextSearchResults*" -File).Count

# By repository
Get-ChildItem -Path "C:\projects\github" -Recurse -File | 
  Select-String -Pattern "FullTextSearchResults" -List | 
  Group-Object { $_.Path.Split('\')[3] } | 
  Select-Object Count, Name | 
  Sort-Object Count -Descending
```
