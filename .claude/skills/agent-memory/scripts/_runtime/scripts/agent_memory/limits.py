"""Retention and size limits."""

from __future__ import annotations

import time
from datetime import datetime, timezone
from pathlib import Path


def prune_old_sessions(raw_dir: Path, retention_days: int) -> list[str]:
    """Delete session files older than retention_days. Returns deleted names."""
    if retention_days <= 0:
        return []
    cutoff = time.time() - retention_days * 86400
    removed: list[str] = []
    for path in sorted(raw_dir.glob("*.yaml")):
        if path.stat().st_mtime < cutoff:
            removed.append(path.name)
            path.unlink()
    return removed


def session_timestamp_slug() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H%M%SZ")


def dream_timestamp_slug() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d-%H%M")
