#!/usr/bin/env python3
"""Compiled harness for task-env-package-tools.

Emits framework verify gate catalog / profiles. Dry-run by default; --run
executes only selected gates (no Docker prune / installs).
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.io_util import emit, find_repo_root  # noqa: E402
from scripts.skill_harness.verify_catalog import (  # noqa: E402
    framework_verify_commands,
    list_profiles,
    resolve_gates,
)


def _run(cmd: str, cwd: Path, timeout: int = 600) -> dict:
    try:
        proc = subprocess.run(
            cmd,
            shell=True,
            cwd=cwd,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
        return {
            "command": cmd,
            "exit_code": proc.returncode,
            "stdout_tail": (proc.stdout or "")[-4000:],
            "stderr_tail": (proc.stderr or "")[-2000:],
            "ok": proc.returncode == 0,
        }
    except subprocess.TimeoutExpired:
        return {
            "command": cmd,
            "exit_code": -1,
            "stdout_tail": "",
            "stderr_tail": "timeout",
            "ok": False,
        }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=None)
    parser.add_argument(
        "--list",
        action="store_true",
        help="List all gates and profiles (default when no profile/gates)",
    )
    parser.add_argument(
        "--profile",
        help="framework|agent-assets|tests|lint",
    )
    parser.add_argument(
        "--gates",
        help="Comma-separated gate ids (sync_check,frontmatter,cli_skills,okf_lint,pytest)",
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Execute selected gate commands (default: print only)",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    gate_ids = [g.strip() for g in (args.gates or "").split(",") if g.strip()]

    result: dict = {
        "repo": str(repo),
        "catalog": framework_verify_commands(),
        "profiles": list_profiles(),
        "selected": [],
        "runs": [],
        "ran": False,
        "notes": [
            "LLM slot: choose --profile / --gates for the change type.",
            "LLM slot: interpret failures; narrow pytest to failing file first.",
            "Human: Docker down/prune and broad installs need confirmation.",
            "Default is dry-run (print commands only).",
        ],
    }

    list_only = args.list or (not args.profile and not gate_ids)
    if list_only and not args.run:
        result["notes"].append(
            "Pass --profile agent-assets (or --gates ...) then optionally --run."
        )
        emit(result, as_json=args.json)
        return 0

    try:
        selected = resolve_gates(profile=args.profile, gate_ids=gate_ids or None)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    result["selected"] = selected
    result["profile"] = args.profile

    if args.run:
        result["ran"] = True
        for entry in selected:
            result["runs"].append(_run(entry["command"], repo))
        if not all(r["ok"] for r in result["runs"]):
            emit(result, as_json=args.json)
            return 1
    else:
        result["notes"].append("Commands not executed; pass --run to execute selected gates.")

    emit(result, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
