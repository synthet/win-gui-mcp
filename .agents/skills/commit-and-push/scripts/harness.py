#!/usr/bin/env python3
"""Compiled harness for commit-and-push.

Inspect status/diff, flag secret paths, optionally run framework verify.
Defaults to dry-run. Commit/push only with --execute plus explicit flags,
and only when the user requested shipping.
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
    is_secret_path,
)


def _git(repo: Path, *args: str) -> str:
    proc = subprocess.run(
        ["git", *args],
        cwd=repo,
        capture_output=True,
        text=True,
        check=False,
    )
    return (proc.stdout or "").strip()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=None)
    parser.add_argument("--release", action="store_true", help="Include framework verify commands")
    parser.add_argument(
        "--run-verify",
        action="store_true",
        help="Actually run framework verify commands (with --release)",
    )
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Allow mutating git (still requires --commit / --push)",
    )
    parser.add_argument("--commit", action="store_true", help="Create commit (needs --execute -m)")
    parser.add_argument("-m", "--message", default=None, help="Commit message")
    parser.add_argument("--push", action="store_true", help="Push HEAD (needs --execute)")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    status = _git(repo, "status", "--short")
    branch = _git(repo, "status", "-sb")
    recent = _git(repo, "log", "-5", "--oneline")
    paths = [line[3:].strip() for line in status.splitlines() if line.strip()]
    secret_hits = [p for p in paths if is_secret_path(p)]

    result: dict = {
        "repo": str(repo),
        "branch_status": branch,
        "status_short": status,
        "recent_commits": recent.splitlines(),
        "paths": paths,
        "secret_paths": secret_hits,
        "verify_commands": framework_verify_commands() if args.release else [],
        "verify_runs": [],
        "committed": False,
        "pushed": False,
        "notes": [
            "LLM slot: draft Conventional Commit subject/body (imperative, why not file list).",
            "Human: do not --execute/--commit/--push unless the user explicitly asked to ship.",
            "Default is dry-run inspect only.",
        ],
    }

    if secret_hits:
        result["notes"].append(
            f"Blocked secret-looking paths: {secret_hits}. Exclude before staging."
        )

    if args.release and args.run_verify:
        for entry in framework_verify_commands():
            proc = subprocess.run(
                entry["command"],
                shell=True,
                cwd=repo,
                capture_output=True,
                text=True,
            )
            result["verify_runs"].append(
                {
                    "id": entry["id"],
                    "command": entry["command"],
                    "exit_code": proc.returncode,
                    "ok": proc.returncode == 0,
                }
            )
        if any(not r["ok"] for r in result["verify_runs"]):
            result["notes"].append("Verify failed; refusing commit/push.")
            emit(result, as_json=args.json)
            return 1

    if args.commit or args.push:
        if not args.execute:
            result["notes"].append("Refusing mutate: pass --execute only after explicit user request.")
            emit(result, as_json=args.json)
            return 1
        if secret_hits:
            emit(result, as_json=args.json)
            return 1

    if args.commit:
        if not args.message:
            result["notes"].append("Commit requires -m/--message.")
            emit(result, as_json=args.json)
            return 1
        add = subprocess.run(["git", "add", "-A"], cwd=repo, capture_output=True, text=True)
        if add.returncode != 0:
            result["notes"].append(add.stderr)
            emit(result, as_json=args.json)
            return 1
        # Re-check staged for secrets
        staged = _git(repo, "diff", "--cached", "--name-only")
        staged_secrets = [p for p in staged.splitlines() if is_secret_path(p)]
        if staged_secrets:
            subprocess.run(["git", "reset", "HEAD"], cwd=repo, capture_output=True)
            result["secret_paths"] = staged_secrets
            result["notes"].append("Reset staging: secret paths present.")
            emit(result, as_json=args.json)
            return 1
        commit = subprocess.run(
            ["git", "commit", "-m", args.message],
            cwd=repo,
            capture_output=True,
            text=True,
        )
        result["committed"] = commit.returncode == 0
        if commit.returncode != 0:
            result["notes"].append(commit.stderr or commit.stdout)
            emit(result, as_json=args.json)
            return 1

    if args.push:
        push = subprocess.run(
            ["git", "push", "-u", "origin", "HEAD"],
            cwd=repo,
            capture_output=True,
            text=True,
        )
        result["pushed"] = push.returncode == 0
        if push.returncode != 0:
            result["notes"].append(push.stderr or push.stdout)
            emit(result, as_json=args.json)
            return 1

    emit(result, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
