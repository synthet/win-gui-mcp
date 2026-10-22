"""Framework self-verify command catalog and secret path gates."""

from __future__ import annotations

import re
from pathlib import Path

FRAMEWORK_VERIFY_COMMANDS: list[tuple[str, str]] = [
    (
        "okf_lint",
        "python scripts/okf_lint.py --profile project --exclude-prefix archive/ docs",
    ),
    ("sync_check", "python scripts/sync_assistant_trees.py --check"),
    ("frontmatter", "python scripts/ci/check_agent_frontmatter.py"),
    ("cli_skills", "python scripts/validate_cli_skills.py"),
    ("pytest", "python -m pytest tests -q"),
]

CLAIM_PROOF_CATALOG: dict[str, dict[str, str]] = {
    "tests_pass": {
        "claim": "Tests pass",
        "proof": "python -m pytest tests -q",
        "not_enough": "Previous run or narrow subset for a broad claim",
    },
    "lint_clean": {
        "claim": "Lint/typecheck clean",
        "proof": "python scripts/okf_lint.py --profile project --exclude-prefix archive/ docs",
        "not_enough": "Formatting only",
    },
    "assets_synced": {
        "claim": "Generated assets in sync",
        "proof": "python scripts/sync_assistant_trees.py --check",
        "not_enough": "Manual copy or assumed sync",
    },
    "frontmatter_ok": {
        "claim": "Agent frontmatter valid",
        "proof": "python scripts/ci/check_agent_frontmatter.py",
        "not_enough": "Spot-check of one file",
    },
    "ready_to_commit": {
        "claim": "Ready to commit",
        "proof": "git status --short && git diff --check",
        "not_enough": "Memory of earlier status",
    },
}

SECRET_PATH_PATTERNS = [
    re.compile(r"(?i)(^|/|\\)\.env(\.|$)"),
    re.compile(r"(?i)secrets\.json$"),
    re.compile(r"(?i)\.pem$"),
    re.compile(r"(?i)id_rsa"),
    re.compile(r"(?i)\.p12$"),
]

# Named gate subsets for task-env-package-tools harness profiles.
VERIFY_PROFILES: dict[str, list[str]] = {
    "framework": [cid for cid, _ in FRAMEWORK_VERIFY_COMMANDS],
    "agent-assets": ["sync_check", "frontmatter", "cli_skills"],
    "tests": ["pytest"],
    "lint": ["okf_lint"],
}


def is_secret_path(path: str | Path) -> bool:
    text = str(path).replace("\\", "/")
    return any(p.search(text) for p in SECRET_PATH_PATTERNS)


def framework_verify_commands() -> list[dict[str, str]]:
    return [{"id": cid, "command": cmd} for cid, cmd in FRAMEWORK_VERIFY_COMMANDS]


def gate_command_map() -> dict[str, str]:
    return {cid: cmd for cid, cmd in FRAMEWORK_VERIFY_COMMANDS}


def resolve_gates(*, profile: str | None = None, gate_ids: list[str] | None = None) -> list[dict[str, str]]:
    """Resolve a profile and/or explicit gate ids to ordered command rows."""
    known = gate_command_map()
    ordered_ids: list[str] = []
    if profile:
        key = profile.strip().lower()
        if key not in VERIFY_PROFILES:
            known_profiles = ", ".join(sorted(VERIFY_PROFILES))
            raise ValueError(f"unknown profile {profile!r}; known: {known_profiles}")
        ordered_ids.extend(VERIFY_PROFILES[key])
    if gate_ids:
        for gid in gate_ids:
            gid = gid.strip()
            if not gid:
                continue
            if gid not in known:
                raise ValueError(f"unknown gate id {gid!r}; known: {', '.join(sorted(known))}")
            if gid not in ordered_ids:
                ordered_ids.append(gid)
    if not ordered_ids:
        ordered_ids = list(known.keys())
    return [{"id": gid, "command": known[gid]} for gid in ordered_ids]


def list_profiles() -> dict[str, list[str]]:
    return {name: list(ids) for name, ids in VERIFY_PROFILES.items()}
