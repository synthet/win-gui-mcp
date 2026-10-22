"""Eval signal schema validation and log_session command construction."""

from __future__ import annotations

from typing import Any

TEST_PASS_RATE = frozenset({"yes", "partial", "no"})
FIRST_TRY_SUCCESS = frozenset({"yes", "no"})
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
CONFIDENCE = frozenset({"low", "medium", "high"})

OUTCOME_CATEGORY_HINTS: list[dict[str, str]] = [
    {
        "outcome": "First-try success, all tests green",
        "category": "successful_pattern",
        "confidence": "high",
    },
    {
        "outcome": "Required 2–3 iterations",
        "category": "recurring_issue",
        "confidence": "medium",
    },
    {
        "outcome": "Required >3 iterations",
        "category": "recurring_issue",
        "confidence": "high",
    },
    {
        "outcome": "Tests were missing (written after code)",
        "category": "working_rule",
        "confidence": "high",
    },
    {
        "outcome": "Regression caught in review",
        "category": "working_rule",
        "confidence": "high",
    },
    {
        "outcome": "Regression shipped (caught later)",
        "category": "recurring_issue",
        "confidence": "high",
    },
]


def schema_skeleton() -> dict[str, Any]:
    return {
        "signals": {
            "test_pass_rate": sorted(TEST_PASS_RATE),
            "first_try_success": sorted(FIRST_TRY_SUCCESS),
            "iteration_count": "non-negative integer",
        },
        "candidate_format": "text|category|confidence",
        "categories": sorted(CATEGORIES),
        "confidence": sorted(CONFIDENCE),
        "outcome_category_hints": OUTCOME_CATEGORY_HINTS,
        "log_script": "python scripts/agent-memory/log_session.py",
        "notes": [
            "LLM slot: score the three signals from the completed task.",
            "LLM slot: phrase durable memory candidate text (no secrets).",
            "Use --emit-log-cmd after filling signals/candidates.",
        ],
    }


def parse_candidate(raw: str) -> tuple[str, str, str]:
    parts = raw.split("|")
    if len(parts) != 3:
        raise ValueError(
            f"candidate must be text|category|confidence, got {raw!r}"
        )
    text, category, confidence = (p.strip() for p in parts)
    if not text:
        raise ValueError("candidate text must be non-empty")
    if category not in CATEGORIES:
        raise ValueError(
            f"unknown category {category!r}; known: {', '.join(sorted(CATEGORIES))}"
        )
    if confidence not in CONFIDENCE:
        raise ValueError(
            f"unknown confidence {confidence!r}; known: {', '.join(sorted(CONFIDENCE))}"
        )
    return text, category, confidence


def validate_signals(
    *,
    test_pass_rate: str,
    first_try_success: str,
    iteration_count: int,
) -> dict[str, Any]:
    if test_pass_rate not in TEST_PASS_RATE:
        raise ValueError(
            f"test_pass_rate must be one of {sorted(TEST_PASS_RATE)}, got {test_pass_rate!r}"
        )
    if first_try_success not in FIRST_TRY_SUCCESS:
        raise ValueError(
            f"first_try_success must be one of {sorted(FIRST_TRY_SUCCESS)}, "
            f"got {first_try_success!r}"
        )
    if iteration_count < 0:
        raise ValueError("iteration_count must be >= 0")
    if first_try_success == "yes" and iteration_count > 1:
        raise ValueError("first_try_success=yes requires iteration_count <= 1")
    return {
        "test_pass_rate": test_pass_rate,
        "first_try_success": first_try_success,
        "iteration_count": iteration_count,
    }


def map_test_results(test_pass_rate: str) -> str:
    return {"yes": "pass", "partial": "partial", "no": "fail"}[test_pass_rate]


def map_outcome(first_try_success: str, iteration_count: int) -> str:
    if first_try_success == "yes":
        return "first_try_success"
    if iteration_count <= 3:
        return "partial"
    return "multi-iteration"


def build_log_command(
    *,
    summary: str,
    test_pass_rate: str,
    first_try_success: str,
    iteration_count: int,
    candidates: list[str],
) -> dict[str, Any]:
    if not summary.strip():
        raise ValueError("summary must be non-empty")
    if not candidates:
        raise ValueError("at least one --candidate is required")

    signals = validate_signals(
        test_pass_rate=test_pass_rate,
        first_try_success=first_try_success,
        iteration_count=iteration_count,
    )
    parsed = [parse_candidate(c) for c in candidates]
    outcome = map_outcome(first_try_success, iteration_count)
    test_results = map_test_results(test_pass_rate)

    argv = [
        "python",
        "scripts/agent-memory/log_session.py",
        "--summary",
        summary,
        "--outcome",
        outcome,
        "--test-results",
        test_results,
    ]
    for text, category, confidence in parsed:
        argv.extend(["--candidate", f"{text}|{category}|{confidence}"])

    # Shell-friendly single line (Windows-safe quoting via simple double quotes)
    def q(s: str) -> str:
        if any(ch.isspace() for ch in s) or "|" in s:
            return '"' + s.replace('"', '\\"') + '"'
        return s

    command = " ".join(q(a) for a in argv)
    return {
        "signals": signals,
        "outcome": outcome,
        "test_results": test_results,
        "candidates": [
            {"text": t, "category": c, "confidence": conf} for t, c, conf in parsed
        ],
        "argv": argv,
        "command": command,
    }
