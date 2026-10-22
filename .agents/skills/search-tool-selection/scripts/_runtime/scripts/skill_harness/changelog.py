"""Keep a Changelog helpers."""

from __future__ import annotations

import re
from datetime import date

UNRELEASED_RE = re.compile(
    r"(?ms)^## \[Unreleased\]\s*\n(.*?)(?=^## \[|\Z)"
)


def promote_unreleased(changelog_text: str, version: str, release_date: date | None = None) -> str:
    """Rename ``## [Unreleased]`` to ``## [version] — YYYY-MM-DD`` and insert a fresh empty Unreleased.

    Empty subsections under the promoted section are dropped. Entries are left intact.
    """
    day = (release_date or date.today()).isoformat()
    match = UNRELEASED_RE.search(changelog_text)
    if not match:
        raise ValueError("CHANGELOG.md has no ## [Unreleased] section")

    body = match.group(1)
    body = _drop_empty_subsections(body).rstrip() + "\n" if body.strip() else ""

    promoted = f"## [{version}] — {day}\n"
    if body.strip():
        promoted += "\n" + body.lstrip("\n")
        if not promoted.endswith("\n"):
            promoted += "\n"

    fresh = "## [Unreleased]\n\n"
    start, end = match.span()
    return changelog_text[:start] + fresh + promoted + "\n" + changelog_text[end:].lstrip("\n")


def _drop_empty_subsections(body: str) -> str:
    """Remove ``### Heading`` blocks that contain no bullet or paragraph content."""
    parts = re.split(r"(?m)(^(?:### .+)\n)", body)
    if len(parts) == 1:
        return body
    out: list[str] = [parts[0]]
    i = 1
    while i < len(parts):
        heading = parts[i]
        content = parts[i + 1] if i + 1 < len(parts) else ""
        if content.strip():
            out.append(heading)
            out.append(content)
        i += 2
    return "".join(out)


def unreleased_bullets(changelog_text: str) -> list[str]:
    match = UNRELEASED_RE.search(changelog_text)
    if not match:
        return []
    bullets: list[str] = []
    for line in match.group(1).splitlines():
        stripped = line.strip()
        if stripped.startswith(("- ", "* ")):
            bullets.append(stripped[2:].strip())
    return bullets
