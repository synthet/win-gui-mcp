#!/usr/bin/env python3
"""Log a structured agent session to .agent-memory/raw-sessions/."""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[2]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.agent_memory import yaml_compat as yaml  # noqa: E402

from scripts.agent_memory.limits import prune_old_sessions, session_timestamp_slug  # noqa: E402
from scripts.agent_memory.paths import ensure_dirs, find_repo_root, load_config  # noqa: E402
from scripts.agent_memory.schema import normalize_session, parse_candidate_flag, validate_session  # noqa: E402
from scripts.agent_memory.secrets import assert_no_secrets  # noqa: E402


def _build_from_args(args: argparse.Namespace) -> dict:
    data: dict = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "task_summary": args.summary or "",
        "final_outcome": args.outcome or "",
        "files_touched": list(args.file or []),
        "commands_run": list(args.command or []),
        "tests_run": list(args.test or []),
        "test_results": args.test_results or "",
        "key_decisions": list(args.decision or []),
        "errors_or_blockers": list(args.error or []),
        "memory_candidates": [],
    }
    for cand_str in args.candidate or []:
        data["memory_candidates"].append(parse_candidate_flag(cand_str))
    return data


def _interactive_session() -> dict:
    print("Interactive session log (empty line to finish lists).")
    summary = input("Task summary: ").strip()
    outcome = input("Final outcome: ").strip()
    data = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "task_summary": summary,
        "final_outcome": outcome,
        "files_touched": [],
        "commands_run": [],
        "tests_run": [],
        "test_results": "",
        "key_decisions": [],
        "errors_or_blockers": [],
        "memory_candidates": [],
    }
    return normalize_session(data)


def main() -> int:
    parser = argparse.ArgumentParser(description="Log an agent session")
    parser.add_argument("--repo-root", type=Path, default=None)
    parser.add_argument("--summary", default="")
    parser.add_argument("--outcome", default="")
    parser.add_argument("--file", action="append", default=[])
    parser.add_argument("--command", action="append", default=[])
    parser.add_argument("--test", action="append", default=[])
    parser.add_argument("--test-results", default="")
    parser.add_argument("--decision", action="append", default=[])
    parser.add_argument("--error", action="append", default=[])
    parser.add_argument(
        "--candidate",
        action="append",
        default=[],
        help="text|category|confidence",
    )
    parser.add_argument("--stdin", action="store_true", help="Read JSON session from stdin")
    parser.add_argument("--interactive", action="store_true")
    args = parser.parse_args()

    repo = args.repo_root or find_repo_root()
    config = load_config(repo)
    dirs = ensure_dirs(repo)

    if args.stdin:
        raw = sys.stdin.read()
        data = json.loads(raw)
    elif args.interactive or not args.summary:
        data = _interactive_session()
    else:
        data = _build_from_args(args)

    data = normalize_session(data)
    if not data.get("timestamp"):
        data["timestamp"] = datetime.now(timezone.utc).isoformat()

    errors = validate_session(data)
    if errors:
        print("Validation errors:", file=sys.stderr)
        for e in errors:
            print(f"  - {e}", file=sys.stderr)
        return 1

    yaml_text = yaml.safe_dump(data, sort_keys=False, allow_unicode=True)
    assert_no_secrets(yaml_text, context="session log")

    slug = session_timestamp_slug()
    out_path = dirs["raw_sessions"] / f"{slug}.yaml"
    while out_path.exists():
        slug = f"{slug}_1"
        out_path = dirs["raw_sessions"] / f"{slug}.yaml"
    out_path.write_text(yaml_text, encoding="utf-8")

    removed = prune_old_sessions(
        dirs["raw_sessions"], config.get("raw_session_retention_days", 90)
    )
    print(out_path)
    if removed:
        print(f"Pruned {len(removed)} old session(s)", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
