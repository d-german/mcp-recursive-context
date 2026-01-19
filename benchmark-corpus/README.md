# Benchmark Corpus

Text corpora for benchmarking the recursive-context MCP tools against other search tools.

## Files

| File | Source | Size | Description |
|------|--------|------|-------------|
| `shakespeare.txt` | [Project Gutenberg #100](https://www.gutenberg.org/ebooks/100) | 5.6 MB | Complete Works of Shakespeare |
| `bible.txt` | [Project Gutenberg #10](https://www.gutenberg.org/ebooks/10) | 4.4 MB | King James Bible |

---

## Verified Test Prompts

These prompts have **exact, verified answers**. Use them to test AI agents using different search tools.

### 📊 LEVEL 1: Simple Pattern Matching

---

#### Test 1.1: Basic Word Count
**Prompt:**
> "How many lines in shakespeare.txt contain the word 'love' (case-sensitive)?"

**Expected Answer:** `2,223 lines`

---

#### Test 1.2: Case-Sensitive Matching
**Prompt:**
> "How many lines in bible.txt contain 'LORD' in all uppercase?"

**Expected Answer:** `6,419 lines`

---

#### Test 1.3: Character Dialogue Detection
**Prompt:**
> "How many times does HAMLET speak in shakespeare.txt? (Count lines starting with 'HAMLET.')"

**Expected Answer:** `361 lines`

---

### 📊 LEVEL 2: Structural Analysis

---

#### Test 2.1: Scene Counting
**Prompt:**
> "How many scenes are there in the Complete Works of Shakespeare? Count lines that start with 'SCENE'."

**Expected Answer:** `791 scenes`

---

#### Test 2.2: Act Counting
**Prompt:**
> "How many acts are there across all Shakespeare plays? Count lines starting with 'ACT'."

**Expected Answer:** `284 acts`

---

#### Test 2.3: Stage Directions - Entrances
**Prompt:**
> "How many stage entrance directions are there in Shakespeare? Count lines containing 'Enter '."

**Expected Answer:** `2,263 entrances`

---

#### Test 2.4: Stage Directions - Exits
**Prompt:**
> "How many 'Exit' stage directions appear in shakespeare.txt?"

**Expected Answer:** `1,026 exits`

---

#### Test 2.5: Group Exits
**Prompt:**
> "How many 'Exeunt' (plural exit) directions appear in shakespeare.txt?"

**Expected Answer:** `1,086 exeunts`

---

### 📊 LEVEL 3: Multi-Character Comparison

---

#### Test 3.1: Character Comparison
**Prompt:**
> "Compare speaking lines: How many lines does HAMLET speak vs MACBETH vs ROMEO vs JULIET? (Count lines starting with 'CHARACTER.')"

**Expected Answer:**
| Character | Lines |
|-----------|-------|
| HAMLET | 361 |
| MACBETH | 205 |
| ROMEO | 163 |
| JULIET | 125 |

---

#### Test 3.2: Archaic Pronouns Comparison
**Prompt:**
> "In shakespeare.txt, compare the frequency of archaic pronouns: how many lines contain 'thee' vs 'thou' vs 'thy'?"

**Expected Answer:**
| Pronoun | Lines |
|---------|-------|
| `thou` | 6,478 |
| `thy` | 4,204 |
| `thee` | 3,459 |

---

### 📊 LEVEL 4: Regex Pattern Matching

---

#### Test 4.1: Questions in Shakespeare
**Prompt:**
> "How many lines in shakespeare.txt end with a question mark?"

**Expected Answer:** `8,138 questions`

---

#### Test 4.2: Exclamations in Shakespeare
**Prompt:**
> "How many lines in shakespeare.txt end with an exclamation mark?"

**Expected Answer:** `4,174 exclamations`

---

#### Test 4.3: Questions in Bible
**Prompt:**
> "How many lines in bible.txt end with a question mark?"

**Expected Answer:** `311 questions`

---

#### Test 4.4: Verse References
**Prompt:**
> "How many lines in bible.txt contain a verse reference pattern like '1:1' or '23:45' (number:number)?"

**Expected Answer:** `37,028 verses`

---

#### Test 4.5: Stage Directions (Bracketed)
**Prompt:**
> "How many stage directions in brackets [like this] appear in shakespeare.txt? Count lines starting with '['."

**Expected Answer:** `2,734 bracketed directions`

---

### 📊 LEVEL 5: Religious/Literary Analysis

---

#### Test 5.1: Commandments Language
**Prompt:**
> "How many lines in bible.txt contain 'thou shalt'?"

**Expected Answer:** `898 lines`

---

#### Test 5.2: Prohibitions
**Prompt:**
> "How many lines in bible.txt contain 'shall not'?"

**Expected Answer:** `697 lines`

---

#### Test 5.3: Amen Count
**Prompt:**
> "How many times does 'Amen' appear in bible.txt?"

**Expected Answer:** `74 lines`

---

#### Test 5.4: Jesus References
**Prompt:**
> "How many lines in bible.txt mention 'Jesus'?"

**Expected Answer:** `966 lines`

---

### 📊 LEVEL 6: Complex Multi-Step Analysis

---

#### Test 6.1: Tragedy vs Comedy Detection
**Prompt:**
> "In shakespeare.txt, are there more lines containing 'death' or lines containing 'love'? What's the ratio?"

**Expected Answer:**
- `love`: 2,223 lines
- `death`: 944 lines
- **Ratio: 2.35:1** (love appears 2.35x more than death)

---

#### Test 6.2: Play Title Detection
**Prompt:**
> "How many lines in shakespeare.txt start with 'THE ' (uppercase, followed by space)? These typically indicate play/poem titles."

**Expected Answer:** `252 lines`

---

#### Test 6.3: Character Line Density
**Prompt:**
> "Which character speaks more: HAMLET or MACBETH? By how many lines?"

**Expected Answer:**
- HAMLET: 361 lines
- MACBETH: 205 lines
- **HAMLET speaks 156 more lines** (76% more)

---

#### Test 6.4: Book Mentions
**Prompt:**
> "How many times is 'Genesis' mentioned in bible.txt? How about 'Revelation'?"

**Expected Answer:**
- Genesis: `2 lines`
- Revelation: `3 lines`

---

#### Test 6.5: Phrase Pattern "To be"
**Prompt:**
> "How many lines in shakespeare.txt contain the phrase 'to be' or 'To be'?"

**Expected Answer:** `1,350 lines`

---

### 📊 LEVEL 7: Cross-File Aggregation

---

#### Test 7.1: Combined Corpus Size
**Prompt:**
> "What is the total line count across both shakespeare.txt and bible.txt?"

**Expected Answer:** Run on both files and sum results

---

#### Test 7.2: Word Presence Comparison
**Prompt:**
> "The word 'Jesus' - how many lines contain it in shakespeare.txt vs bible.txt?"

**Expected Answer:**
- shakespeare.txt: `0 lines`
- bible.txt: `966 lines`

---

#### Test 7.3: Indentation Analysis
**Prompt:**
> "How many lines in shakespeare.txt start with two spaces (indented verse lines)?"

**Expected Answer:** `196,395 lines`

---

## Ground Truth Reference Table

| File | Pattern | Lines Matching | Regex |
|------|---------|----------------|-------|
| shakespeare.txt | `love` | 2,223 | `love` |
| shakespeare.txt | `death` | 944 | `death` |
| shakespeare.txt | `king` | 1,788 | `king` |
| shakespeare.txt | `thee` | 3,459 | `thee` |
| shakespeare.txt | `thou` | 6,478 | `thou` |
| shakespeare.txt | `thy` | 4,204 | `thy` |
| shakespeare.txt | `^SCENE` | 791 | `^SCENE` |
| shakespeare.txt | `^ACT` | 284 | `^ACT` |
| shakespeare.txt | `Enter ` | 2,263 | `Enter ` |
| shakespeare.txt | `Exit` | 1,026 | `Exit` |
| shakespeare.txt | `Exeunt` | 1,086 | `Exeunt` |
| shakespeare.txt | `HAMLET.` | 361 | `^HAMLET\.` |
| shakespeare.txt | `MACBETH.` | 205 | `^MACBETH\.` |
| shakespeare.txt | `ROMEO.` | 163 | `^ROMEO\.` |
| shakespeare.txt | `JULIET.` | 125 | `^JULIET\.` |
| shakespeare.txt | `?$` | 8,138 | `\?$` |
| shakespeare.txt | `!$` | 4,174 | `!$` |
| shakespeare.txt | `^THE ` | 252 | `^THE ` |
| shakespeare.txt | `[Tt]o be` | 1,350 | `[Tt]o be` |
| shakespeare.txt | `^\[` | 2,734 | `^\[` |
| shakespeare.txt | `^  ` | 196,395 | `^  ` |
| bible.txt | `LORD` | 6,419 | `LORD` |
| bible.txt | `Jesus` | 966 | `Jesus` |
| bible.txt | `thou shalt` | 898 | `thou shalt` |
| bible.txt | `shall not` | 697 | `shall not` |
| bible.txt | `Amen` | 74 | `Amen` |
| bible.txt | `?$` | 311 | `\?$` |
| bible.txt | `\d+:\d+` | 37,028 | `[0-9]+:[0-9]+` |
| bible.txt | `Genesis` | 2 | `Genesis` |
| bible.txt | `Revelation` | 3 | `Revelation` |

---

## How to Verify Answers

### Windows (findstr)
```cmd
findstr /R /C:"pattern" benchmark-corpus\shakespeare.txt | find /c /v ""
```

### PowerShell
```powershell
(Select-String -Path "benchmark-corpus\shakespeare.txt" -Pattern "pattern").Count
```

### Linux/Mac (grep)
```bash
grep -c "pattern" benchmark-corpus/shakespeare.txt
```

---

## License

These texts are in the public domain (Project Gutenberg).
