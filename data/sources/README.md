# Official data provenance

Every production data bundle must follow:

```text
source → normalized → validated → seed/import
```

The current `data/normalized/` bundle is the Cục Cảnh sát giao thông 2025
question bank: 600 questions, 60 critical questions, and versioned B/C1
blueprints. The source PDF is not committed; it is an input artifact for the
extractor. The generated JSON records the canonical CSGT URL, the retrieval
mirror used in this workspace, retrieval/effective dates, SHA-256, and source
page for each question.

## Rebuild the question bank

Install `pdfplumber` in a local tooling environment, obtain the PDF from the
official CSGT publication, and run:

```bash
python data/scripts/extract/extract_csgt_2025.py \
  --input /path/to/official-600-question-bank.pdf \
  --output data/normalized/question-banks/v1.json
python data/scripts/validate/validate_question_bank.py
```

The extractor reads the underlines in the PDF as the answer key and fails if
it cannot find exactly one answer for every question. It does not import
competitor explanations, watermarks, or proprietary assets. Explanations and
memory tips remain editable Question Bank fields and are blank when they are
not present in the official source.

## Canonical references

- [Bộ 600 câu hỏi — Cục Cảnh sát giao thông](https://www.csgt.vn/)
- [Công văn 2262/CSGT-P5 and exam structure — Báo Điện tử Chính phủ](https://xaydungchinhsach.chinhphu.vn/huong-dan-su-dung-bo-600-cau-hoi-dung-de-sat-hach-lai-xe-co-gioi-duong-bo-119250513110514585.htm)

The mirror is recorded only for reproducibility of the retrieval artifact;
the official CSGT publication and the Government portal remain the source of
truth.
