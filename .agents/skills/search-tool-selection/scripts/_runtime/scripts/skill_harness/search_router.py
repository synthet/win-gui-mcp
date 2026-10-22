"""Search tool selection decision table."""

from __future__ import annotations

from dataclasses import asdict, dataclass


@dataclass(frozen=True)
class SearchRecommendation:
    task: str
    use: str
    avoid: str
    notes: str

    def to_dict(self) -> dict[str, str]:
        return asdict(self)


TABLE: dict[str, SearchRecommendation] = {
    "filename": SearchRecommendation(
        "File by name/path",
        "fd",
        "grep -r for filenames",
        "Prefer IDE Glob on Windows when available.",
    ),
    "content": SearchRecommendation(
        "Text/literals in contents",
        "rg",
        "bare grep -r .",
        "Always prefer rg; use grep only if rg unavailable.",
    ),
    "syntax": SearchRecommendation(
        "Syntax/AST shapes",
        "ast-grep",
        "regex alone",
        "Escalate after rg false positives.",
    ),
    "security": SearchRecommendation(
        "Security rule packs",
        "semgrep scan",
        "ad hoc rg for policy",
        "Use project policy packs when present.",
    ),
    "symbols": SearchRecommendation(
        "Symbol index / cross-ref",
        "ctags, Serena MCP, Zoekt",
        "rg for every ref",
        "Prefer indexed tools for repeated cross-refs.",
    ),
    "layout": SearchRecommendation(
        "Repo layout",
        "tree -L 3",
        "loading all paths",
        "Bound depth; exclude node_modules/dist/build/.git.",
    ),
    "config": SearchRecommendation(
        "Config keys in JSON/YAML",
        "jq / yq",
        "rg on minified JSON",
        "Parse structured files instead of regex.",
    ),
    "repeated": SearchRecommendation(
        "Repeated repo search (MCP connected)",
        "fff ffgrep/fffind",
        "many grep tool roundtrips",
        "Fall back to rg/fd when fff MCP is unavailable.",
    ),
    "cursor": SearchRecommendation(
        "Cursor agent first pass",
        "Grep / SemanticSearch / Glob",
        "shell when tool bound",
        "IDE tools often beat shell on Windows.",
    ),
}

ALIASES: dict[str, str] = {
    "name": "filename",
    "file": "filename",
    "path": "filename",
    "text": "content",
    "literal": "content",
    "grep": "content",
    "rg": "content",
    "ast": "syntax",
    "ast-grep": "syntax",
    "refactor": "syntax",
    "semgrep": "security",
    "policy": "security",
    "symbol": "symbols",
    "ctags": "symbols",
    "tree": "layout",
    "structure": "layout",
    "json": "config",
    "yaml": "config",
    "jq": "config",
    "yq": "config",
    "fff": "repeated",
    "mcp-search": "repeated",
    "ide": "cursor",
}


def recommend(task_type: str) -> SearchRecommendation:
    key = task_type.strip().lower().replace(" ", "_").replace("-", "_")
    # normalize underscores then try aliases
    key_compact = key.replace("_", "")
    resolved = TABLE.get(key) or TABLE.get(key.replace("_", "-"))
    if resolved is None:
        for alias, target in ALIASES.items():
            if key == alias or key_compact == alias.replace("-", "").replace("_", ""):
                resolved = TABLE[target]
                break
    if resolved is None and key in TABLE:
        resolved = TABLE[key]
    # try direct keys without underscore
    if resolved is None:
        for table_key, rec in TABLE.items():
            if key.replace("_", "") == table_key.replace("_", "").replace("-", ""):
                resolved = rec
                break
    if resolved is None:
        known = ", ".join(sorted(TABLE))
        raise ValueError(f"unknown task type {task_type!r}; known: {known}")
    return resolved


def list_task_types() -> list[str]:
    return sorted(TABLE)
