"""Parse memory.md, merge session candidates, emit dream proposal + changelog."""

from __future__ import annotations

import hashlib
import re
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from scripts.agent_memory import yaml_compat as yaml

from scripts.agent_memory import schema
from scripts.agent_memory.paths import load_config

_NORMALIZE_RE = re.compile(r"[^a-z0-9]+")
_DATE_SUFFIX_RE = re.compile(r"\s*\(updated:\s*(\d{4}-\d{2}-\d{2})\)\s*$")


def normalize_text(text: str) -> str:
    return _NORMALIZE_RE.sub(" ", text.lower()).strip()


def _today() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d")


@dataclass
class MemoryItem:
    text: str
    section: str
    sources: list[str] = field(default_factory=list)
    confidence: str = "medium"
    last_updated_at: str = ""
    id: str = ""
    source_hint: str = ""
    verified_at: str = ""
    verification_status: str = "unverified"
    stale_after: str = ""
    related_paths: list[str] = field(default_factory=list)
    related_tasks: list[str] = field(default_factory=list)

    @property
    def summary(self) -> str:
        return self.text

    @property
    def key(self) -> str:
        return f"{self.section}::{normalize_text(self.text)}"


def _stable_id(section: str, text: str) -> str:
    identity = f"{section}\0{normalize_text(text)}".encode("utf-8")
    digest = hashlib.sha1(identity).hexdigest()[:12]
    return f"mem-{digest}"


def _as_list(value: Any) -> list[str]:
    if value is None:
        return []
    if isinstance(value, list):
        return [str(v) for v in value if str(v).strip()]
    return [str(value)] if str(value).strip() else []


def _apply_metadata(item: MemoryItem, metadata: dict[str, Any]) -> None:
    item.id = str(metadata.get("id") or item.id or _stable_id(item.section, item.text))
    item.source_hint = str(metadata.get("source_hint") or item.source_hint or "")
    item.confidence = str(metadata.get("confidence") or item.confidence or "medium")
    item.verified_at = str(metadata.get("verified_at") or item.verified_at or "")
    item.verification_status = str(
        metadata.get("verification_status") or item.verification_status or "unverified"
    )
    item.stale_after = str(metadata.get("stale_after") or item.stale_after or "")
    item.related_paths = _merge_unique(item.related_paths, _as_list(metadata.get("related_paths")))
    item.related_tasks = _merge_unique(item.related_tasks, _as_list(metadata.get("related_tasks")))


def parse_memory_markdown(content: str) -> dict[str, list[MemoryItem]]:
    """Parse H2 sections into lists of MemoryItem, including indented YAML metadata blocks."""
    sections: dict[str, list[MemoryItem]] = {s: [] for s in schema.SECTION_ORDER}
    current: str | None = None
    last_item: MemoryItem | None = None
    meta_lines: list[str] = []

    def flush_meta() -> None:
        nonlocal meta_lines, last_item
        if last_item and meta_lines:
            try:
                metadata = yaml.safe_load("\n".join(meta_lines)) or {}
            except yaml.YAMLError:
                metadata = {}
            if isinstance(metadata, dict):
                _apply_metadata(last_item, metadata)
        meta_lines = []

    for line in content.splitlines():
        if line.startswith("## "):
            flush_meta()
            title = line[3:].strip()
            current = title if title in sections else None
            last_item = None
            continue
        if current and line.startswith("- "):
            flush_meta()
            raw = line[2:].strip()
            if not raw or raw == schema.PLACEHOLDER_NONE:
                last_item = None
                continue
            m = _DATE_SUFFIX_RE.search(raw)
            if m:
                last_updated_at = m.group(1)
                text = raw[: m.start()].strip()
            else:
                last_updated_at = ""
                text = raw
            last_item = MemoryItem(text=text, section=current, last_updated_at=last_updated_at)
            last_item.id = _stable_id(current, text)
            sections[current].append(last_item)
            continue
        if current and last_item and line.startswith("  ") and line.strip():
            meta_lines.append(line[2:])
            continue
        flush_meta()
    flush_meta()
    return sections


def load_sessions(
    raw_dir: Path,
    *,
    max_sessions: int,
    max_bytes: int,
) -> list[tuple[str, dict[str, Any]]]:
    """Load recent YAML sessions newest-first."""
    files = sorted(raw_dir.glob("*.yaml"), key=lambda p: p.stat().st_mtime, reverse=True)
    loaded: list[tuple[str, dict[str, Any]]] = []
    for path in files[:max_sessions]:
        if path.stat().st_size > max_bytes:
            continue
        with path.open(encoding="utf-8") as fh:
            data = yaml.safe_load(fh) or {}
        if isinstance(data, dict):
            loaded.append((path.name, data))
    return loaded


def _item_from_candidate(
    cand: dict[str, Any], session_name: str
) -> MemoryItem | None:
    cat = cand.get("category")
    section = schema.CATEGORY_TO_SECTION.get(cat)
    if not section:
        return None
    text = (cand.get("text") or "").strip()
    if not text:
        return None
    source_hint = str(cand.get("source_hint") or session_name)
    verified_at = str(cand.get("verified_at") or _today())
    return MemoryItem(
        text=str(cand.get("summary") or text),
        section=section,
        sources=[session_name],
        confidence=cand.get("confidence") or "medium",
        last_updated_at=_today(),
        id=str(cand.get("id") or _stable_id(section, text)),
        source_hint=source_hint,
        verified_at=verified_at,
        verification_status=str(cand.get("verification_status") or "verified"),
        stale_after=str(cand.get("stale_after") or ""),
        related_paths=_as_list(cand.get("related_paths")),
        related_tasks=_as_list(
            cand.get("related_tasks") or cand.get("related_issues") or cand.get("related_prs")
        ),
    )


def merge_sections(
    base: dict[str, list[MemoryItem]],
    sessions: list[tuple[str, dict[str, Any]]],
    *,
    max_per_section: int,
) -> tuple[dict[str, list[MemoryItem]], dict[str, list[str]]]:
    """
    Merge session candidates into base sections.
    Returns (merged_sections, changelog_dict).
    """
    changelog: dict[str, list[str]] = {
        "added": [],
        "updated": [],
        "removed": [],
        "deprecated": [],
        "uncertain": [],
    }
    merged = {
        section: [
            MemoryItem(
                i.text,
                i.section,
                list(i.sources),
                i.confidence,
                i.last_updated_at,
                i.id,
                i.source_hint,
                i.verified_at,
                i.verification_status,
                i.stale_after,
                list(i.related_paths),
                list(i.related_tasks),
            )
            for i in items
        ]
        for section, items in base.items()
    }
    index: dict[str, MemoryItem] = {}
    for section, items in merged.items():
        for item in items:
            index[item.key] = item

    open_section = "Open Questions"

    for session_name, data in sessions:
        for cand in data.get("memory_candidates") or []:
            if not isinstance(cand, dict):
                continue
            new_item = _item_from_candidate(cand, session_name)
            if not new_item:
                continue
            existing = index.get(new_item.key)
            if existing:
                for src in new_item.sources:
                    if src not in existing.sources:
                        existing.sources.append(src)
                _merge_item_metadata(existing, new_item)
                existing.last_updated_at = _today()
                continue
            # Contradiction: same normalized text in different section?
            cross = _find_cross_section_conflict(merged, new_item)
            if cross:
                msg = (
                    f'"{new_item.text}" conflicts with existing in "{cross.section}" '
                    f"(from: {session_name})"
                )
                changelog["uncertain"].append(msg)
                oq = MemoryItem(
                    text=f"Resolve: {new_item.text} vs {cross.text}",
                    section=open_section,
                    sources=[session_name],
                    confidence="low",
                )
                okey = oq.key
                if okey not in index:
                    merged[open_section].append(oq)
                    index[okey] = oq
                    changelog["added"].append(f"{oq.text} (from: {session_name})")
                continue
            merged[new_item.section].append(new_item)
            index[new_item.key] = new_item
            changelog["added"].append(f"{new_item.text} (from: {session_name})")

    for section in schema.SECTION_ORDER:
        items = merged[section]
        if len(items) > max_per_section:
            dropped = items[max_per_section:]
            merged[section] = items[:max_per_section]
            for d in dropped:
                changelog["removed"].append(f"{d.text} (truncated: section limit)")

    return merged, changelog


def _merge_unique(left: list[str], right: list[str]) -> list[str]:
    out = list(left)
    for value in right:
        if value and value not in out:
            out.append(value)
    return out


def _merge_item_metadata(existing: MemoryItem, new_item: MemoryItem) -> None:
    if not existing.id:
        existing.id = new_item.id
    if (
        new_item.source_hint
        and new_item.source_hint not in existing.source_hint.split("; ")
    ):
        existing.source_hint = "; ".join(
            [v for v in [existing.source_hint, new_item.source_hint] if v]
        )
    if schema.confidence_rank(new_item.confidence) > schema.confidence_rank(
        existing.confidence
    ):
        existing.confidence = new_item.confidence
    if new_item.verified_at and new_item.verified_at > existing.verified_at:
        existing.verified_at = new_item.verified_at
    if new_item.verification_status == "verified":
        existing.verification_status = new_item.verification_status
    if new_item.stale_after and (
        not existing.stale_after or new_item.stale_after > existing.stale_after
    ):
        existing.stale_after = new_item.stale_after
    existing.related_paths = _merge_unique(existing.related_paths, new_item.related_paths)
    existing.related_tasks = _merge_unique(existing.related_tasks, new_item.related_tasks)


def _find_cross_section_conflict(
    merged: dict[str, list[MemoryItem]], new_item: MemoryItem
) -> MemoryItem | None:
    norm = normalize_text(new_item.text)
    for section, items in merged.items():
        if section == new_item.section:
            continue
        for item in items:
            if normalize_text(item.text) == norm:
                return item
    return None


def diff_against_base(
    base: dict[str, list[MemoryItem]],
    merged: dict[str, list[MemoryItem]],
) -> dict[str, list[str]]:
    """Compare merged to original base for updated/removed."""
    changelog: dict[str, list[str]] = {
        "added": [],
        "updated": [],
        "removed": [],
        "deprecated": [],
        "uncertain": [],
    }
    for section in schema.SECTION_ORDER:
        base_keys = {normalize_text(i.text) for i in base[section]}
        merged_keys = {normalize_text(i.text) for i in merged[section]}
        for item in merged[section]:
            n = normalize_text(item.text)
            if n not in base_keys:
                entry = item.text
                if item.sources:
                    entry += f" (from: {', '.join(item.sources)})"
                if entry not in changelog["added"]:
                    changelog["added"].append(entry)
        for item in base[section]:
            n = normalize_text(item.text)
            if n not in merged_keys:
                changelog["removed"].append(item.text)
    return changelog


def merge_changelogs(*parts: dict[str, list[str]]) -> dict[str, list[str]]:
    out: dict[str, list[str]] = {
        "added": [],
        "updated": [],
        "removed": [],
        "deprecated": [],
        "uncertain": [],
        "stale": [],
    }
    seen: dict[str, set[str]] = {k: set() for k in out}
    for part in parts:
        for key, entries in part.items():
            if key not in seen:
                continue
            for e in entries:
                if e not in seen[key]:
                    seen[key].add(e)
                    out[key].append(e)
    return out


def find_stale_items(
    sections: dict[str, list[MemoryItem]],
    *,
    staleness_days: int,
) -> list[str]:
    """Return changelog entries for items whose last_updated_at is older than staleness_days."""
    today = datetime.now(timezone.utc).date()
    entries: list[str] = []
    for section, items in sections.items():
        for item in items:
            if not item.last_updated_at:
                continue
            try:
                updated = datetime.strptime(item.last_updated_at, "%Y-%m-%d").date()
            except ValueError:
                continue
            age = (today - updated).days
            explicit_stale = False
            if item.stale_after:
                try:
                    explicit_stale = today >= datetime.strptime(
                        item.stale_after, "%Y-%m-%d"
                    ).date()
                except ValueError:
                    explicit_stale = False
            if age >= staleness_days or explicit_stale:
                reason = (
                    f"stale after: {item.stale_after}"
                    if explicit_stale
                    else f"last updated: {item.last_updated_at}, {age} days ago"
                )
                entries.append(f"{item.text} ({reason}) [section: {section}]")
    return entries


def render_memory_markdown(
    sections: dict[str, list[MemoryItem]],
    *,
    front_matter: dict[str, Any] | None = None,
) -> str:
    lines: list[str] = []
    if front_matter:
        lines.append("---")
        lines.append(yaml.safe_dump(front_matter, default_flow_style=False).strip())
        lines.append("---")
        lines.append("")
    lines.append("# Project Memory")
    lines.append("")
    lines.append("Proposed consolidated memory (review before promote).")
    lines.append("")
    for section in schema.SECTION_ORDER:
        lines.append(f"## {section}")
        lines.append("")
        items = sections[section]
        if not items:
            lines.append(f"- {schema.PLACEHOLDER_NONE}")
        else:
            for item in items:
                if not item.id:
                    item.id = _stable_id(item.section, item.text)
                suffix = f" (updated: {item.last_updated_at})" if item.last_updated_at else ""
                lines.append(f"- {item.text}{suffix}")
                metadata = {
                    "id": item.id,
                    "summary": item.summary,
                    "source_hint": item.source_hint,
                    "confidence": item.confidence,
                    "verified_at": item.verified_at,
                    "verification_status": item.verification_status,
                    "stale_after": item.stale_after,
                    "related_paths": item.related_paths,
                    "related_tasks": item.related_tasks,
                }
                rendered = (
                    yaml.safe_dump(metadata, sort_keys=False, default_flow_style=False)
                    .rstrip()
                    .splitlines()
                )
                lines.extend(f"  {line}" for line in rendered)
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def render_changelog(changelog: dict[str, list[str]]) -> str:
    titles = {
        "added": "Added",
        "updated": "Updated",
        "removed": "Removed",
        "deprecated": "Deprecated",
        "uncertain": "Uncertain / needs review",
        "stale": "Stale / needs re-verification",
    }
    lines = ["# Dream changelog", ""]
    for key, title in titles.items():
        lines.append(f"## {title}")
        lines.append("")
        entries = changelog.get(key) or []
        if not entries:
            lines.append("- (none)")
        else:
            for e in entries:
                lines.append(f"- {e}")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def run_dream(
    repo_root: Path,
    *,
    max_sessions: int | None = None,
    staleness_days: int | None = None,
) -> tuple[Path, Path]:
    """Generate dream + changelog paths. Never writes memory.md."""
    config = load_config(repo_root)
    dirs = {
        "raw": repo_root / ".agent-memory" / "raw-sessions",
        "dreams": repo_root / ".agent-memory" / "dreams",
        "memory": repo_root / ".agent-memory" / "memory.md",
    }
    dirs["dreams"].mkdir(parents=True, exist_ok=True)

    base_content = ""
    if dirs["memory"].is_file():
        base_content = dirs["memory"].read_text(encoding="utf-8")

    base_sections = parse_memory_markdown(base_content)
    sessions = load_sessions(
        dirs["raw"],
        max_sessions=max_sessions or config["max_sessions"],
        max_bytes=config["max_session_bytes"],
    )
    session_names = [n for n, _ in sessions]

    merged, cand_changelog = merge_sections(
        base_sections,
        sessions,
        max_per_section=config["max_items_per_section"],
    )
    struct_changelog = diff_against_base(base_sections, merged)
    changelog = merge_changelogs(cand_changelog, struct_changelog)

    threshold = staleness_days if staleness_days is not None else config.get("staleness_threshold_days", 180)
    stale = find_stale_items(merged, staleness_days=threshold)
    changelog["stale"] = stale

    from scripts.agent_memory.limits import dream_timestamp_slug

    slug = dream_timestamp_slug()
    item_counts = {s: len(merged[s]) for s in schema.SECTION_ORDER}
    front_matter = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "source_sessions": session_names,
        "item_counts": item_counts,
    }
    dream_path = dirs["dreams"] / f"{slug}.md"
    changelog_path = dirs["dreams"] / f"{slug}-changelog.md"
    dream_body = render_memory_markdown(merged, front_matter=front_matter)
    from scripts.agent_memory.secrets import assert_no_secrets

    assert_no_secrets(dream_body, context="dream proposal")
    assert_no_secrets(render_changelog(changelog), context="changelog")
    dream_path.write_text(dream_body, encoding="utf-8")
    changelog_path.write_text(render_changelog(changelog), encoding="utf-8")
    return dream_path, changelog_path


def extract_body_from_dream(content: str) -> str:
    """Strip YAML front matter and dream-only banners for promotion."""
    body = content
    if body.startswith("---"):
        parts = body.split("---", 2)
        if len(parts) >= 3:
            body = parts[2].lstrip("\n")
    lines = body.splitlines()
    filtered: list[str] = []
    for line in lines:
        if "Proposed consolidated memory" in line:
            continue
        filtered.append(line)
    body = "\n".join(filtered).rstrip() + "\n"
    if not body.startswith("# Project Memory"):
        body = "# Project Memory\n\n" + body.lstrip()
    return body


def promote_dream(repo_root: Path, dream_path: Path, *, force: bool = False) -> Path:
    """Promote dream to memory.md with archive."""
    memory_path = repo_root / ".agent-memory" / "memory.md"
    archive_dir = repo_root / ".agent-memory" / "dreams" / "archive"
    archive_dir.mkdir(parents=True, exist_ok=True)

    changelog_path = dream_path.with_name(f"{dream_path.stem}-changelog.md")
    if not changelog_path.is_file() and not force:
        raise FileNotFoundError(
            f"Changelog missing: {changelog_path}. Use --force to promote anyway."
        )

    content = dream_path.read_text(encoding="utf-8")
    from scripts.agent_memory.secrets import assert_no_secrets

    assert_no_secrets(content, context="dream promotion")
    body = extract_body_from_dream(content)

    from scripts.agent_memory.limits import dream_timestamp_slug

    if memory_path.is_file():
        archive_name = f"memory-{dream_timestamp_slug()}.md"
        archive_path = archive_dir / archive_name
        archive_path.write_text(memory_path.read_text(encoding="utf-8"), encoding="utf-8")

    memory_path.write_text(body, encoding="utf-8")
    return memory_path


def load_context(repo_root: Path) -> str:
    """Load memory.md for context printing."""
    memory_path = repo_root / ".agent-memory" / "memory.md"
    if not memory_path.is_file():
        return ""
    return memory_path.read_text(encoding="utf-8")
