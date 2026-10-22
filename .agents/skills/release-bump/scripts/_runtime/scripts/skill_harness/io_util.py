"""I/O helpers for harness CLIs."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any


def find_repo_root(start: Path | None = None) -> Path:
    """Walk up from *start* (or cwd) until ``AGENTS.md`` or ``.git`` is found."""
    cur = (start or Path.cwd()).resolve()
    for candidate in (cur, *cur.parents):
        if (candidate / "AGENTS.md").is_file() or (candidate / ".git").exists():
            return candidate
    return cur


def emit(data: Any, *, as_json: bool) -> None:
    """Print *data* as JSON or a human-readable summary."""
    if as_json:
        json.dump(data, sys.stdout, indent=2, sort_keys=True)
        sys.stdout.write("\n")
        return
    if isinstance(data, dict):
        for key, value in data.items():
            if isinstance(value, (dict, list)):
                print(f"{key}:")
                print(json.dumps(value, indent=2, sort_keys=True))
            else:
                print(f"{key}: {value}")
        return
    print(data)
