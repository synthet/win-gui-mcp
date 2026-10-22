"""Resolve repository and .agent-memory paths."""

from __future__ import annotations

import json
from pathlib import Path

MEMORY_DIR_NAME = ".agent-memory"
CONFIG_FILENAME = "config.json"
MEMORY_FILENAME = "memory.md"
RAW_SESSIONS_DIR = "raw-sessions"
DREAMS_DIR = "dreams"
ARCHIVE_DIR = "archive"


def find_repo_root(start: Path | None = None) -> Path:
    """Walk parents until AGENTS.md or .agent-memory exists."""
    current = (start or Path.cwd()).resolve()
    for candidate in [current, *current.parents]:
        if (candidate / "AGENTS.md").is_file() or (candidate / MEMORY_DIR_NAME).is_dir():
            return candidate
    raise FileNotFoundError(
        f"Could not find repo root from {current} (expected AGENTS.md or {MEMORY_DIR_NAME}/)"
    )


def memory_root(repo_root: Path | None = None) -> Path:
    root = repo_root or find_repo_root()
    return root / MEMORY_DIR_NAME


def load_config(repo_root: Path | None = None) -> dict:
    path = memory_root(repo_root) / CONFIG_FILENAME
    defaults = {
        "max_sessions": 20,
        "max_session_bytes": 65536,
        "max_items_per_section": 40,
        "max_context_chars": 12000,
        "raw_session_retention_days": 90,
        "dream_filename_format": "YYYY-MM-DD-HHMM",
    }
    if not path.is_file():
        return defaults
    with path.open(encoding="utf-8") as fh:
        data = json.load(fh)
    return {**defaults, **data}


def ensure_dirs(repo_root: Path | None = None) -> dict[str, Path]:
    base = memory_root(repo_root)
    paths = {
        "base": base,
        "raw_sessions": base / RAW_SESSIONS_DIR,
        "dreams": base / DREAMS_DIR,
        "archive": base / DREAMS_DIR / ARCHIVE_DIR,
        "memory": base / MEMORY_FILENAME,
    }
    for key in ("base", "raw_sessions", "dreams", "archive"):
        paths[key].mkdir(parents=True, exist_ok=True)
    return paths
