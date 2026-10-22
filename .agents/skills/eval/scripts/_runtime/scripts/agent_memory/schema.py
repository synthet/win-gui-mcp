"""Session and memory category validation."""

from __future__ import annotations

from typing import Any

CATEGORIES = frozenset(
    {
        "stable_fact",
        "user_preference",
        "working_rule",
        "recurring_issue",
        "successful_pattern",
        "open_question",
        "deprecated",
    }
)

CONFIDENCE_LEVELS = frozenset({"low", "medium", "high"})
CONFIDENCE_RANK = {"low": 1, "medium": 2, "high": 3}
VERIFICATION_STATUSES = frozenset({"unverified", "verified", "failed", "needs_review"})

SECTION_ORDER = [
    "Stable Project Facts",
    "User Preferences",
    "Working Rules",
    "Recurring Issues",
    "Successful Patterns",
    "Open Questions",
    "Deprecated / Superseded",
]

CATEGORY_TO_SECTION = {
    "stable_fact": "Stable Project Facts",
    "user_preference": "User Preferences",
    "working_rule": "Working Rules",
    "recurring_issue": "Recurring Issues",
    "successful_pattern": "Successful Patterns",
    "open_question": "Open Questions",
    "deprecated": "Deprecated / Superseded",
}

SECTION_TO_CATEGORY = {v: k for k, v in CATEGORY_TO_SECTION.items()}

PLACEHOLDER_NONE = "(none yet)"


def validate_session(data: dict[str, Any]) -> list[str]:
    """Return list of validation errors (empty if ok)."""
    errors: list[str] = []
    if not data.get("task_summary"):
        errors.append("task_summary is required")
    if not data.get("timestamp"):
        errors.append("timestamp is required")
    for i, cand in enumerate(data.get("memory_candidates") or []):
        if not isinstance(cand, dict):
            errors.append(f"memory_candidates[{i}] must be an object")
            continue
        if not cand.get("text"):
            errors.append(f"memory_candidates[{i}].text is required")
        cat = cand.get("category")
        if cat not in CATEGORIES:
            errors.append(f"memory_candidates[{i}].category invalid: {cat!r}")
        conf = cand.get("confidence")
        if conf not in CONFIDENCE_LEVELS:
            errors.append(f"memory_candidates[{i}].confidence invalid: {conf!r}")
        status = cand.get("verification_status")
        if status is not None and status not in VERIFICATION_STATUSES:
            errors.append(f"memory_candidates[{i}].verification_status invalid: {status!r}")
        for date_key in ("verified_at", "stale_after"):
            value = cand.get(date_key)
            if value is not None and not _is_iso_date(str(value)):
                errors.append(f"memory_candidates[{i}].{date_key} must be YYYY-MM-DD")
        for list_key in (
            "related_paths",
            "related_tasks",
            "related_issues",
            "related_prs",
        ):
            value = cand.get(list_key)
            if value is not None and not isinstance(value, list):
                errors.append(f"memory_candidates[{i}].{list_key} must be a list")
    return errors


def normalize_session(data: dict[str, Any]) -> dict[str, Any]:
    """Fill defaults for optional list fields."""
    out = dict(data)
    for key in (
        "files_touched",
        "commands_run",
        "tests_run",
        "key_decisions",
        "errors_or_blockers",
        "memory_candidates",
    ):
        if out.get(key) is None:
            out[key] = []
    return out


def parse_candidate_flag(value: str) -> dict[str, str]:
    """Parse 'text|category|confidence' from CLI."""
    parts = value.split("|")
    if len(parts) != 3:
        raise ValueError(
            f"Candidate must be text|category|confidence, got {value!r}"
        )
    text, category, confidence = (p.strip() for p in parts)
    cand = {"text": text, "category": category, "confidence": confidence}
    errs = validate_session({"task_summary": "x", "timestamp": "x", "memory_candidates": [cand]})
    if errs:
        raise ValueError("; ".join(errs))
    return cand


def confidence_rank(value: str) -> int:
    return CONFIDENCE_RANK.get(value, 0)


def _is_iso_date(value: str) -> bool:
    import re

    return re.fullmatch(r"\d{4}-\d{2}-\d{2}", value) is not None
