# Cross-Repository Dependency Analysis - FullTextSearchResults Model (No MCP Test)

## Test Objective
Determine if custom scripts and file operations can perform the same dependency analysis as the recursive-context MCP by mapping all classes that depend on the `FullTextSearchResults` data model across multiple repositories.

## Task
**Analyze the `FullTextSearchResults` model and create a complete dependency graph showing:**
1. All classes that directly use/reference `FullTextSearchResults`
2. All classes that transitively depend on it (use classes that use it)
3. How the model flows through the 5-layer architecture
4. Data transformations applied at each layer
5. Which properties are used vs. ignored at each layer

## CRITICAL CONSTRAINTS
⚠️ **MANDATORY RESTRICTIONS**:
- **NO MCP SERVERS**: You MUST NOT use ANY MCP servers (recursive-context, serena, ref, or any others)
- **CUSTOM SCRIPTS ALLOWED**: You MAY create and execute custom scripts (PowerShell, Python, ripgrep, etc.)
- **TEXT-BASED ANALYSIS**: Use grep, ripgrep, find, or similar text-search tools
- All analysis must be accomplished through standard file system operations and custom scripts only

## Required Deliverables

### 1. **Complete Dependency Graph (Mermaid)**
Create a dependency graph showing:
```
FullTextSearchResults (source definition)
    ↓ used by
TypeScript Interface (Angular UI)
    ↓ used by
FullTextSearchComponent
FullTextSearchService
    ↓ passed to
C# DTO (BFF layer)
    ↓ used by
PatientContentsController
PatientContentRepository
    ↓ transformed to
[continue through all 5 layers]
```

### 2. **Property Usage Analysis**
For each layer, document:
- Which properties of `FullTextSearchResults` are READ
- Which properties are WRITTEN/transformed
- Which properties are IGNORED
- Any property name changes during transformation

Example:
```
Layer: Angular UI
- hitCount: READ (displayed in UI)
- fullTextPageDataItems: READ (rendered as highlights)
- hitHighlightHtml: READ (injected into DOM)

Layer: BFF
- hitCount: PASS-THROUGH (no transformation)
- fullTextPageDataItems: FILTERED (by category)
- hitHighlightHtml: SANITIZED (XSS protection)
```

### 3. **Transitive Dependency Chain**
Show the complete chain:
```
FullTextSearchResults
  → Used by: IHealthcareDocument (HCC-onbase-healthcare-api)
    → Used by: IHealthcareCollection
      → Used by: ContentAdapter (HHSS)
        → Used by: ContentController
          → Transformed to: PatientContents (PAX-hcw-api)
            → Used by: PatientContentRepository (BFF)
              → Transformed to: PatientContent[] (Angular)
                → Used by: FullTextSearchResultsComponent
```

### 4. **Cross-Repository Impact Analysis**
If the `FullTextSearchResults` model changes (e.g., add a new property `relevanceScore: number`):
- Which files in which repositories must be updated?
- What is the blast radius of the change?
- Are there any breaking changes vs. non-breaking additions?

### 5. **Data Transformation Map**
Document how the model is transformed at each boundary:

```
┌─────────────────────┬──────────────────────┬─────────────────────────┐
│ Repository          │ Input Type           │ Output Type             │
├─────────────────────┼──────────────────────┼─────────────────────────┤
│ HCC-onbase-...      │ OnBase Document      │ FullTextSearchResults   │
├─────────────────────┼──────────────────────┼─────────────────────────┤
│ HHSS               │ FullTextSearchResults│ ContentCollection       │
├─────────────────────┼──────────────────────┼─────────────────────────┤
│ PAX-hcw-api        │ ContentCollection    │ PatientContents         │
├─────────────────────┼──────────────────────┼─────────────────────────┤
│ PAX-astra-bff      │ PatientContents      │ PatientContents (cached)│
├─────────────────────┼──────────────────────┼─────────────────────────┤
│ PAX-astra (UI)     │ PatientContents      │ UI ViewModel            │
└─────────────────────┴──────────────────────┴─────────────────────────┘
```

## Acceptance Criteria
1. **Complete Discovery**: Find ALL classes across ALL 5 repositories that reference `FullTextSearchResults`
2. **Usage Patterns**: Show actual usage patterns through text analysis
3. **Transitive Dependencies**: Include classes that don't directly use the model but depend on classes that do
4. **Property-Level Detail**: Document which specific properties are used at each layer
5. **Mermaid Diagrams**: Visual dependency graph and data flow diagram
6. **Impact Analysis**: Clear assessment of change blast radius

## Discovery Strategy (Using Scripts and Grep Only)

1. **Find the source definition**:
   ```powershell
   # Find all files with "FullTextSearchResults"
   rg "FullTextSearchResults" --files-with-matches
   
   # Or use grep recursively
   grep -r "FullTextSearchResults" C:\projects\github\
   ```

2. **Direct usage search**:
   ```powershell
   # Search for all occurrences with context
   rg "FullTextSearchResults" -A 5 -B 5
   
   # Count occurrences per file
   rg "FullTextSearchResults" --count-matches
   ```

3. **Property usage analysis**:
   ```powershell
   # Search for property access patterns
   rg "\.hitCount|\.fullTextPageDataItems|\.hitHighlightHtml"
   
   # Find where properties are read/written
   rg "(\.hitCount|fullTextPageDataItems)" --context 3
   ```

4. **Transformation tracking**:
   ```powershell
   # Find converter/mapper classes
   rg "class.*Converter|class.*Mapper" --type cs
   
   # Find transformation methods
   rg "ToPatientContents|ToHealthcareDocument" --type cs
   ```

5. **Transitive dependency discovery**:
   ```powershell
   # Find classes using PatientContents (which uses FullTextSearchResults)
   rg "PatientContents" --type cs
   
   # Build dependency chains manually
   ```

6. **Cross-repository aggregation**:
   ```powershell
   # Create a PowerShell script to aggregate findings
   Get-ChildItem -Recurse -Filter "*.cs","*.ts" | 
     Select-String "FullTextSearchResults" |
     Group-Object Path |
     Format-Table Count, Name
   ```

## Expected Output
- Save analysis to: `C:\projects\github\FullTextSearchResults-Dependency-Analysis-NoMCP-[TIMESTAMP].md`
- Include all 5 deliverables listed above
- Professional Mermaid diagrams
- Discovery process notes (which grep patterns were most useful)

## Comparison Baseline
The MCP-based analysis found:
- **10 source definitions** across repositories
- **40+ transitive dependencies**
- **7 data transformation points**
- **5 repositories** with complete coverage

Can you match or exceed this using only grep/ripgrep and scripts?

## Success Metrics
- **Completeness**: Find the same number of dependencies
- **Accuracy**: No false positives from text matching
- **Efficiency**: How long does it take vs. MCP?
- **Quality**: Same level of detail and diagrams

## Expected Challenges
- **Text matching limitations**: Distinguishing between variable names vs. type names
- **Multiple representations**: Same data in TypeScript (UI) vs. C# (backend)
- **Property aliases**: Properties renamed during transformation
- **Nested dependencies**: Classes indirectly affected by changes

## Reminder of Restrictions
- ❌ NO MCP servers (recursive-context, serena, ref, or any others)
- ✅ Custom scripts ARE allowed (PowerShell, Python, bash, ripgrep)
- ✅ Standard text search tools allowed (grep, rg, find, ack)
- 📝 Document which tools/patterns were most effective
