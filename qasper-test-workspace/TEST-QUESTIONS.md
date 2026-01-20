# QASPER Test Questions

These are example questions from the QASPER dataset for testing the RecursiveContext.Mcp tool.
**Important:** The ground truth answers are in `../qasper-dataset/questions_*.json` - don't peek!

---

## Test Case 1
**Paper:** 1909.00694 (in train/)
**Question:** What embedding methods do they use?
**Expected Answer Type:** Extractive

---

## Test Case 2
**Paper:** 1909.01515 (in train/)
**Question:** How many annotators labeled the data?
**Expected Answer Type:** Extractive (number)

---

## Test Case 3
**Paper:** 1909.04556 (in train/)
**Question:** Is there any baseline?
**Expected Answer Type:** Yes/No

---

## Test Case 4
**Paper:** 1909.05855 (in train/)
**Question:** Which datasets were used for experiments?
**Expected Answer Type:** Extractive (list)

---

## Test Case 5
**Paper:** 1909.07512 (in train/)
**Question:** What languages are considered?
**Expected Answer Type:** Extractive (list)

---

## How to Test

1. Open this folder (`qasper-test-workspace`) in VS Code
2. Start the MCP server (it will use recursive-con configured in .vscode/mcp.json)
3. Ask a question like:
   - "In paper 1909.00694, what embedding methods do they use?"
   - "Search through the papers to find which one discusses X..."
4. Compare the LLM's answer with ground truth in `../qasper-dataset/questions_train.json`

## Scoring

The original QASPER evaluation uses F1 score for extractive answers and accuracy for Yes/No.
You can use the evaluator script in `../qasper-dataset/source-of-truth/qasper_evaluator.py`.
