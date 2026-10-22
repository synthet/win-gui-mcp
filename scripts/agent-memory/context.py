#!/usr/bin/env python3
"""Print compact project memory for session start."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[2]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

# Windows consoles default to cp1252 and choke on non-ASCII memory content.
try:  # pragma: no cover - platform-dependent
    sys.stdout.reconfigure(encoding="utf-8")
except (AttributeError, ValueError):
    pass

from scripts.agent_memory.consolidate import load_context  # noqa: E402
from scripts.agent_memory.paths import find_repo_root, load_config  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Print project memory context")
    parser.add_argument("--repo-root", type=Path, default=None)
    args = parser.parse_args()

    repo = args.repo_root or find_repo_root()
    config = load_config(repo)
    content = load_context(repo)
    if not content:
        print("# Project Memory\n\n(no memory.md yet)", end="")
        return 0

    max_chars = config.get("max_context_chars", 12000)
    if len(content) > max_chars:
        content = content[: max_chars - 40] + "\n\n... (truncated)\n"
    print(content, end="" if content.endswith("\n") else "\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
