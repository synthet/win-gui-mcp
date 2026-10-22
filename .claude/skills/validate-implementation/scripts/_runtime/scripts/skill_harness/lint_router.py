"""Lint / format / security stack decision table."""

from __future__ import annotations

from dataclasses import asdict, dataclass


@dataclass(frozen=True)
class LintRecommendation:
    stack: str
    check_commands: list[str]
    avoid: str
    confirmation_gates: list[str]
    notes: str

    def to_dict(self) -> dict:
        return asdict(self)


TABLE: dict[str, LintRecommendation] = {
    "python": LintRecommendation(
        "python",
        [
            "ruff check {paths} --output-format=concise",
            "ruff format --check {paths}",
        ],
        "project-wide ruff --fix without approval; inventing pyright when AGENTS.md has no typecheck",
        ["ruff check --fix", "ruff format (write mode)"],
        "Scope to changed paths. Prefer project-local ruff via uv/venv.",
    ),
    "js_ts": LintRecommendation(
        "js_ts",
        [
            "npx eslint {paths} --max-warnings 0",
            "npx prettier --check {paths}",
        ],
        "global eslint; broad --fix/--write without approval",
        ["eslint --fix", "prettier --write"],
        "Prefer project-local npx over global installs.",
    ),
    "shell": LintRecommendation(
        "shell",
        ["shellcheck {paths}"],
        "running shellcheck on binary/data files",
        [],
        "Bound globs to *.sh / scripts; shellcheck is check-only.",
    ),
    "container": LintRecommendation(
        "container",
        [
            "hadolint Dockerfile",
            "trivy fs --scanners vuln --exit-code 0 .",
        ],
        "trivy image scans of unrelated registries without intent",
        [],
        "trivy DB download may need network once. Scope Dockerfile path when known.",
    ),
    "secrets": LintRecommendation(
        "secrets",
        ["gitleaks detect --source . --no-git -v"],
        "committing .env / secrets.json; disabling secret scanners to go green",
        [],
        "Never paste secret values into prompts or logs. Prefer project CI config when present.",
    ),
    "rust": LintRecommendation(
        "rust",
        [
            "cargo fmt --check",
            "cargo clippy --all-targets -- -D warnings",
        ],
        "cargo clippy --fix or cargo fmt (write) on a broad tree without approval",
        ["cargo fmt", "cargo clippy --fix"],
        "Prefer the repo's AGENTS.md lint command when it is stricter. Scope clippy to the touched crate when practical.",
    ),
}

ALIASES: dict[str, str] = {
    "py": "python",
    "ruff": "python",
    "python3": "python",
    "js": "js_ts",
    "ts": "js_ts",
    "typescript": "js_ts",
    "javascript": "js_ts",
    "eslint": "js_ts",
    "prettier": "js_ts",
    "bash": "shell",
    "sh": "shell",
    "shellcheck": "shell",
    "docker": "container",
    "dockerfile": "container",
    "hadolint": "container",
    "trivy": "container",
    "gitleaks": "secrets",
    "secret": "secrets",
    "cargo": "rust",
    "clippy": "rust",
    "rustfmt": "rust",
    "rs": "rust",
}


def recommend(stack: str, paths: str = "path/to/module") -> LintRecommendation:
    key = stack.strip().lower().replace(" ", "_").replace("-", "_")
    resolved_key = TABLE.get(key) and key
    if resolved_key is None:
        alias_target = ALIASES.get(key) or ALIASES.get(key.replace("_", ""))
        if alias_target:
            resolved_key = alias_target
    if resolved_key is None:
        compact = key.replace("_", "")
        for table_key in TABLE:
            if compact == table_key.replace("_", "").replace("-", ""):
                resolved_key = table_key
                break
    if resolved_key is None:
        known = ", ".join(sorted(TABLE))
        raise ValueError(f"unknown stack {stack!r}; known: {known}")

    base = TABLE[resolved_key]
    path_token = paths.strip() or "path/to/module"
    return LintRecommendation(
        stack=base.stack,
        check_commands=[c.format(paths=path_token) for c in base.check_commands],
        avoid=base.avoid,
        confirmation_gates=list(base.confirmation_gates),
        notes=base.notes,
    )


def list_stacks() -> list[str]:
    return sorted(TABLE)
