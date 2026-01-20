#!/usr/bin/env python3
"""
Extract QASPER papers to individual text files for use with RecursiveContext.Mcp tools.

This creates one .txt file per paper, making them searchable with pattern matching tools.
"""

import json
import os
from pathlib import Path

def extract_papers(json_path: str, output_dir: str) -> dict:
    """Extract papers from QASPER JSON to individual text files."""
    
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    os.makedirs(output_dir, exist_ok=True)
    
    stats = {"papers": 0, "questions": 0, "total_chars": 0}
    
    for paper_id, paper in data.items():
        # Build the full text content
        lines = []
        lines.append(f"# {paper['title']}")
        lines.append(f"")
        lines.append(f"**Paper ID:** {paper_id}")
        lines.append(f"")
        lines.append(f"## Abstract")
        lines.append(f"")
        lines.append(paper['abstract'])
        lines.append(f"")
        
        # Add all sections
        for section in paper.get('full_text', []):
            section_name = section.get('section_name', 'Unnamed Section')
            lines.append(f"## {section_name}")
            lines.append(f"")
            for para in section.get('paragraphs', []):
                if para.strip():
                    lines.append(para)
                    lines.append(f"")
        
        content = '\n'.join(lines)
        
        # Write to file
        output_path = os.path.join(output_dir, f"{paper_id}.txt")
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        stats["papers"] += 1
        stats["questions"] += len(paper.get('qas', []))
        stats["total_chars"] += len(content)
    
    return stats

def extract_questions(json_path: str, output_path: str) -> int:
    """Extract questions with ground truth answers to a separate file."""
    
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    questions = []
    
    for paper_id, paper in data.items():
        for qa in paper.get('qas', []):
            question_data = {
                "paper_id": paper_id,
                "paper_title": paper['title'],
                "question_id": qa.get('question_id', ''),
                "question": qa.get('question', ''),
                "answers": []
            }
            
            # Extract all answers for this question
            for answer in qa.get('answers', []):
                ans = answer.get('answer', {})
                answer_data = {
                    "unanswerable": ans.get('unanswerable', False),
                    "yes_no": ans.get('yes_no'),
                    "free_form_answer": ans.get('free_form_answer', ''),
                    "extractive_spans": ans.get('extractive_spans', []),
                    "evidence": ans.get('evidence', [])
                }
                question_data["answers"].append(answer_data)
            
            questions.append(question_data)
    
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(questions, f, indent=2, ensure_ascii=False)
    
    return len(questions)

def main():
    base_dir = Path(__file__).parent
    source_dir = base_dir / "source-of-truth"
    papers_dir = base_dir / "papers"
    
    print("Extracting QASPER papers to individual text files...")
    print("=" * 60)
    
    # Process each split
    splits = [
        ("qasper-train-v0.3.json", "train"),
        ("qasper-dev-v0.3.json", "validation"),
        ("qasper-test-v0.3.json", "test")
    ]
    
    total_stats = {"papers": 0, "questions": 0, "total_chars": 0}
    
    for json_file, split_name in splits:
        json_path = source_dir / json_file
        if not json_path.exists():
            print(f"  Skipping {split_name}: {json_file} not found")
            continue
        
        # Extract papers
        split_papers_dir = papers_dir / split_name
        stats = extract_papers(str(json_path), str(split_papers_dir))
        
        # Extract questions with ground truth
        questions_path = base_dir / f"questions_{split_name}.json"
        num_questions = extract_questions(str(json_path), str(questions_path))
        
        print(f"  {split_name}:")
        print(f"    Papers: {stats['papers']}")
        print(f"    Questions: {num_questions}")
        print(f"    Total chars: {stats['total_chars']:,}")
        
        total_stats["papers"] += stats["papers"]
        total_stats["questions"] += num_questions
        total_stats["total_chars"] += stats["total_chars"]
    
    print("=" * 60)
    print(f"Total: {total_stats['papers']} papers, {total_stats['questions']} questions")
    print(f"Total text: {total_stats['total_chars']:,} characters ({total_stats['total_chars'] / 1_000_000:.1f} MB)")
    print(f"\nPapers extracted to: {papers_dir}")
    print(f"Questions extracted to: {base_dir}/questions_*.json")

if __name__ == "__main__":
    main()
