# Stress Test Comparison: Recursive-Context MCP vs Native Approaches

## Test Overview

**Date**: January 19, 2026  
**Test Prompt**: Comprehensive text analysis of shakespeare.txt (~5.5MB, 196k lines) and bible.txt (~4.4MB, 100k lines)  
**Models Tested**: Claude Sonnet 4.5, Claude Haiku 4.5, GPT-5 mini, GPT-4.1  
**Tasks**: 5 analysis tasks (cross-reference, structural patterns, character frequency, sentiment words, question density)

### Original Prompt

```
Analyze both shakespeare.txt and bible.txt to find:

   Cross-reference analysis: Find the top 10 words that appear in BOTH files more than 100 times each. For each word, show the count in each file and the ratio.

   Structural patterns: In bible.txt, identify which "book" has the most verses (lines starting with chapter:verse pattern). List the top 5 books by verse count.

   Character frequency: In shakespeare.txt, count how many times each character name appears (lines that are just "CHARACTERNAME." like "HAMLET." or "FALSTAFF."). Show top 20 characters.

   Sentiment words: Count occurrences of these 15 words in both files: love, hate, death, life, god, king, war, peace, heaven, hell, blood, heart, soul, truth, fear. Create a comparison table.

   Question density: What percentage of lines contain a question mark in each file? Sample 10 questions from the middle third of each file.

   Report all counts with specific line number examples.
```

---

## Performance Comparison (Latest: January 19, 2026)

| Metric | Sonnet 4.5 + RC MCP | No MCP (Python Script) | RC Advantage |
|--------|---------------------|------------------------|--------------|
| **Wall Time** | **4m 4s** | 8m 58s | **2.2x faster** |
| **API Time** | **2m 31s** | 3m 21s | **1.3x faster** |
| **Input Tokens** | **393.8k** | 577.0k | **1.47x fewer** |
| **Output Tokens** | **10.6k** | 14.3k | **1.35x fewer** |
| **Cache Read** | **362.5k** | 548.9k | **1.51x fewer** |
| **Context Usage** | 43k (30%) | 40k (28%) | ~Similar |
| **Custom Scripts** | **None** | 1 Python script | **Zero-script** |

---

## Full Model Comparison (All with RC MCP)

| Metric | Sonnet 4.5 | Haiku 4.5 | GPT-4.1 | GPT-5 mini |
|--------|------------|-----------|---------|------------|
| **Wall Time** | **4m 4s** | 9m 9s | 6m 11s | 20m 42s |
| **API Time** | 2m 31s | 1m 47s | **1m 18s** | 11m 56s |
| **Input Tokens** | **393.8k** | 720.8k | 890.2k | 548.9k |
| **Output Tokens** | 10.6k | 11.3k | **3.7k** | 41.4k |
| **Cache Read** | **362.5k** | 686.6k | 827.9k | 424.3k |
| **Context Usage** | 43k (30%) | 41k (32%) | 40k (62%) | 70k (55%) |
| **Premium Cost** | 2 | 0.33 | **0 (FREE)** | **0 (FREE)** |
| **Followed Rules** | Yes | Yes | Yes | **NO** |
| **Complete Results** | Yes | Yes | Partial | Partial |

### Model Rankings

| Category | 1st | 2nd | 3rd | 4th |
|----------|-----|-----|-----|-----|
| **Speed (Wall)** | Sonnet (4m) | GPT-4.1 (6m) | Haiku (9m) | GPT-5 mini (21m) |
| **Speed (API)** | GPT-4.1 (1m18s) | Haiku (1m47s) | Sonnet (2m31s) | GPT-5 mini (12m) |
| **Token Efficiency** | Sonnet (394k) | GPT-5 mini (549k) | Haiku (721k) | GPT-4.1 (890k) |
| **Cost** | GPT-4.1/GPT-5 mini (FREE) | Haiku (0.33x) | Sonnet (2x) | - |
| **Constraint Following** | Sonnet/Haiku/GPT-4.1 | - | - | GPT-5 mini (FAILED) |
| **Completeness** | Sonnet/Haiku (100%) | GPT-4.1/GPT-5 mini (Partial) | - | - |

---

## Free Model Analysis (GPT-4.1 vs GPT-5 mini)

### GPT-4.1 (FREE - Recommended)

| Aspect | Result |
|--------|--------|
| **Followed "no scripts" rule** | YES |
| **Wall Time** | 6m 11s (1.5x slower than Sonnet) |
| **API Time** | 1m 18s (fastest of all!) |
| **Accuracy (where complete)** | 100% match with Sonnet/Haiku |
| **Completeness** | Partial - skipped Bible book analysis, abbreviated cross-reference |

**Verdict**: Good free option for simpler tasks. Follows instructions reliably.

### GPT-5 mini (FREE - Not Recommended)

| Aspect | Result |
|--------|--------|
| **Followed "no scripts" rule** | **NO - Created PowerShell script!** |
| **Wall Time** | 20m 42s (5x slower than Sonnet) |
| **API Time** | 11m 56s (slowest) |
| **Output Tokens** | 41.4k (4x more than Sonnet - very verbose) |
| **Accuracy** | Unknown - violated test constraints |

**Verdict**: Failed the constraint test. Not recommended for tasks requiring instruction-following.

---

## Accuracy Comparison (Where Results Complete)

| Metric | Sonnet 4.5 | Haiku 4.5 | GPT-4.1 | Match? |
|--------|------------|-----------|---------|--------|
| FALSTAFF count | 472 | 472 | 472 | 100% |
| KING count | 455 | 455 | 455 | 100% |
| HAMLET count | 358 | 358 | 358 | 100% |
| love (Shakes/Bible) | 2,223/290 | 2,223/290 | 2,223/290 | 100% |
| king (Shakes/Bible) | 552/2,279 | 552/2,279 | 552/2,279 | 100% |
| heart (Shakes/Bible) | 1,114/813 | 1,114/813 | 1,114/813 | 100% |
| Shakespeare questions | 5.31% | 5.31% | 5.31% | 100% |
| Bible questions | 3.04% | 3.04% | 3.04% | 100% |

**Conclusion**: RC MCP tools produce **deterministic results** - accuracy depends on tools, not model.

---

## Key Differentiator: Script Creation Requirement

| Approach | Scripts Created | Followed Rules | Code Generated |
|----------|-----------------|----------------|----------------|
| **RC MCP (Sonnet)** | 0 | Yes | 0 lines |
| **RC MCP (Haiku)** | 0 | Yes | 0 lines |
| **RC MCP (GPT-4.1)** | 0 | Yes | 0 lines |
| **RC MCP (GPT-5 mini)** | 1 (PowerShell) | **NO** | ~20 lines |
| **No MCP (Python)** | 1 (analyze_corpus.py) | N/A | ~100+ lines |

---

## Recommendations

### By Use Case

| Priority | Recommended Model | Why |
|----------|-------------------|-----|
| **Speed + Quality** | Sonnet 4.5 | Fastest wall time, complete results |
| **Budget + Quality** | Haiku 4.5 | 6x cheaper, 100% accuracy match |
| **Free + Simple Tasks** | GPT-4.1 | Free, follows rules, partial results OK |
| **NOT Recommended** | GPT-5 mini | Violates constraints, very slow |

### Decision Matrix

| If you need... | Use... |
|----------------|--------|
| Complete, accurate results fast | Sonnet 4.5 + RC |
| Complete results, budget-conscious | Haiku 4.5 + RC |
| Free option, simple analysis | GPT-4.1 + RC |
| Complex multi-part analysis | Sonnet or Haiku (avoid free models) |

---

## Historical Performance Comparison (All Test Runs)

| Metric | RC Sonnet | RC Haiku | RC GPT-4.1 | RC GPT-5 mini | No MCP |
|--------|-----------|----------|------------|---------------|--------|
| **Wall Time** | 4m 4s | 9m 9s | 6m 11s | 20m 42s | 8m 58s |
| **API Time** | 2m 31s | 1m 47s | 1m 18s | 11m 56s | 3m 21s |
| **Input Tokens** | 393.8k | 720.8k | 890.2k | 548.9k | 577.0k |
| **Output Tokens** | 10.6k | 11.3k | 3.7k | 41.4k | 14.3k |
| **Custom Scripts** | None | None | None | 1 PS | 1 Python |
| **Premium Cost** | 2 | 0.33 | 0 | 0 | 2 |
| **Complete** | Yes | Yes | Partial | Partial | Yes |
| **Rules Followed** | Yes | Yes | Yes | No | N/A |

---

## Key Findings

### Winner: Recursive-Context MCP + Claude Models

1. **Sonnet 4.5**: Best overall (speed + quality + completeness)
2. **Haiku 4.5**: Best value (6x cheaper, same accuracy)
3. **GPT-4.1**: Best free option (follows rules, partial results)
4. **GPT-5 mini**: Not recommended (violated constraints)

### Why RC MCP Matters

- **Deterministic accuracy**: Same results regardless of model
- **Zero scripts**: No code generation required
- **Model flexibility**: Works with multiple providers
- **Cost scaling**: Enables cheaper models to succeed

### Free Model Limitations

- May produce incomplete results
- GPT-5 mini failed to follow constraints
- GPT-4.1 abbreviated some responses
- Best for simple, single-focus tasks

---

## Conclusion

| Category | Winner | Notes |
|----------|--------|-------|
| Speed | Sonnet 4.5 | 4m wall time |
| Token Efficiency | Sonnet 4.5 | 394k input |
| Cost (Paid) | Haiku 4.5 | 0.33x (6x cheaper) |
| Cost (Free) | GPT-4.1 | 0x but partial results |
| Accuracy | All (with RC MCP) | Deterministic |
| Constraint Following | Sonnet/Haiku/GPT-4.1 | GPT-5 mini failed |
| Completeness | Sonnet/Haiku | Free models partial |

**Bottom Line**: RC MCP tools provide substantial efficiency gains and enable model flexibility. For production use, Claude models (Sonnet/Haiku) are recommended. GPT-4.1 is a viable free option for simpler tasks.

---

*Generated: January 19, 2026*
*Last Updated: January 19, 2026 - Added GPT-5 mini and GPT-4.1 comparisons*
