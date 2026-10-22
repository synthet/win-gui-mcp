#!/usr/bin/env python3
"""Compiled harness for validate-implementation.

Parses AC-n criteria, enforces verdict enum, rejects Verified without evidence,
renders the standard validation report. Verdict assignment is an LLM slot.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.acceptance import (  # noqa: E402
    VERDICTS,
    parse_acceptance_criteria,
    render_validation_report,
)
from scripts.skill_harness.io_util import emit, find_repo_root  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--spec", type=Path, required=True, help="Path to spec markdown")
    parser.add_argument("--title", default=None, help="Report title (default: spec stem)")
    parser.add_argument(
        "--verdict",
        action="append",
        default=[],
        metavar="AC-N=Verdict|evidence",
        help="Repeatable. Example: --verdict 'AC-1=Verified|pytest passed'",
    )
    parser.add_argument(
        "--skeleton",
        action="store_true",
        help="Emit report with all Unknown and empty evidence",
    )
    parser.add_argument("--write", type=Path, default=None, help="Write report markdown to path")
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--repo", type=Path, default=None)
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    spec_path = args.spec if args.spec.is_absolute() else repo / args.spec
    if not spec_path.is_file():
        print(f"spec not found: {spec_path}", file=sys.stderr)
        return 1

    criteria = parse_acceptance_criteria(spec_path.read_text(encoding="utf-8"))
    if not criteria:
        print("no AC-n criteria found in spec", file=sys.stderr)
        return 1

    by_id = {c.id: c for c in criteria}
    verdict_map: dict[str, dict[str, str]] = {}

    if args.skeleton and not args.verdict:
        for c in criteria:
            verdict_map[c.id] = {
                "id": c.id,
                "criterion": c.text,
                "verdict": "Unknown",
                "evidence": "",
            }
    else:
        for raw in args.verdict:
            if "=" not in raw:
                print(f"bad --verdict {raw!r}; expected AC-N=Verdict|evidence", file=sys.stderr)
                return 1
            ac_part, rest = raw.split("=", 1)
            ac_id = ac_part.strip().upper()
            if ac_id not in by_id:
                # allow AC-1 vs ac-1
                ac_id = ac_id if ac_id.startswith("AC-") else f"AC-{ac_id}"
            if "|" in rest:
                verdict, evidence = rest.split("|", 1)
            else:
                verdict, evidence = rest, ""
            verdict = verdict.strip()
            evidence = evidence.strip()
            if verdict not in VERDICTS:
                print(f"invalid verdict {verdict!r}", file=sys.stderr)
                return 1
            if ac_id not in by_id:
                print(f"unknown criterion {ac_id}", file=sys.stderr)
                return 1
            verdict_map[ac_id] = {
                "id": ac_id,
                "criterion": by_id[ac_id].text,
                "verdict": verdict,
                "evidence": evidence,
            }

        # Fill missing as Unknown
        for c in criteria:
            if c.id not in verdict_map:
                verdict_map[c.id] = {
                    "id": c.id,
                    "criterion": c.text,
                    "verdict": "Unknown",
                    "evidence": "not checked in this run",
                }

    rows = [verdict_map[c.id] for c in criteria]
    title = args.title or spec_path.stem

    try:
        report = render_validation_report(title, rows)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 1

    if args.write:
        args.write.parent.mkdir(parents=True, exist_ok=True)
        args.write.write_text(report, encoding="utf-8")

    payload = {
        "title": title,
        "criteria": [c.to_dict() for c in criteria],
        "rows": rows,
        "report": report,
        "all_verified": all(r["verdict"] == "Verified" for r in rows),
        "notes": [
            "LLM slot: assign Verified/Failed/Unknown with concrete evidence after running checks.",
            "Verified without evidence is rejected by this harness.",
        ],
    }
    if args.json:
        emit({k: v for k, v in payload.items() if k != "report"} | {"report": report}, as_json=True)
    else:
        sys.stdout.write(report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
