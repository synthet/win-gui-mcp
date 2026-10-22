#!/usr/bin/env python3
"""Consolidate session logs into a proposed memory file (does not overwrite memory.md)."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[2]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.agent_memory.consolidate import run_dream  # noqa: E402
from scripts.agent_memory.paths import find_repo_root  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate dream memory proposal")
    parser.add_argument("--repo-root", type=Path, default=None)
    parser.add_argument("--max-sessions", type=int, default=None)
    parser.add_argument(
        "--staleness-days",
        type=int,
        default=None,
        help="Flag items not updated in this many days as stale (default: from config)",
    )
    args = parser.parse_args()

    repo = args.repo_root or find_repo_root()
    try:
        dream_path, changelog_path = run_dream(
            repo,
            max_sessions=args.max_sessions,
            staleness_days=args.staleness_days,
        )
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    print(dream_path)
    print(changelog_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
