#!/usr/bin/env python3
"""Compiled harness for search-tool-selection.

Pure decision table. Classifying a vague ask into a task type is an LLM slot.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.io_util import emit  # noqa: E402
from scripts.skill_harness.search_router import list_task_types, recommend  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--task-type",
        help="filename|content|syntax|security|symbols|layout|config|repeated|cursor",
    )
    parser.add_argument("--list", action="store_true", help="List known task types")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    if args.list or not args.task_type:
        payload = {
            "task_types": list_task_types(),
            "notes": [
                "LLM slot: map the user ask to a task type, then re-run with --task-type.",
                "Default escalation: fd → rg → ast-grep → bat/Read.",
            ],
        }
        emit(payload, as_json=args.json)
        return 0

    try:
        rec = recommend(args.task_type)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    payload = {
        **rec.to_dict(),
        "task_type": args.task_type,
        "notes": [
            "Stop escalating when the task is satisfied.",
            "Exclude node_modules, dist, build, .git.",
            "fzf is humans-only.",
        ],
    }
    emit(payload, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
