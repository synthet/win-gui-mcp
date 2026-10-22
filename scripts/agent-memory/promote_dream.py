#!/usr/bin/env python3
"""Promote a reviewed dream file into .agent-memory/memory.md."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[2]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.agent_memory.consolidate import promote_dream  # noqa: E402
from scripts.agent_memory.paths import find_repo_root  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Promote dream to active memory")
    parser.add_argument("--dream", type=Path, required=True, help="Path to dream .md file")
    parser.add_argument("--repo-root", type=Path, default=None)
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()

    repo = args.repo_root or find_repo_root()
    dream_path = args.dream if args.dream.is_absolute() else repo / args.dream

    if not dream_path.is_file():
        print(f"Dream file not found: {dream_path}", file=sys.stderr)
        return 1

    try:
        memory_path = promote_dream(repo, dream_path, force=args.force)
    except (FileNotFoundError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        return 1

    print(memory_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
