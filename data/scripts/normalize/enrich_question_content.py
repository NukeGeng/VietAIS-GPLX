#!/usr/bin/env python3
"""Add safe editorial learning aids to a normalized question bank.

The official publication supplies the questions and answer key, not editorial
explanations. This step keeps those concerns separate: it preserves any
manually authored content and supplies a conservative baseline for questions
that do not have one yet.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


TOPIC_TIPS = {
    "Quy định chung và quy tắc giao thông đường bộ":
        "Đọc kỹ từ khóa về quyền ưu tiên, phần đường và hành vi bị cấm trước khi chọn.",
    "Văn hóa giao thông, đạo đức người lái xe, kỹ năng phòng cháy, chữa cháy và cứu hộ, cứu nạn":
        "Ưu tiên phương án tuân thủ pháp luật, chủ động phòng ngừa và bảo vệ người tham gia giao thông.",
    "Kỹ thuật lái xe":
        "Ưu tiên thao tác ổn định, đúng kỹ thuật và an toàn thay vì xử lý gấp.",
    "Cấu tạo và sửa chữa":
        "Đối chiếu bộ phận với chức năng và dấu hiệu hỏng trước khi chọn đáp án.",
    "Báo hiệu đường bộ":
        "Nhớ hình dạng, màu nền và biểu tượng chính của biển báo rồi đối chiếu với tình huống.",
    "Giải thế sa hình và kỹ năng xử lý tình huống giao thông":
        "Xác định hướng đi, biển báo và thứ tự ưu tiên trước khi suy luận xe được đi.",
}


def enrich(question: dict) -> None:
    options = {option["id"]: option["text"] for option in question.get("options", [])}
    correct_text = options.get(question.get("correctOptionId"), "đáp án được đánh dấu trong nguồn chính thức")
    if not str(question.get("explanation") or "").strip():
        question["explanation"] = (
            f'Đáp án đúng theo bộ câu hỏi chính thức là “{correct_text}”. '
            "Hãy đối chiếu các từ khóa trong câu hỏi với nội dung đáp án khi ôn tập."
        )
    if not str(question.get("memoryTip") or "").strip():
        if question.get("isCritical"):
            question["memoryTip"] = (
                "Câu điểm liệt: luôn ưu tiên phương án an toàn và không chấp nhận hành vi nguy hiểm."
            )
        else:
            question["memoryTip"] = TOPIC_TIPS.get(
                question.get("topic", ""),
                "Đọc kỹ câu hỏi, loại trừ đáp án trái với nguyên tắc an toàn giao thông.",
            )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    document = json.loads(args.input.read_text(encoding="utf-8"))
    for question in document.get("questions", []):
        enrich(question)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(document, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Enriched {len(document.get('questions', []))} questions")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
