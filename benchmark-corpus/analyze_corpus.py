#!/usr/bin/env python3
"""Comprehensive corpus analysis for shakespeare.txt and bible.txt"""

import re
from collections import Counter, defaultdict
from typing import Dict, List, Tuple

def load_file(filename: str) -> Tuple[List[str], str]:
    """Load file and return lines and full text"""
    with open(filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    return lines, ''.join(lines)

def extract_words(text: str) -> List[str]:
    """Extract all words (3+ letters) from text"""
    return [word.lower() for word in re.findall(r'\b[a-zA-Z]{3,}\b', text)]

def cross_reference_analysis(s_lines, s_text, b_lines, b_text):
    """Find top 10 words appearing >100 times in BOTH files"""
    print("\n=== 1. CROSS-REFERENCE ANALYSIS ===")
    
    # Count word frequencies
    s_words = extract_words(s_text)
    b_words = extract_words(b_text)
    
    s_freq = Counter(s_words)
    b_freq = Counter(b_words)
    
    print(f"Shakespeare: {len(s_words)} total words, {len(s_freq)} unique")
    print(f"Bible: {len(b_words)} total words, {len(b_freq)} unique\n")
    
    # Find common words >100 in both
    common = []
    for word in s_freq:
        if word in b_freq and s_freq[word] > 100 and b_freq[word] > 100:
            common.append({
                'word': word,
                'shakespeare': s_freq[word],
                'bible': b_freq[word],
                'ratio': round(s_freq[word] / b_freq[word], 2),
                'total': s_freq[word] + b_freq[word]
            })
    
    common.sort(key=lambda x: x['total'], reverse=True)
    top10 = common[:10]
    
    print("TOP 10 WORDS (>100 occurrences in both files):")
    print(f"{'Word':<15} {'Shakespeare':>12} {'Bible':>12} {'Ratio':>8}")
    print("-" * 52)
    for item in top10:
        print(f"{item['word']:<15} {item['shakespeare']:>12} {item['bible']:>12} {item['ratio']:>8}")
    
    # Show example lines for top 3
    print("\nExample lines for top 3 words:")
    for item in top10[:3]:
        word = item['word']
        pattern = re.compile(r'\b' + re.escape(word) + r'\b', re.IGNORECASE)
        
        for i, line in enumerate(s_lines[:10000]):
            if pattern.search(line) and line.strip():
                print(f"  '{word}' Shakespeare L{i+1}: {line.strip()[:65]}...")
                break
        
        for i, line in enumerate(b_lines[:10000]):
            if pattern.search(line) and line.strip():
                print(f"  '{word}' Bible L{i+1}: {line.strip()[:65]}...")
                break

def structural_patterns_bible(b_lines):
    """Find top 5 books by verse count in Bible"""
    print("\n\n=== 2. STRUCTURAL PATTERNS (Bible) ===")
    
    # Pattern for verses: chapter:verse at start of line or after space
    verse_pattern = re.compile(r'(\d+):(\d+)')
    
    # Track current book and verse counts
    book_verses = defaultdict(int)
    current_book = None
    
    # Find book markers (lines that introduce books)
    book_pattern = re.compile(r'^(?:The )?(First|Second|Third|Fourth|Fifth|Book of|The Book of the Prophet)?\s*(.+)$')
    
    for i, line in enumerate(b_lines):
        # Check if this is a book title line
        if 'Book of' in line or (i < 500 and ':' not in line and len(line.strip()) > 5 and len(line.strip()) < 100):
            # Potential book title
            stripped = line.strip()
            if stripped and not verse_pattern.search(line):
                # Look for known book patterns
                if any(marker in stripped for marker in ['Genesis', 'Exodus', 'Leviticus', 'Numbers', 'Deuteronomy', 
                                                          'Joshua', 'Judges', 'Ruth', 'Samuel', 'Kings', 'Chronicles',
                                                          'Ezra', 'Nehemiah', 'Esther', 'Job', 'Psalms', 'Proverbs',
                                                          'Ecclesiastes', 'Solomon', 'Isaiah', 'Jeremiah', 'Lamentations',
                                                          'Ezekiel', 'Daniel', 'Hosea', 'Joel', 'Amos', 'Obadiah',
                                                          'Jonah', 'Micah', 'Nahum', 'Habakkuk', 'Zephaniah', 'Haggai',
                                                          'Zechariah', 'Malachi', 'Matthew', 'Mark', 'Luke', 'John',
                                                          'Acts', 'Romans', 'Corinthians', 'Galatians', 'Ephesians',
                                                          'Philippians', 'Colossians', 'Thessalonians', 'Timothy',
                                                          'Titus', 'Philemon', 'Hebrews', 'James', 'Peter', 'Jude', 'Revelation']):
                    current_book = stripped[:50]  # Limit book name length
        
        # Count verses
        if verse_pattern.search(line) and current_book:
            book_verses[current_book] += 1
    
    # Get top 5 books by verse count
    top5_books = sorted(book_verses.items(), key=lambda x: x[1], reverse=True)[:5]
    
    print("TOP 5 BOOKS BY VERSE COUNT:")
    print(f"{'Book':<50} {'Verses':>8}")
    print("-" * 60)
    for book, count in top5_books:
        print(f"{book:<50} {count:>8}")
    
    # Show example verses from top book
    if top5_books:
        top_book = top5_books[0][0]
        print(f"\nExample verses from '{top_book}':")
        in_book = False
        count = 0
        for i, line in enumerate(b_lines):
            if top_book in line:
                in_book = True
            if in_book and verse_pattern.search(line) and count < 3:
                print(f"  L{i+1}: {line.strip()[:70]}...")
                count += 1
                if count >= 3:
                    break

def character_frequency_shakespeare(s_lines):
    """Count character names in Shakespeare"""
    print("\n\n=== 3. CHARACTER FREQUENCY (Shakespeare) ===")
    
    # Pattern for character names: all caps word(s) followed by period, on its own line
    char_pattern = re.compile(r'^\s*([A-Z][A-Z\s\-\']+)\.\s*$')
    
    characters = Counter()
    example_lines = {}
    
    for i, line in enumerate(s_lines):
        match = char_pattern.match(line)
        if match:
            char_name = match.group(1).strip()
            # Filter out false positives (Roman numerals, single letters, etc.)
            if len(char_name) >= 3 and char_name not in ['ACT', 'SCENE', 'THE', 'END']:
                characters[char_name] += 1
                if char_name not in example_lines:
                    example_lines[char_name] = i + 1
    
    top20 = characters.most_common(20)
    
    print("TOP 20 CHARACTERS BY SPEAKING FREQUENCY:")
    print(f"{'Character':<30} {'Count':>8} {'First Line':>12}")
    print("-" * 52)
    for char, count in top20:
        first_line = example_lines.get(char, 0)
        print(f"{char:<30} {count:>8} {first_line:>12}")

def sentiment_words_analysis(s_lines, s_text, b_lines, b_text):
    """Count sentiment words in both files"""
    print("\n\n=== 4. SENTIMENT WORDS COMPARISON ===")
    
    sentiment_words = ['love', 'hate', 'death', 'life', 'god', 'king', 'war', 'peace', 
                       'heaven', 'hell', 'blood', 'heart', 'soul', 'truth', 'fear']
    
    s_counts = {}
    b_counts = {}
    
    for word in sentiment_words:
        pattern = re.compile(r'\b' + re.escape(word) + r'\b', re.IGNORECASE)
        s_counts[word] = len(pattern.findall(s_text))
        b_counts[word] = len(pattern.findall(b_text))
    
    print(f"{'Word':<12} {'Shakespeare':>12} {'Bible':>12} {'Ratio':>8}")
    print("-" * 48)
    for word in sentiment_words:
        ratio = round(s_counts[word] / b_counts[word], 2) if b_counts[word] > 0 else 0
        print(f"{word:<12} {s_counts[word]:>12} {b_counts[word]:>12} {ratio:>8}")
    
    # Show example lines for 'love' and 'death'
    print("\nExample lines:")
    for word in ['love', 'death']:
        pattern = re.compile(r'\b' + re.escape(word) + r'\b', re.IGNORECASE)
        for i, line in enumerate(s_lines[:20000]):
            if pattern.search(line) and line.strip() and len(line.strip()) > 20:
                print(f"  '{word}' Shakespeare L{i+1}: {line.strip()[:60]}...")
                break
        for i, line in enumerate(b_lines[:20000]):
            if pattern.search(line) and line.strip() and len(line.strip()) > 20:
                print(f"  '{word}' Bible L{i+1}: {line.strip()[:60]}...")
                break

def question_density_analysis(s_lines, b_lines):
    """Analyze question marks in both files"""
    print("\n\n=== 5. QUESTION DENSITY ===")
    
    # Count questions
    s_questions = [(i+1, line.strip()) for i, line in enumerate(s_lines) if '?' in line and line.strip()]
    b_questions = [(i+1, line.strip()) for i, line in enumerate(b_lines) if '?' in line and line.strip()]
    
    s_pct = round(100 * len(s_questions) / len(s_lines), 2)
    b_pct = round(100 * len(b_questions) / len(b_lines), 2)
    
    print(f"Shakespeare: {len(s_questions)} questions out of {len(s_lines)} lines ({s_pct}%)")
    print(f"Bible: {len(b_questions)} questions out of {len(b_lines)} lines ({b_pct}%)")
    
    # Sample 10 questions from middle third
    s_middle_start = len(s_questions) // 3
    s_middle_end = 2 * len(s_questions) // 3
    s_sample = s_questions[s_middle_start:s_middle_end:max(1, (s_middle_end - s_middle_start) // 10)][:10]
    
    b_middle_start = len(b_questions) // 3
    b_middle_end = 2 * len(b_questions) // 3
    b_sample = b_questions[b_middle_start:b_middle_end:max(1, (b_middle_end - b_middle_start) // 10)][:10]
    
    print("\n10 sample questions from MIDDLE THIRD of Shakespeare:")
    for line_no, line in s_sample:
        print(f"  L{line_no}: {line[:70]}...")
    
    print("\n10 sample questions from MIDDLE THIRD of Bible:")
    for line_no, line in b_sample:
        print(f"  L{line_no}: {line[:70]}...")

def main():
    print("=" * 70)
    print("COMPREHENSIVE CORPUS ANALYSIS")
    print("=" * 70)
    
    # Load files
    print("\nLoading files...")
    s_lines, s_text = load_file('shakespeare.txt')
    b_lines, b_text = load_file('bible.txt')
    print(f"Shakespeare: {len(s_lines)} lines")
    print(f"Bible: {len(b_lines)} lines")
    
    # Run all analyses
    cross_reference_analysis(s_lines, s_text, b_lines, b_text)
    structural_patterns_bible(b_lines)
    character_frequency_shakespeare(s_lines)
    sentiment_words_analysis(s_lines, s_text, b_lines, b_text)
    question_density_analysis(s_lines, b_lines)
    
    print("\n" + "=" * 70)
    print("ANALYSIS COMPLETE")
    print("=" * 70)

if __name__ == '__main__':
    main()
