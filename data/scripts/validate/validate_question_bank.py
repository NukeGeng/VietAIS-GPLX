#!/usr/bin/env python3
"""Validate normalized GPLX data bundles without third-party dependencies."""

from __future__ import annotations

import json
import sys
from pathlib import Path


def load(path: Path) -> dict:
    try:
        with path.open(encoding="utf-8") as file:
            return json.load(file)
    except (OSError, json.JSONDecodeError) as error:
        raise ValueError(f"Cannot read {path}: {error}") from error


def validate(root: Path) -> list[str]:
    errors: list[str] = []
    bank = load(root / "question-banks/v1.json")
    classes = load(root / "license-classes.json")
    blueprint = load(root / "exam-blueprints/v1.json")
    regulation = load(root / "regulations/v1.json")

    class_slugs = {item.get("slug") for item in classes.get("licenseClasses", [])}
    questions = bank.get("questions", [])
    question_ids: set[str] = set()
    for index, question in enumerate(questions, start=1):
        prefix = f"question[{index}]"
        question_id = question.get("id")
        if not question_id or question_id in question_ids:
            errors.append(f"{prefix}: id must be present and unique")
        question_ids.add(question_id)
        if not question.get("text") or not question.get("topic"):
            errors.append(f"{prefix}: text and topic are required")
        if not question.get("explanation") or not question.get("memoryTip"):
            errors.append(f"{prefix}: explanation and memoryTip are required")
        if not set(question.get("licenseClassSlugs", [])) & class_slugs:
            errors.append(f"{prefix}: at least one known license class is required")
        options = question.get("options", [])
        option_ids = {option.get("id") for option in options}
        if len(options) < 2 or len(option_ids) != len(options) or any(not option.get("text") for option in options):
            errors.append(f"{prefix}: options must contain at least two unique ids")
        if question.get("correctOptionId") not in option_ids:
            errors.append(f"{prefix}: correctOptionId must reference an option")
        source = question.get("source", {})
        if not source.get("title") or not source.get("url") or not source.get("retrievedAt"):
            errors.append(f"{prefix}: source title, url and retrievedAt are required")

    for item in blueprint.get("blueprints", []):
        slug = item.get("licenseClassSlug")
        if slug not in class_slugs:
            errors.append(f"blueprint: unknown license class {slug}")
        if item.get("questionCount", 0) <= 0 or item.get("durationSeconds", 0) <= 0:
            errors.append(f"blueprint[{slug}]: questionCount and durationSeconds must be positive")
        if item.get("passingScore", 0) > item.get("questionCount", 0):
            errors.append(f"blueprint[{slug}]: passingScore cannot exceed questionCount")
        topic_counts = item.get("topicQuestionCounts", {})
        if topic_counts and sum(topic_counts.values()) + item.get("criticalQuestionCount", 0) != item.get("questionCount", 0):
            errors.append(f"blueprint[{slug}]: topicQuestionCounts plus criticalQuestionCount must sum to questionCount")
        if item.get("criticalQuestionCount", 0) > item.get("questionCount", 0):
            errors.append(f"blueprint[{slug}]: criticalQuestionCount cannot exceed questionCount")

    for name, document in (("bank", bank), ("classes", classes), ("blueprint", blueprint), ("regulation", regulation)):
        if not document.get("schemaVersion"):
            errors.append(f"{name}: schemaVersion is required")

    if bank.get("version") == "csgt-2025-question-bank-1":
        if len(questions) != 600:
            errors.append("official CSGT 2025 bank must contain 600 questions")
        if sum(bool(question.get("isCritical")) for question in questions) != 60:
            errors.append("official CSGT 2025 bank must contain 60 critical questions")
        if not bank.get("source", {}).get("sha256"):
            errors.append("official CSGT 2025 bank source sha256 is required")

    return errors


def main() -> int:
    root = Path(__file__).resolve().parents[2] / "normalized"
    errors = validate(root)
    if errors:
        print("Validation failed:")
        print("\n".join(f"- {error}" for error in errors))
        return 1
    print(f"Validated normalized GPLX data under {root}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
