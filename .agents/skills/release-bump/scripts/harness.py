#!/usr/bin/env python3
"""Compiled harness for the release-bump skill.

Detects version source, bumps semver, promotes CHANGELOG Unreleased.
Semver *level* is an LLM judgment slot — pass --level after deciding.
Does not commit, tag, or push.
"""

from __future__ import annotations

import argparse
import sys
from datetime import date
from pathlib import Path

_ROOT = Path(__file__).resolve().parent / '_runtime'
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.changelog import promote_unreleased, unreleased_bullets  # noqa: E402
from scripts.skill_harness.io_util import emit, find_repo_root  # noqa: E402
from scripts.skill_harness.verify_catalog import framework_verify_commands  # noqa: E402
from scripts.skill_harness.version import (  # noqa: E402
    bump_semver,
    detect_version_source,
    write_version,
)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=None, help="Repository root")
    parser.add_argument(
        "--level",
        choices=("major", "minor", "patch"),
        help="Semver bump level (LLM judgment slot — required to apply)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Write VERSION/pyproject/package.json and CHANGELOG.md",
    )
    parser.add_argument("--date", default=None, help="Release date YYYY-MM-DD (default: today)")
    parser.add_argument("--json", action="store_true", help="Emit JSON")
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    source = detect_version_source(repo)
    changelog_path = repo / "CHANGELOG.md"
    changelog_text = changelog_path.read_text(encoding="utf-8") if changelog_path.is_file() else ""

    result: dict = {
        "repo": str(repo),
        "version_source": None,
        "current_version": None,
        "proposed_version": None,
        "unreleased_bullets": unreleased_bullets(changelog_text) if changelog_text else [],
        "verify_commands": framework_verify_commands(),
        "applied": False,
        "notes": [],
    }

    if source is None:
        result["notes"].append(
            "No version source found (VERSION / pyproject.toml / package.json). Ask the user where version lives."
        )
        emit(result, as_json=args.json)
        return 1

    result["version_source"] = {"kind": source.kind, "path": str(source.path.relative_to(repo))}
    result["current_version"] = source.version

    if not args.level:
        result["notes"].append(
            "LLM slot: choose major|minor|patch from git log + Unreleased bullets, then re-run with --level."
        )
        emit(result, as_json=args.json)
        return 0

    new_version = bump_semver(source.version, args.level)
    result["proposed_version"] = new_version
    result["level"] = args.level

    release_date = date.fromisoformat(args.date) if args.date else date.today()

    if args.apply:
        if not changelog_path.is_file():
            result["notes"].append("CHANGELOG.md missing; refusing to apply.")
            emit(result, as_json=args.json)
            return 1
        write_version(source, new_version)
        updated = promote_unreleased(changelog_text, new_version, release_date)
        changelog_path.write_text(updated, encoding="utf-8")
        result["applied"] = True
        result["notes"].append(
            "Files updated. Run verify_commands, then commit only when the user asks "
            "(chore(release): vX.Y.Z). Do not tag/push unless requested."
        )
    else:
        result["notes"].append("Dry-run only. Pass --apply to write version + changelog.")

    emit(result, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
