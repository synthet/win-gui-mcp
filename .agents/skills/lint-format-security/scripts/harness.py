#!/usr/bin/env python3
"""Compiled harness for lint-format-security.

Pure decision table. Mapping ask/paths → stack is an LLM judgment slot.
Recommend check-mode commands only; never auto-fix.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.io_util import emit  # noqa: E402
from scripts.skill_harness.lint_router import list_stacks, recommend  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--stack",
        help="python|js_ts|rust|shell|container|secrets",
    )
    parser.add_argument(
        "--paths",
        default="path/to/module",
        help="Path glob/token substituted into check commands",
    )
    parser.add_argument("--list", action="store_true", help="List known stacks")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    if args.list or not args.stack:
        payload = {
            "stacks": list_stacks(),
            "notes": [
                "LLM slot: map changed files / user ask to --stack, then re-run.",
                "Harness recommends check-mode only; never auto-fix.",
                "Human: --fix / --write on broad trees needs explicit approval.",
            ],
        }
        emit(payload, as_json=args.json)
        return 0

    try:
        rec = recommend(args.stack, paths=args.paths)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    payload = {
        **rec.to_dict(),
        "paths": args.paths,
        "notes": [
            "Run check commands on the narrowest scope first.",
            "Do not pass --fix/--write unless the user approved auto-fix.",
            "See commands-requiring-confirmation for write modes.",
        ],
    }
    emit(payload, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
