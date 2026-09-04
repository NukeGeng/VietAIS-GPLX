#!/usr/bin/env python3
"""Extract the 2025 CSGT question bank from its PDF.

The answer key in the source is represented by vector underlines. The
extractor intentionally fails closed when a question does not have exactly
one underlined option. The PDF is an input artifact and is not committed to
the repository; the generated JSON is reviewed by the normal data validator.

Requires: pdfplumber (kept out of the runtime and CI dependency graph).
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import uuid
from pathlib import Path
from typing import Any

import pdfplumber


OFFICIAL_URL = (
    "https://www.csgt.vn/upload/services/273963059_B%E1%BB%99%20600%20c%C3%A2u%20"
    "h%E1%BB%8Fi%20d%C3%B9ng%20cho%20s%C3%A1t%20h%E1%BA%A1ch%20l%C3%A1i%20xe%20c%C6%A1%20"
    "gi%E1%BB%9Bi%20%C4%91%C6%B0%E1%BB%9Dng%20b%E1%BB%99.pdf"
)
RETRIEVED_FROM = (
    "https://cms.luatvietnam.vn/uploaded/Others/2025/05/13/"
    "Bo_600_cau_hoi_danh_cho_sat_hach_lai_xe_co_gioi_duong_bo_-_LVN_1305094143.pdf"
)
QUESTION_NAMESPACE = uuid.UUID("f4a3d49e-5d7d-4fd9-a4e1-3d0d69dd48b1")
CRITICAL_QUESTIONS = {
    19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 30, 32, 34, 35, 47, 48, 52, 53,
    55, 58, 63, 64, 65, 66, 67, 68, 70, 71, 72, 73, 74, 85, 86, 87, 88,
    89, 90, 91, 92, 93, 97, 98, 102, 117, 163, 165, 167, 197, 198, 206,
    215, 226, 234, 245, 246, 252, 253, 254, 255, 260,
}
TOPICS = (
    (180, "Quy định chung và quy tắc giao thông đường bộ"),
    (205, "Văn hóa giao thông, đạo đức người lái xe, kỹ năng phòng cháy, chữa cháy và cứu hộ, cứu nạn"),
    (263, "Kỹ thuật lái xe"),
    (300, "Cấu tạo và sửa chữa"),
    (485, "Báo hiệu đường bộ"),
    (600, "Giải thế sa hình và kỹ năng xử lý tình huống giao thông"),
)


def topic_for(number: int) -> str:
    for last_number, topic in TOPICS:
        if number <= last_number:
            return topic
    raise ValueError(f"Question number is outside the source range: {number}")


def compact_text(words: list[dict[str, Any]]) -> str:
    parts: list[str] = []
    punctuation = {",", ".", ";", ":", "?", "!", ")", "]", "%"}
    for word in words:
        text = str(word["text"])
        if not text:
            continue
        if parts and text in punctuation:
            parts[-1] += text
        elif parts and parts[-1].endswith(("(", "[")):
            parts[-1] += text
        elif (
            parts
            and len(parts[-1]) == 1
            and parts[-1].lower() in "bcdfghklmnpqrstvx"
            and text[0].lower() in "aàáảãạăằắẳẵặâầấẩẫậeèéẻẽẹêềếểễệiìíỉĩịoòóỏõọôồốổỗộơờớởỡợpùúủũụưừứửữựyỳýỷỹỵ"
        ):
            parts[-1] += text
        else:
            parts.append(text)
    return " ".join(parts).strip()


def is_page_number(word: dict[str, Any]) -> bool:
    return word["top"] < 55 and 285 < word["x0"] < 330 and word["text"].isdigit()


def is_option_start(words: list[dict[str, Any]], index: int) -> bool:
    word = words[index]
    if not re.fullmatch(r"[1-4]\.", word["text"]):
        return False

    same_line = sorted(
        [
            item
            for item in words
            if item["page"] == word["page"] and abs(item["top"] - word["top"]) < 1
        ],
        key=lambda item: item["x0"],
    )
    position = next(
        position
        for position, item in enumerate(same_line)
        if item["x0"] == word["x0"] and item["text"] == word["text"]
    )
    previous = same_line[position - 1] if position else None
    # A large horizontal gap marks the beginning of a second answer column.
    # A small gap is an in-sentence number such as "Điều 1.".
    return previous is None or word["x0"] - previous["x1"] >= 35


def is_underlined(option_words: list[dict[str, Any]], underlines: list[dict[str, Any]]) -> bool:
    for underline in underlines:
        for word in option_words:
            horizontal_overlap = min(word["x1"], underline["x1"]) - max(word["x0"], underline["x0"])
            if (
                word["page"] == underline["page"]
                and abs(underline["top"] - word["bottom"]) < 2.5
                and horizontal_overlap > 0
            ):
                return True
    return False


def extract_questions(pdf_path: Path) -> list[dict[str, Any]]:
    all_words: list[dict[str, Any]] = []
    underlines: list[dict[str, Any]] = []
    with pdfplumber.open(pdf_path) as pdf:
        for page_number, page in enumerate(pdf.pages):
            all_words.extend({**word, "page": page_number} for word in page.extract_words())
            underlines.extend(
                {**rect, "page": page_number}
                for rect in page.rects
                if rect.get("height", 0) < 2 and rect.get("width", 0) > 5
            )

    starts: list[tuple[int, int]] = []
    for index, word in enumerate(all_words[:-1]):
        if word["text"] != "Câu":
            continue
        match = re.match(r"^(\d+)[.:]", str(all_words[index + 1]["text"]))
        if match:
            starts.append((int(match.group(1)), index))

    if len(starts) != 600 or {number for number, _ in starts} != set(range(1, 601)):
        raise ValueError("The PDF must contain exactly one question numbered 1 through 600")

    extracted: list[dict[str, Any]] = []
    for start_index, (number, word_index) in enumerate(starts):
        end_index = starts[start_index + 1][1] if start_index + 1 < len(starts) else len(all_words)
        segment = all_words[word_index:end_index]
        option_indexes = [
            index for index in range(len(segment)) if is_option_start(segment, index)
        ]
        if not 2 <= len(option_indexes) <= 4:
            raise ValueError(f"Question {number} has {len(option_indexes)} options")

        question_words = [word for word in segment[: option_indexes[0]] if not is_page_number(word)]
        if question_words and question_words[0]["text"] == "Câu":
            question_words = question_words[1:]
        if question_words:
            question_words[0] = {
                **question_words[0],
                "text": re.sub(r"^\d+[.:]", "", question_words[0]["text"]),
            }
        question_text = compact_text(question_words)

        options: list[dict[str, str]] = []
        underlined_options: list[str] = []
        for option_position, option_index in enumerate(option_indexes):
            end_option = option_indexes[option_position + 1] if option_position + 1 < len(option_indexes) else len(segment)
            words = [word for word in segment[option_index:end_option] if not is_page_number(word)]
            if not words:
                raise ValueError(f"Question {number} has an empty option at index {option_index}")
            label = words[0]["text"][0]
            option_text = compact_text(words)[2:].strip()
            option_id = chr(ord("a") + int(label) - 1)
            options.append({"id": option_id, "text": option_text})
            if is_underlined(words, underlines):
                underlined_options.append(option_id)

        options.sort(key=lambda option: option["id"])

        if len(underlined_options) != 1:
            raise ValueError(f"Question {number} has {len(underlined_options)} underlined answers")

        extracted.append(
            {
                "id": str(uuid.uuid5(QUESTION_NAMESPACE, f"csgt-2025-question-{number}")),
                "slug": f"csgt-2025-q-{number:03d}",
                "licenseClassSlugs": ["b", "c1"],
                "topic": topic_for(number),
                "text": question_text,
                "options": options,
                "correctOptionId": underlined_options[0],
                "isCritical": number in CRITICAL_QUESTIONS,
                "explanation": "",
                "memoryTip": None,
                "source": {
                    "title": "Bộ Công an — Cục Cảnh sát giao thông, Bộ 600 câu hỏi năm 2025",
                    "url": OFFICIAL_URL,
                    "retrievedFrom": RETRIEVED_FROM,
                    "retrievedAt": "2026-09-04",
                    "effectiveFrom": "2025-06-01",
                    "sha256": None,
                    "sourcePage": segment[0]["page"] + 1,
                },
            }
        )

    if sum(question["isCritical"] for question in extracted) != 60:
        raise ValueError("The extracted critical-question set must contain 60 questions")
    return extracted


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    digest = hashlib.sha256(args.input.read_bytes()).hexdigest()
    questions = extract_questions(args.input)
    source = {
        "title": "Bộ Công an — Cục Cảnh sát giao thông, Bộ 600 câu hỏi dùng cho sát hạch lái xe cơ giới đường bộ (2025)",
        "url": OFFICIAL_URL,
        "retrievedFrom": RETRIEVED_FROM,
        "retrievedAt": "2026-09-04",
        "effectiveFrom": "2025-06-01",
        "sha256": digest,
    }
    for question in questions:
        question["source"]["sha256"] = digest

    bundle = {
        "schemaVersion": "1.0",
        "version": "csgt-2025-question-bank-1",
        "status": "published",
        "effectiveFrom": "2025-06-01",
        "licenseClassSlugs": ["b", "c1"],
        "questions": questions,
        "source": source,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(bundle, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Extracted {len(questions)} questions; sha256={digest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
