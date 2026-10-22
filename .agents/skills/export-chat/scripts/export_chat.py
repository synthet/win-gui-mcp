#!/usr/bin/env python3
"""Export a Cursor agent chat transcript to Markdown (mimics Claude Code ``/export``).

Locates the newest transcript under
``~/.cursor/projects/<workspace-slug>/agent-transcripts/`` (or the one named by
``--transcript``), renders user/assistant text plus full tool-call inputs, and
writes it to a file or the clipboard. Stdlib only so the skill can be copied to
``~/.cursor/skills/`` and run from any project.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

WRAPPER_TAGS = (
    "agent_skills",
    "agent_transcripts",
    "attached_files",
    "git_status",
    "manually_attached_skills",
    "open_and_recently_viewed_files",
    "rules",
    "system_notification",
    "system_reminder",
    "timestamp",
    "user_info",
)
_WRAPPER_RE = re.compile(
    r"<(" + "|".join(WRAPPER_TAGS) + r")\b[^>]*>.*?</\1>", re.DOTALL
)
_USER_QUERY_RE = re.compile(r"<user_query>(.*?)</user_query>", re.DOTALL)


def workspace_slug(workspace: Path) -> str:
    """``D:\\Projects\\my-app`` -> ``D-Projects-my-app`` (Cursor's project dir naming)."""
    return re.sub(r"[^A-Za-z0-9]+", "-", str(workspace)).strip("-")


def find_transcript_dir(cursor_home: Path, workspace: Path) -> Path | None:
    projects = cursor_home / "projects"
    if not projects.is_dir():
        return None
    wanted = workspace_slug(workspace).lower()
    for child in projects.iterdir():
        if child.is_dir() and child.name.lower() == wanted:
            transcripts = child / "agent-transcripts"
            return transcripts if transcripts.is_dir() else None
    return None


def list_transcripts(transcript_dir: Path) -> list[Path]:
    """All transcripts (nested ``<uuid>/<uuid>.jsonl`` and flat ``<uuid>.jsonl``), newest first."""
    found = {p for p in transcript_dir.glob("*.jsonl")}
    found |= {p for p in transcript_dir.glob("*/*.jsonl") if p.stem == p.parent.name}
    return sorted(found, key=lambda p: p.stat().st_mtime, reverse=True)


def resolve_transcript(ref: str, transcript_dir: Path | None) -> Path | None:
    path = Path(ref).expanduser()
    if path.is_file():
        return path
    if transcript_dir is None:
        return None
    matches = [p for p in list_transcripts(transcript_dir) if p.stem.startswith(ref)]
    return matches[0] if len(matches) == 1 else None


def load_events(path: Path) -> list[dict[str, Any]]:
    events = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            events.append(json.loads(line))
        except json.JSONDecodeError:
            continue
    return events


def clean_user_text(text: str, *, raw: bool) -> str:
    if raw:
        return text.strip()
    queries = _USER_QUERY_RE.findall(text)
    if queries:
        return "\n\n".join(q.strip() for q in queries)
    return _WRAPPER_RE.sub("", text).strip()


def _blocks(event: dict[str, Any]) -> list[dict[str, Any]]:
    content = (event.get("message") or {}).get("content")
    if isinstance(content, str):
        return [{"type": "text", "text": content}]
    return [b for b in content or [] if isinstance(b, dict)]


def first_prompt(events: list[dict[str, Any]]) -> str:
    for event in events:
        if event.get("role") != "user":
            continue
        for block in _blocks(event):
            if block.get("type") == "text":
                text = clean_user_text(block.get("text", ""), raw=False)
                if text:
                    return text
    return ""


def _fence(body: str) -> str:
    longest = max((len(m) for m in re.findall(r"`+", body)), default=0)
    return "`" * max(3, longest + 1)


def render_markdown(
    events: list[dict[str, Any]],
    *,
    transcript_id: str,
    workspace: str,
    raw: bool = False,
    exported_at: datetime | None = None,
) -> tuple[str, dict[str, int]]:
    exported_at = exported_at or datetime.now().astimezone()
    out = [
        "# Chat export",
        "",
        f"- Transcript: `{transcript_id}`",
        f"- Workspace: `{workspace}`",
        f"- Exported: {exported_at.isoformat(timespec='seconds')}",
        "",
    ]
    stats = {"user_messages": 0, "assistant_messages": 0, "tool_calls": 0, "turns": 0}
    last_role = None

    for event in events:
        if event.get("type") == "turn_ended":
            stats["turns"] += 1
            out += ["---", ""]
            last_role = None
            continue
        role = event.get("role")
        if role not in ("user", "assistant"):
            continue

        parts: list[str] = []
        for block in _blocks(event):
            kind = block.get("type")
            if kind == "text":
                text = block.get("text", "")
                text = clean_user_text(text, raw=raw) if role == "user" else text.strip()
                if text:
                    parts.append(text)
            elif kind == "tool_use":
                stats["tool_calls"] += 1
                body = json.dumps(block.get("input", {}), indent=2, ensure_ascii=False)
                fence = _fence(body)
                parts.append(f"**Tool: {block.get('name', '?')}**\n\n{fence}json\n{body}\n{fence}")
        if not parts:
            continue

        stats[f"{role}_messages"] += 1
        if role != last_role:
            out += [f"## {role.capitalize()}", ""]
            last_role = role
        for part in parts:
            out += [part, ""]

    return "\n".join(out).rstrip() + "\n", stats


def default_filename(prompt: str, now: datetime | None = None) -> str:
    now = now or datetime.now()
    slug = re.sub(r"[^a-z0-9]+", "-", prompt.lower()).strip("-")[:50].rstrip("-")
    return f"{now:%Y-%m-%d-%H%M%S}-{slug or 'chat'}.md"


def copy_to_clipboard(text: str) -> str:
    """Copy *text*; return the tool used. Raises RuntimeError when none is available."""
    if sys.platform == "win32":
        subprocess.run(["clip"], input=text.encode("utf-16"), check=True)
        return "clip"
    candidates = [
        ["pbcopy"],
        ["wl-copy"],
        ["xclip", "-selection", "clipboard"],
        ["xsel", "--clipboard", "--input"],
    ]
    for cmd in candidates:
        if shutil.which(cmd[0]):
            subprocess.run(cmd, input=text.encode("utf-8"), check=True)
            return cmd[0]
    raise RuntimeError("no clipboard tool found (pbcopy, wl-copy, xclip, xsel)")


def _emit(data: dict[str, Any], *, as_json: bool, message: str) -> None:
    if as_json:
        json.dump(data, sys.stdout, indent=2, ensure_ascii=False)
        sys.stdout.write("\n")
    else:
        print(message)


def main(argv: list[str] | None = None) -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("output", nargs="?", help="Output file (default: timestamped .md in cwd)")
    parser.add_argument("--clipboard", action="store_true", help="Copy to clipboard instead of a file")
    parser.add_argument("--transcript", help="Transcript uuid (prefix ok) or .jsonl path")
    parser.add_argument("--workspace", type=Path, default=None, help="Workspace path (default: cwd)")
    parser.add_argument("--cursor-home", type=Path, default=Path.home() / ".cursor")
    parser.add_argument("--list", action="store_true", help="List recent transcripts and exit")
    parser.add_argument("--limit", type=int, default=10, help="Rows for --list")
    parser.add_argument("--raw", action="store_true", help="Keep injected wrapper tags in user messages")
    parser.add_argument("--json", action="store_true", help="Print a JSON result summary")
    args = parser.parse_args(argv)

    workspace = (args.workspace or Path.cwd()).resolve()
    transcript_dir = find_transcript_dir(args.cursor_home, workspace)

    def fail(msg: str) -> int:
        _emit({"ok": False, "error": msg}, as_json=args.json, message=f"error: {msg}")
        return 1

    if args.list:
        if transcript_dir is None:
            return fail(f"no Cursor transcripts found for workspace {workspace}")
        rows = []
        for path in list_transcripts(transcript_dir)[: args.limit]:
            mtime = datetime.fromtimestamp(path.stat().st_mtime)
            prompt = first_prompt(load_events(path)).splitlines()
            rows.append(
                {
                    "id": path.stem,
                    "modified": mtime.isoformat(timespec="seconds"),
                    "first_prompt": prompt[0][:80] if prompt else "",
                    "path": str(path),
                }
            )
        lines = [f"{r['modified']}  {r['id']}  {r['first_prompt']}" for r in rows]
        _emit({"ok": True, "transcripts": rows}, as_json=args.json, message="\n".join(lines))
        return 0

    if args.transcript:
        transcript = resolve_transcript(args.transcript, transcript_dir)
        if transcript is None:
            return fail(f"transcript not found or ambiguous: {args.transcript}")
    else:
        if transcript_dir is None:
            return fail(
                f"no Cursor transcripts found for workspace {workspace}; pass --transcript or --workspace"
            )
        transcripts = list_transcripts(transcript_dir)
        if not transcripts:
            return fail(f"no transcripts in {transcript_dir}")
        transcript = transcripts[0]

    events = load_events(transcript)
    markdown, stats = render_markdown(
        events, transcript_id=transcript.stem, workspace=str(workspace), raw=args.raw
    )
    result: dict[str, Any] = {"ok": True, "transcript": str(transcript), **stats}

    if args.clipboard:
        try:
            result["clipboard"] = copy_to_clipboard(markdown)
        except (RuntimeError, OSError, subprocess.CalledProcessError) as exc:
            return fail(f"clipboard copy failed: {exc}")
        _emit(result, as_json=args.json, message=f"Copied {transcript.stem} to clipboard")
        return 0

    output = Path(args.output) if args.output else Path.cwd() / default_filename(first_prompt(events))
    output = output.expanduser().resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(markdown, encoding="utf-8")
    result["output"] = str(output)
    _emit(result, as_json=args.json, message=f"Exported {transcript.stem} to {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
