#!/usr/bin/env python3
"""Compiled harness for verification-before-completion.

Claim→proof catalog and optional command runner. Interpreting whether output
supports a claim is an LLM judgment slot.
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
    CLAIM_PROOF_CATALOG,
    FRAMEWORK_VERIFY_COMMANDS,
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
        out = (proc.stdout or "")[-4000:]
        err = (proc.stderr or "")[-2000:]
        return {
            "command": cmd,
            "exit_code": proc.returncode,
            "stdout_tail": out,
            "stderr_tail": err,
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
        "--claim",
        action="append",
        default=[],
        help="Claim id from catalog (repeatable). Default: list catalog.",
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Execute proof commands for selected claims (or framework suite with --framework)",
    )
    parser.add_argument(
        "--framework",
        action="store_true",
        help="Use full framework self-verify command list",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    result: dict = {
        "repo": str(repo),
        "catalog": CLAIM_PROOF_CATALOG,
        "selected": [],
        "runs": [],
        "report": "",
        "notes": [
            "LLM slot: name the claim, read exit code/output, report only supported claims.",
            "Use ⚠️ when verification is incomplete; never claim pass from stale runs.",
        ],
    }

    if args.framework:
        selected = [
            {"id": cid, "claim": cid, "proof": cmd, "not_enough": "skipped subset"}
            for cid, cmd in FRAMEWORK_VERIFY_COMMANDS
        ]
    elif args.claim:
        selected = []
        for cid in args.claim:
            if cid not in CLAIM_PROOF_CATALOG:
                print(f"unknown claim id {cid!r}", file=sys.stderr)
                print(f"known: {', '.join(sorted(CLAIM_PROOF_CATALOG))}", file=sys.stderr)
                return 1
            entry = dict(CLAIM_PROOF_CATALOG[cid])
            entry["id"] = cid
            selected.append(entry)
    else:
        result["notes"].append("Pass --claim <id> and/or --framework; use --run to execute proofs.")
        emit(result, as_json=args.json)
        return 0

    result["selected"] = selected

    if args.run:
        for entry in selected:
            result["runs"].append(_run(entry["proof"], repo))

    lines = ["## Verification"]
    if result["runs"]:
        for entry, run in zip(selected, result["runs"]):
            mark = "✅" if run["ok"] else "❌"
            lines.append(f"- {mark} `{run['command']}` — {entry.get('claim', entry['id'])} (exit {run['exit_code']})")
            if not run["ok"]:
                lines.append(f"  - next: inspect stderr_tail / fix before claiming success")
    else:
        for entry in selected:
            lines.append(
                f"- ⬜ `{entry['proof']}` — proof for: {entry.get('claim', entry['id'])} (not run; pass --run)"
            )
    lines.append("")
    result["report"] = "\n".join(lines)

    if args.json:
        emit(result, as_json=True)
    else:
        sys.stdout.write(result["report"])
        if not args.run:
            print("\n# Catalog")
            for cid, entry in CLAIM_PROOF_CATALOG.items():
                print(f"- {cid}: {entry['claim']} → `{entry['proof']}`")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
