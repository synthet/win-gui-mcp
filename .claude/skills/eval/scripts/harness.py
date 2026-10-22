#!/usr/bin/env python3
"""Compiled harness for eval.

Validates quality-signal enums and emits a log_session.py command.
Scoring signals and phrasing candidates remain LLM judgment slots.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.eval_signals import (  # noqa: E402
    build_log_command,
    schema_skeleton,
)
from scripts.skill_harness.io_util import emit  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--emit-log-cmd",
        action="store_true",
        help="Build log_session.py command from signals + candidates",
    )
    parser.add_argument("--summary", default="", help="Session summary for log_session")
    parser.add_argument(
        "--test-pass-rate",
        choices=["yes", "partial", "no"],
        help="Did all tests pass on the first agent attempt?",
    )
    parser.add_argument(
        "--first-try-success",
        choices=["yes", "no"],
        help="Was the implementation accepted without revision?",
    )
    parser.add_argument(
        "--iteration-count",
        type=int,
        default=None,
        help="Agent rounds before done",
    )
    parser.add_argument(
        "--candidate",
        action="append",
        default=[],
        help="text|category|confidence (repeatable)",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    if not args.emit_log_cmd:
        emit(schema_skeleton(), as_json=args.json)
        return 0

    missing = []
    if not args.summary:
        missing.append("--summary")
    if args.test_pass_rate is None:
        missing.append("--test-pass-rate")
    if args.first_try_success is None:
        missing.append("--first-try-success")
    if args.iteration_count is None:
        missing.append("--iteration-count")
    if missing:
        print(f"missing required args: {', '.join(missing)}", file=sys.stderr)
        return 1

    try:
        payload = build_log_command(
            summary=args.summary,
            test_pass_rate=args.test_pass_rate,
            first_try_success=args.first_try_success,
            iteration_count=args.iteration_count,
            candidates=args.candidate,
        )
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    payload["notes"] = [
        "LLM already scored signals / phrased candidates; run the command next.",
        "Do not log secrets, .env, or personal machine paths.",
        "Periodic: after 5–10 tasks run /dream-memory.",
    ]
    emit(payload, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
