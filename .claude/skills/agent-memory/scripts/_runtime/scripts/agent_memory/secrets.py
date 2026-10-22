"""Detect likely secrets before writing memory artifacts."""

from __future__ import annotations

import re
from typing import Iterable

# Patterns: (name, compiled regex)
_PATTERNS: list[tuple[str, re.Pattern[str]]] = [
    ("openai_api_key", re.compile(r"sk-[A-Za-z0-9]{20,}", re.I)),
    ("anthropic_api_key", re.compile(r"sk-ant-[A-Za-z0-9\-]{20,}", re.I)),
    ("aws_access_key", re.compile(r"AKIA[0-9A-Z]{16}")),
    ("bearer_token", re.compile(r"Bearer\s+[A-Za-z0-9\-._~+/]+=*", re.I)),
    ("password_assignment", re.compile(r"password\s*=\s*['\"]?.+['\"]?", re.I)),
    ("pem_private_key", re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----")),
    (
        "secrets_json_load",
        re.compile(
            r"(?:open|read|load|Path)\s*\([^)]*secrets\.json",
            re.I,
        ),
    ),
    ("env_secret_assignment", re.compile(r"[A-Z0-9_]*(?:PASSWORD|SECRET|TOKEN|API_KEY)\s*=\s*\S+", re.I)),
    ("github_pat", re.compile(r"ghp_[A-Za-z0-9]{36,}", re.I)),
    ("generic_api_key", re.compile(r"api[_-]?key\s*[:=]\s*['\"]?[A-Za-z0-9]{16,}", re.I)),
]


def find_secret_hits(text: str) -> list[str]:
    """Return human-readable labels for matched secret patterns."""
    hits: list[str] = []
    for name, pattern in _PATTERNS:
        if pattern.search(text):
            hits.append(name)
    return hits


def assert_no_secrets(text: str, *, context: str = "content") -> None:
    hits = find_secret_hits(text)
    if hits:
        raise ValueError(
            f"Refusing to write {context}: possible secret detected ({', '.join(hits)}). "
            "Redact and retry."
        )


def assert_no_secrets_in_iterable(lines: Iterable[str], *, context: str = "content") -> None:
    for line in lines:
        assert_no_secrets(line, context=context)
