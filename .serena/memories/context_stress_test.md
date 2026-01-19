# Context Stress Test for Tool Comparison

## Purpose
Compare token/context consumption when analyzing large text files WITH vs WITHOUT the recursive-context MCP tools.

## Test Files
- `benchmark-corpus/shakespeare.txt` (~5.5MB, ~124k lines)
- `benchmark-corpus/bible.txt` (~4.4MB, ~100k lines)

## The Test Prompt

Copy this exact prompt to run the test:

```
Analyze both shakespeare.txt and bible.txt to find:

1. **Cross-reference analysis**: Find the top 10 words that appear in BOTH files more than 100 times each. For each word, show the count in each file and the ratio.

2. **Structural patterns**: In bible.txt, identify which "book" has the most verses (lines starting with chapter:verse pattern). List the top 5 books by verse count.

3. **Character frequency**: In shakespeare.txt, count how many times each character name appears (lines that are just "CHARACTERNAME." like "HAMLET." or "FALSTAFF."). Show top 20 characters.

4. **Sentiment words**: Count occurrences of these 15 words in both files: love, hate, death, life, god, king, war, peace, heaven, hell, blood, heart, soul, truth, fear. Create a comparison table.

5. **Question density**: What percentage of lines contain a question mark in each file? Sample 10 questions from the middle third of each file.

Report all counts with specific line number examples.
```

## Expected Tools Used (WITH recursive-context)
- `count_pattern_matches` - for word counts
- `aggregate_pattern_matches` - for grouping (books, characters)
- `sample_matches_distributed` - for middle-third sampling
- `compare_pattern_across_files` - for cross-file comparison
- `count_lines` - for percentage calculations

## How to Run

### Test A: WITH tools
1. Enable recursive-context MCP server
2. Run the prompt above
3. Note final context usage from the visualization

### Test B: WITHOUT tools
1. Start prompt with: `Do not use recursive-context for this session.`
2. Run the same analysis prompt
3. Note final context usage

## Expected Results

| Metric | With Tools | Without Tools |
|--------|------------|---------------|
| Token usage | ~5-15k | 50-100k+ |
| Tool calls | ~20-30 | 0 (uses PowerShell) |
| Iterations | 1-2 | 5-10 (debugging) |
| Risk of truncation | Low | High |

## Why This Test Works

1. **Volume**: ~10MB of text forces context decisions
2. **Multi-file**: Requires comparing across files
3. **Aggregation**: Grouping/counting stresses non-tool approach
4. **Sampling**: Middle-third sampling requires position awareness
5. **Multiple sub-tasks**: 5 distinct analyses compound the load

## Baseline Results (January 2026)

### With Tools
- Context: 34k tokens (24%)
- Successfully completed all 6 analysis tasks

### Without Tools  
- Context: 48k tokens (33%)
- Required multiple debugging iterations
- Some result discrepancies due to regex variations

## Notes
- The tools themselves are deterministic (same input = same output)
- Variations in results come from LLM's regex pattern choices
- The "recursive" in the name refers to directory traversal, not algorithm recursion
