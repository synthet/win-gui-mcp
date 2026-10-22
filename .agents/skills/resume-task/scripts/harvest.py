#!/usr/bin/env python3
"""Reconstruct ongoing work threads from recent coding-agent transcripts (for /resume-task).

Reads sessions from Claude Code, OpenAI Codex CLI, Cursor (IDE + cursor-agent CLI),
Google Antigravity (IDE + agy CLI) and exported Markdown/text transcripts
(SpecStory, Claude Code /export, hand exports), then writes:

  <out>/digest.md       compact digest grouped by project: per session its state,
                        goal, where it stopped, files touched, open items, resume command
  <out>/candidates.json  the same data plus the last few turns of every session

Standard library only. Read-only: never writes to any agent's store.

Examples
  python3 harvest.py --probe                    # what stores exist here?
  python3 harvest.py --days 7 --out ./nt        # last week, everything
  python3 harvest.py --project ~/src/ProjectTwelve --md ~/exports
  python3 harvest.py --grep "chunk streaming" --days 30   # one named thread
"""
from __future__ import annotations

import argparse
import glob
import json
import os
import re
import shutil
import shlex
import sqlite3
import subprocess
import sys
import tempfile
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
from urllib.parse import unquote, urlparse

UTC = timezone.utc
SNIP = 300
PROFILE_ROOTS: list[Path] = []

# --------------------------------------------------------------------------- utils

SECRET_PATTERNS = [
    re.compile(r"sk-(?:ant-|proj-)?[A-Za-z0-9_\-]{16,}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
    re.compile(r"github_pat_[A-Za-z0-9_]{20,}"),
    re.compile(r"AKIA[0-9A-Z]{16}"),
    re.compile(r"xox[abprs]-[A-Za-z0-9\-]{10,}"),
    re.compile(r"AIza[0-9A-Za-z_\-]{30,}"),
    re.compile(r"eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}"),
    re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?-----END [A-Z ]*PRIVATE KEY-----"),
    re.compile(r"(?i)\b(bearer)\s+[A-Za-z0-9._\-]{16,}"),
    re.compile(r"(?i)\b(password|passwd|secret|api[_-]?key|token)\s*[:=]\s*\S{6,}"),
]


def redact(s: str) -> str:
    for p in SECRET_PATTERNS:
        s = p.sub(lambda m: (m.group(1) + " " if m.lastindex else "") + "[REDACTED]", s)
    return s


def redact_report(value):
    if isinstance(value, str):
        return redact(value)
    if isinstance(value, dict):
        return {redact(str(key)): redact_report(item) for key, item in value.items()}
    if isinstance(value, list):
        return [redact_report(item) for item in value]
    return value


NOISE_BLOCKS = re.compile(
    r"<(system-reminder|environment_context|user_instructions|command-message|command-args|"
    r"local-command-stdout|local-command-stderr|ide_opened_file|ide_selection)>[\s\S]*?</\1>",
    re.I,
)


def clean(s: str) -> str:
    s = NOISE_BLOCKS.sub(" ", s or "")
    s = re.sub(r"<command-name>\s*(\S+)\s*</command-name>", r"\1", s)
    return s.strip()


def one_line(s: str, n: int = SNIP) -> str:
    s = re.sub(r"\s+", " ", redact(s or "")).strip()
    return s if len(s) <= n else s[: n - 1] + "…"


def tail(s: str, n: int = SNIP) -> str:
    s = re.sub(r"\s+", " ", redact(s or "")).strip()
    return s if len(s) <= n else "…" + s[-(n - 1):]


def parse_time(v) -> datetime | None:
    if v is None or v == "":
        return None
    try:
        if isinstance(v, (int, float)) or (isinstance(v, str) and v.isdigit()):
            x = float(v)
            if x > 1e14:      # microseconds
                x /= 1e6
            elif x > 1e11:    # milliseconds
                x /= 1e3
            return datetime.fromtimestamp(x, UTC)
        s = str(v).strip().replace("Z", "+00:00")
        d = datetime.fromisoformat(s)
        return d if d.tzinfo else d.replace(tzinfo=UTC)
    except Exception:
        return None


def mtime(p: Path) -> datetime:
    try:
        return datetime.fromtimestamp(p.stat().st_mtime, UTC)
    except OSError:
        return datetime.fromtimestamp(0, UTC)


def texts(obj, depth: int = 0) -> list[str]:
    """Pull human-readable text out of arbitrary message JSON."""
    if depth > 6 or obj is None:
        return []
    if isinstance(obj, str):
        return [obj]
    if isinstance(obj, list):
        out = []
        for x in obj:
            out += texts(x, depth + 1)
        return out
    if isinstance(obj, dict):
        t = obj.get("type")
        if t in ("tool_use", "tool_result", "function_call", "function_call_output",
                 "image", "thinking", "redacted_thinking", "reasoning"):
            return []
        for k in ("text", "content", "message", "value", "markdown", "body"):
            if k in obj:
                r = texts(obj[k], depth + 1)
                if r:
                    return r
    return []


def file_uri_to_path(u: str) -> str | None:
    if not u:
        return None
    if u.startswith("file://"):
        p = unquote(urlparse(u).path)
        if re.match(r"^/[A-Za-z]:/", p):  # /c:/Users/...
            p = p[1:]
        return p
    if u.startswith("/") or re.match(r"^[A-Za-z]:[\\/]", u):
        return u
    return None


def read_jsonl(path: Path):
    try:
        with open(path, "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    yield json.loads(line)
                except json.JSONDecodeError:
                    continue
    except OSError:
        return


def open_sqlite_ro(path: Path):
    """Open a possibly-live SQLite DB without disturbing the owning app."""
    try:
        con = sqlite3.connect(f"file:{path}?mode=ro", uri=True, timeout=2)
        con.execute("select 1").fetchone()
        return con, None
    except sqlite3.Error:
        pass
    tmpdir = tempfile.mkdtemp(prefix="nt-db-")
    try:
        for suffix in ("", "-wal", "-shm", "-journal"):
            src = Path(str(path) + suffix)
            if src.exists():
                shutil.copy2(src, Path(tmpdir) / (path.name + suffix))
        con = sqlite3.connect(f"file:{Path(tmpdir) / path.name}?mode=ro", uri=True)
        return con, tmpdir
    except Exception:
        shutil.rmtree(tmpdir, ignore_errors=True)
        return None, None


def homes() -> list[Path]:
    return list(dict.fromkeys([Path.home(), *PROFILE_ROOTS]))


# --------------------------------------------------------------------------- model

@dataclass
class Turn:
    role: str            # "user" | "assistant"
    text: str
    ts: datetime | None = None


@dataclass
class Session:
    tool: str
    sid: str
    path: str
    project: str | None = None
    title: str | None = None
    started: datetime | None = None
    updated: datetime | None = None
    turns: list[Turn] = field(default_factory=list)
    todos: list[tuple[str, str]] = field(default_factory=list)   # latest snapshot (text, status)
    plan_docs: list[tuple[str, str]] = field(default_factory=list)  # (name, markdown)
    interrupted: bool = False
    note: str | None = None
    files: list[str] = field(default_factory=list)          # files the agent edited/created

    def touch(self, f):
        if f and isinstance(f, str) and f not in self.files:
            self.files.append(f)

    def first_ask(self) -> str:
        for t in self.turns:
            if t.role == "user" and len(t.text) > 3:
                return t.text
        return self.title or ""

    def finalize(self):
        stamps = [t.ts for t in self.turns if t.ts]
        if stamps:
            self.started = self.started or min(stamps)
            self.updated = max([self.updated] + stamps) if self.updated else max(stamps)
        if not self.title:
            self.title = one_line(self.first_ask(), 120)


# --------------------------------------------------------------------------- Claude Code

def claude_roots() -> list[Path]:
    roots = []
    if os.environ.get("CLAUDE_CONFIG_DIR"):
        roots.append(Path(os.environ["CLAUDE_CONFIG_DIR"]).expanduser())
    for h in homes():
        roots += [h / ".claude", h / ".config" / "claude"]
    return [r for r in dict.fromkeys(roots) if (r / "projects").is_dir()]


def claude_files(since: datetime, include_sub: bool) -> list[Path]:
    out = []
    for r in claude_roots():
        for p in (r / "projects").rglob("*.jsonl"):
            if not include_sub and "subagents" in p.parts:
                continue
            if mtime(p) >= since:
                out.append(p)
    return out


def parse_claude(path: Path) -> Session | None:
    s = Session(tool="claude-code", sid=path.stem, path=str(path))
    tasks: dict[str, dict] = {}          # id -> {subject, status}
    pending_create: dict[str, str] = {}  # tool_use_id -> subject
    for o in read_jsonl(path):
        typ = o.get("type")
        if typ == "summary" and o.get("summary"):
            s.title = s.title or one_line(o["summary"], 120)
            continue
        if typ == "custom-title" and o.get("customTitle"):
            s.title = one_line(o["customTitle"], 120)
            continue
        if typ not in ("user", "assistant"):
            continue
        if o.get("isSidechain") and "subagents" not in path.parts:
            continue
        s.project = s.project or o.get("cwd")
        s.sid = o.get("sessionId", s.sid)
        ts = parse_time(o.get("timestamp"))
        msg = o.get("message") or {}
        content = msg.get("content")
        blocks = content if isinstance(content, list) else [content]
        for b in blocks:
            if isinstance(b, dict) and b.get("type") == "tool_use":
                name, inp = b.get("name", ""), b.get("input") or {}
                if name == "TodoWrite" and isinstance(inp.get("todos"), list):
                    s.todos = [(t.get("content") or t.get("activeForm") or "", t.get("status", ""))
                               for t in inp["todos"] if isinstance(t, dict)]
                elif name == "TaskCreate":
                    pending_create[b.get("id", "")] = inp.get("subject") or inp.get("description") or ""
                elif name == "TaskUpdate" and inp.get("taskId") is not None:
                    t = tasks.setdefault(str(inp["taskId"]), {"subject": "", "status": "pending"})
                    if inp.get("status"):
                        t["status"] = inp["status"]
                    if inp.get("subject"):
                        t["subject"] = inp["subject"]
                elif name in ("Edit", "Write", "MultiEdit", "NotebookEdit"):
                    s.touch(inp.get("file_path") or inp.get("notebook_path"))
                elif name == "ExitPlanMode" and inp.get("plan"):
                    s.plan_docs.append(("plan", str(inp["plan"])))
            elif isinstance(b, dict) and b.get("type") == "tool_result":
                tid = b.get("tool_use_id", "")
                if tid in pending_create:
                    m = re.search(r"Task #?(\d+)", " ".join(texts(b.get("content"))))
                    key = m.group(1) if m else f"c{len(tasks)+1}"
                    tasks.setdefault(key, {"subject": "", "status": "pending"})["subject"] = pending_create.pop(tid)
        if o.get("isMeta") or o.get("isCompactSummary"):
            continue
        txt = clean("\n".join(texts(content)))
        if not txt:
            continue
        if "[Request interrupted" in txt:
            s.interrupted = True
            continue
        if txt.startswith("Caveat: The messages below"):
            continue
        s.turns.append(Turn("user" if typ == "user" else "assistant", txt, ts))
    if tasks and not s.todos:
        s.todos = [(v["subject"], v["status"]) for v in tasks.values() if v["subject"] and v["status"] != "deleted"]
    elif tasks:
        s.todos += [(v["subject"], v["status"]) for v in tasks.values() if v["subject"] and v["status"] != "deleted"]
    s.updated = mtime(path)
    s.finalize()
    return s if s.turns or s.todos else None


# --------------------------------------------------------------------------- Codex

def codex_files(since: datetime) -> list[Path]:
    roots = []
    if os.environ.get("CODEX_HOME"):
        roots.append(Path(os.environ["CODEX_HOME"]).expanduser())
    roots += [h / ".codex" for h in homes()]
    out = []
    for r in dict.fromkeys(roots):
        for sub in ("sessions", "archived_sessions"):
            d = r / sub
            if d.is_dir():
                out += [p for p in d.rglob("rollout-*.jsonl") if mtime(p) >= since]
                out += [p for p in d.rglob("rollout-*.json") if mtime(p) >= since]
    return out


CODEX_SKIP = re.compile(r"^\s*(<environment_context>|<user_instructions>|# AGENTS\.md|<permissions)", re.I)


def parse_codex(path: Path) -> Session | None:
    s = Session(tool="codex", sid=path.stem.replace("rollout-", ""), path=str(path))
    resp_turns, event_turns = [], []
    if path.suffix == ".json":  # very old single-JSON format
        try:
            data = json.loads(path.read_text(encoding="utf-8", errors="replace"))
            items = data.get("items", []) if isinstance(data, dict) else data
        except Exception:
            items = []
        lines = [{"type": "response_item", "payload": it} for it in items]
    else:
        lines = read_jsonl(path)
    for o in lines:
        typ, p = o.get("type"), o.get("payload")
        if p is None and o.get("type") in ("message", "function_call"):  # legacy flat lines
            typ, p = "response_item", o
        p = p or {}
        ts = parse_time(o.get("timestamp"))
        if typ == "session_meta":
            s.project = p.get("cwd") or s.project
            s.sid = p.get("id", s.sid)
            s.started = parse_time(p.get("timestamp")) or s.started
        elif typ == "turn_context":
            s.project = s.project or p.get("cwd")
        elif typ == "response_item":
            pt = p.get("type")
            if pt == "message" and p.get("role") in ("user", "assistant"):
                txt = clean("\n".join(texts(p.get("content"))))
                if txt and not CODEX_SKIP.match(txt):
                    resp_turns.append(Turn(p["role"], txt, ts))
            elif pt in ("function_call", "custom_tool_call", "local_shell_call") and p.get("name") != "update_plan":
                raw = json.dumps(p)
                for f in re.findall(r"\*\*\* (?:Update|Add) File: ([^\\\n\"]+)", raw):
                    s.touch(f.strip())
            elif pt == "function_call" and p.get("name") == "update_plan":
                try:
                    args = json.loads(p.get("arguments") or "{}")
                    s.todos = [(x.get("step", ""), x.get("status", "")) for x in args.get("plan", [])]
                except Exception:
                    pass
        elif typ == "event_msg":
            pt = p.get("type")
            if pt == "user_message" and p.get("message"):
                txt = clean(p["message"])
                if txt and not CODEX_SKIP.match(txt):
                    event_turns.append(Turn("user", txt, ts))
            elif pt == "agent_message" and p.get("message"):
                event_turns.append(Turn("assistant", clean(p["message"]), ts))
            elif pt == "turn_aborted":
                s.interrupted = True
            elif pt == "task_complete":
                s.interrupted = False
    s.turns = resp_turns if resp_turns else event_turns
    s.updated = mtime(path)
    s.finalize()
    return s if s.turns or s.todos else None


# --------------------------------------------------------------------------- Cursor IDE

def cursor_user_dirs() -> list[Path]:
    cands = []
    for h in homes():
        cands += [h / "Library/Application Support/Cursor/User",
                  h / ".config/Cursor/User",
                  h / "AppData/Roaming/Cursor/User"]
    if os.environ.get("APPDATA"):
        cands.append(Path(os.environ["APPDATA"]) / "Cursor/User")
    return [c for c in dict.fromkeys(cands) if (c / "globalStorage/state.vscdb").exists()]


def cursor_workspace_map(user_dir: Path) -> dict[str, str]:
    """composerId -> workspace folder path."""
    m: dict[str, str] = {}
    ws = user_dir / "workspaceStorage"
    if not ws.is_dir():
        return m
    for d in ws.iterdir():
        db, wj = d / "state.vscdb", d / "workspace.json"
        if not db.exists():
            continue
        folder = None
        try:
            j = json.loads(wj.read_text(encoding="utf-8"))
            folder = file_uri_to_path(j.get("folder") or j.get("workspace") or "")
        except Exception:
            pass
        con, tmp = open_sqlite_ro(db)
        if not con:
            continue
        try:
            row = con.execute("select value from ItemTable where key='composer.composerData'").fetchone()
            if row:
                data = json.loads(row[0])
                for c in data.get("allComposers", []):
                    if c.get("composerId") and folder:
                        m[c["composerId"]] = folder
        except Exception:
            pass
        finally:
            con.close()
            if tmp:
                shutil.rmtree(tmp, ignore_errors=True)
    return m


def parse_cursor_ide(since: datetime) -> list[Session]:
    out = []
    for ud in cursor_user_dirs():
        wsmap = cursor_workspace_map(ud)
        con, tmp = open_sqlite_ro(ud / "globalStorage/state.vscdb")
        if not con:
            continue
        try:
            rows = con.execute("select key, value from cursorDiskKV where key like 'composerData:%'").fetchall()
            for key, val in rows:
                try:
                    cd = json.loads(val)
                except Exception:
                    continue
                if not isinstance(cd, dict):
                    continue
                cid = key.split(":", 1)[1]
                upd = parse_time(cd.get("lastUpdatedAt")) or parse_time(cd.get("createdAt"))
                if not upd or upd < since:
                    continue
                s = Session(tool="cursor", sid=cid, path=f"{ud}/globalStorage/state.vscdb#{key}",
                            project=wsmap.get(cid), title=cd.get("name"),
                            started=parse_time(cd.get("createdAt")), updated=upd)
                bubbles = []
                if isinstance(cd.get("conversation"), list) and cd["conversation"]:
                    bubbles = cd["conversation"]
                else:
                    for h in cd.get("fullConversationHeadersOnly") or []:
                        r = con.execute("select value from cursorDiskKV where key=?",
                                        (f"bubbleId:{cid}:{h.get('bubbleId')}",)).fetchone()
                        if r:
                            try:
                                bubbles.append(json.loads(r[0]))
                            except Exception:
                                pass
                for b in bubbles:
                    if not isinstance(b, dict):
                        continue
                    role = "user" if b.get("type") == 1 else "assistant" if b.get("type") == 2 else None
                    txt = clean(b.get("text") or "")
                    if role and txt:
                        s.turns.append(Turn(role, txt, parse_time(b.get("createdAt"))))
                    if not s.project:
                        for f in (b.get("relevantFiles") or [])[:1]:
                            s.project = os.path.dirname(f) if isinstance(f, str) and f.startswith("/") else None
                todos = cd.get("todos")
                if isinstance(todos, list):
                    s.todos = [(t.get("content") or t.get("text") or "", str(t.get("status", "")).lower())
                               for t in todos if isinstance(t, dict)]
                s.finalize()
                if s.turns or s.todos:
                    out.append(s)
        except sqlite3.Error as e:
            print(f"[cursor] {ud}: {e}", file=sys.stderr)
        finally:
            con.close()
            if tmp:
                shutil.rmtree(tmp, ignore_errors=True)
    return out


# --------------------------------------------------------------------------- Cursor CLI (cursor-agent)

def parse_cursor_cli(since: datetime) -> list[Session]:
    """Best effort: ~/.cursor/chats/<hash>/<id>/store.db (meta + content-addressed blobs)."""
    out = []
    for h in homes():
        for db in glob.glob(str(h / ".cursor/chats/*/*/store.db")):
            p = Path(db)
            if mtime(p) < since:
                continue
            con, tmp = open_sqlite_ro(p)
            if not con:
                continue
            s = Session(tool="cursor-agent", sid=p.parent.name, path=db, updated=mtime(p))
            try:
                tables = {r[0] for r in con.execute("select name from sqlite_master where type='table'")}
                if "meta" in tables:
                    for (v,) in con.execute("select value from meta"):
                        try:
                            raw = bytes.fromhex(v) if isinstance(v, str) and re.fullmatch(r"[0-9a-f]+", v) else v
                            j = json.loads(raw)
                            s.title = s.title or j.get("name")
                            s.project = s.project or j.get("cwd") or j.get("workspace")
                        except Exception:
                            pass
                if "blobs" in tables:
                    for (data,) in con.execute("select data from blobs"):
                        try:
                            j = json.loads(data if isinstance(data, str) else bytes(data).decode("utf-8"))
                        except Exception:
                            continue
                        if isinstance(j, dict) and j.get("role") in ("user", "assistant"):
                            txt = clean("\n".join(texts(j.get("content"))))
                            if txt:
                                s.turns.append(Turn(j["role"], txt))
            except sqlite3.Error:
                pass
            finally:
                con.close()
                if tmp:
                    shutil.rmtree(tmp, ignore_errors=True)
            s.finalize()
            if s.turns:
                s.note = "cursor-agent store parsed best-effort"
                out.append(s)
    return out


# --------------------------------------------------------------------------- Antigravity

AG_ROLE_USER = re.compile(r"user|human", re.I)
AG_ROLE_AI = re.compile(r"assistant|model|agent|planner|gemini|bot", re.I)


def ag_role(o: dict) -> str | None:
    for k in ("role", "author", "source", "type", "speaker", "sender"):
        v = o.get(k)
        if isinstance(v, str):
            if AG_ROLE_USER.search(v):
                return "user"
            if AG_ROLE_AI.search(v) and not re.search(r"tool|function|result|system", v, re.I):
                return "assistant"
    if isinstance(o.get("message"), dict):
        return ag_role(o["message"])
    return None


def find_paths(obj, acc: list[str], depth=0):
    if depth > 5 or len(acc) > 50:
        return
    if isinstance(obj, str):
        p = file_uri_to_path(obj) if obj.startswith("file://") else None
        if p:
            acc.append(p)
    elif isinstance(obj, dict):
        for k, v in obj.items():
            if k in ("cwd", "workspace", "workspacePath", "workspaceRoot", "projectRoot") and isinstance(v, str):
                p = file_uri_to_path(v)
                if p:
                    acc.insert(0, p)
            find_paths(v, acc, depth + 1)
    elif isinstance(obj, list):
        for v in obj[:50]:
            find_paths(v, acc, depth + 1)


def guess_project(paths: list[str]) -> str | None:
    paths = [p for p in paths if p]
    if not paths:
        return None
    try:
        dirs = [p if not os.path.splitext(p)[1] else os.path.dirname(p) for p in paths[:30]]
        return os.path.commonpath(dirs) or None
    except ValueError:
        return Counter(os.path.dirname(p) for p in paths).most_common(1)[0][0]


def parse_antigravity(since: datetime) -> list[Session]:
    out = []
    for h in homes():
        for root, label in ((h / ".gemini/antigravity", "antigravity"),
                            (h / ".gemini/antigravity-cli", "antigravity-cli")):
            brain = root / "brain"
            if not brain.is_dir():
                continue
            for d in brain.iterdir():
                if not d.is_dir():
                    continue
                files = [f for f in d.rglob("*") if f.is_file()]
                if not files:
                    continue
                newest = max(mtime(f) for f in files)
                if newest < since:
                    continue
                s = Session(tool=label, sid=d.name, path=str(d), updated=newest)
                pathhints: list[str] = []
                # Plan artifacts (task.md, implementation_plan.md, walkthrough.md, *.md.resolved)
                for f in sorted(d.glob("*.md")) + sorted(d.glob("*.md.resolved")):
                    try:
                        md = f.read_text(encoding="utf-8", errors="replace")
                    except OSError:
                        continue
                    name = f.name.replace(".resolved", "")
                    if any(n == name for n, _ in s.plan_docs):
                        continue
                    s.plan_docs.append((name, md))
                    if name == "task.md":
                        s.todos = checkbox_items(md)
                        m = re.search(r"^#\s+(.+)$", md, re.M)
                        if m:
                            s.title = one_line(m.group(1), 120)
                    find_paths(re.findall(r"file://[^\s)\]>\"']+", md), pathhints)
                for f in d.glob("*.metadata.json"):
                    try:
                        find_paths(json.loads(f.read_text(encoding="utf-8")), pathhints)
                    except Exception:
                        pass
                # CLI display-log transcripts
                logs = sorted(d.glob(".system_generated/logs/transcript*.jsonl"))
                full = [x for x in logs if "full" in x.name] or logs[:1]
                for lf in full:
                    for o in read_jsonl(lf):
                        if not isinstance(o, dict):
                            continue
                        if len(pathhints) < 20:
                            find_paths(o, pathhints)
                        role = ag_role(o)
                        if not role:
                            continue
                        txt = clean("\n".join(texts(o)))
                        if txt:
                            ts = parse_time(o.get("timestamp") or o.get("createdAt") or o.get("time"))
                            s.turns.append(Turn(role, txt, ts))
                s.project = guess_project(pathhints)
                if not s.turns:
                    s.note = "no readable transcript (IDE .pb conversations are opaque); using brain artifacts only"
                s.finalize()
                if s.turns or s.todos or s.plan_docs:
                    out.append(s)
    return out


# --------------------------------------------------------------------------- Markdown / text exports

ROLE_LINE = [
    re.compile(r"^\s*_\*\*(?P<r>[A-Za-z][\w .()-]{0,30}?)\*\*_\s*$"),                       # SpecStory
    re.compile(r"^\s*#{1,4}\s*(?:\W{0,3}\s*)?(?P<r>User|Human|You|Me|Assistant|Claude|Codex|Agent|Model|AI|Cursor|Gemini|Antigravity)\b.*$", re.I),
    re.compile(r"^\s*\*\*(?P<r>User|Human|You|Me|Assistant|Claude|Codex|Agent|AI|Model|Cursor|Gemini)\s*:?\*\*:?", re.I),
    re.compile(r"^(?P<r>Human|User|Assistant|Claude|AI|Codex|Gemini):\s", re.I),
]
USERISH = re.compile(r"^(user|human|you|me)\b", re.I)


def split_md_turns(md: str) -> list[Turn]:
    lines = md.splitlines()
    turns: list[Turn] = []
    cur_role, buf = None, []

    def flush():
        if cur_role and buf:
            t = clean("\n".join(x for x in buf if not re.fullmatch(r"\s*(-{3,}|\*{3,}|_{3,})\s*", x)))
            if t:
                turns.append(Turn(cur_role, t))

    if "⏺" in md:  # Claude Code /export transcript
        for ln in lines:
            if ln.startswith("> "):
                if cur_role != "user":
                    flush(); cur_role, buf = "user", []
                buf.append(ln[2:])
            elif ln.startswith("⏺"):
                flush(); cur_role, buf = "assistant", [ln.lstrip("⏺ ")]
            else:
                buf.append(ln)
        flush()
        return turns

    for ln in lines:
        role = None
        for rx in ROLE_LINE:
            m = rx.match(ln)
            if m:
                role = "user" if USERISH.match(m.group("r").strip()) else "assistant"
                break
        if role:
            flush()
            cur_role, buf = role, []
            rest = ln.split(":", 1)[1] if rx is ROLE_LINE[3] and ":" in ln else ""
            if rest.strip():
                buf.append(rest)
        else:
            buf.append(ln)
    flush()
    return turns


def date_from_name(name: str) -> datetime | None:
    m = re.search(r"(\d{4}-\d{2}-\d{2})(?:[_T](\d{2})-(\d{2}))?", name)
    if not m:
        return None
    try:
        d = datetime.strptime(m.group(1), "%Y-%m-%d").replace(tzinfo=UTC)
        if m.group(2):
            d = d.replace(hour=int(m.group(2)), minute=int(m.group(3)))
        return d
    except ValueError:
        return None


def guess_tool_from_md(md: str, path: Path) -> str:
    low = md[:4000].lower()
    if ".specstory" in str(path):
        if "cursor" in low:
            return "cursor (specstory)"
        if "claude" in low:
            return "claude-code (specstory)"
        if "codex" in low:
            return "codex (specstory)"
        return "specstory"
    if "⏺" in md or "claude code" in low:
        return "claude-code (export)"
    if "codex" in low:
        return "codex (export)"
    if "antigravity" in low or "gemini" in low:
        return "antigravity (export)"
    if "cursor" in low:
        return "cursor (export)"
    return "md-export"


def md_files(paths: list[str], projects: list[str], since: datetime, explicit_any_age: bool) -> list[tuple[Path, str | None]]:
    out: list[tuple[Path, str | None]] = []
    seen = set()
    for p in paths:
        P = Path(p).expanduser()
        cands = [P] if P.is_file() else [x for x in P.rglob("*") if x.suffix.lower() in (".md", ".markdown", ".txt")]
        for f in cands:
            if f in seen:
                continue
            seen.add(f)
            when = date_from_name(f.name) or mtime(f)
            if explicit_any_age and P.is_file() or when >= since:
                out.append((f, None))
    for proj in projects:
        hist = Path(proj).expanduser() / ".specstory" / "history"
        if hist.is_dir():
            for f in hist.glob("*.md"):
                if f not in seen and (date_from_name(f.name) or mtime(f)) >= since:
                    seen.add(f)
                    out.append((f, str(Path(proj).expanduser().resolve())))
    return out


def parse_md(path: Path, project: str | None) -> Session | None:
    try:
        md = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return None
    s = Session(tool=guess_tool_from_md(md, path), sid=path.stem, path=str(path), project=project,
                updated=mtime(path), started=date_from_name(path.name))
    s.turns = split_md_turns(md)
    if not s.turns:
        s.turns = [Turn("assistant", md)]
        s.note = "no speaker markers found; treated as one document"
    m = re.search(r"^#\s+(.+)$", md, re.M)
    s.title = one_line(m.group(1), 120) if m else None
    if not project:
        hints: list[str] = []
        m2 = re.search(r"(?im)^(?:cwd|workspace|project|directory)\s*[:=]\s*(\S+)", md)
        if m2:
            s.project = file_uri_to_path(m2.group(1)) or m2.group(1)
        elif ".specstory" in path.parts:
            s.project = str(path.parents[2])
        else:
            find_paths(re.findall(r"file://[^\s)\]>\"']+", md), hints)
            s.project = guess_project(hints)
    s.finalize()
    return s


# --------------------------------------------------------------------------- candidate extraction

DONE = {"completed", "complete", "done", "finished", "cancelled", "canceled", "x"}
CHECKBOX = re.compile(r"^\s*[-*+]\s*\[(?P<m>[ xX/~\-!>])\]\s*(?P<t>.+)$")
SECTION = re.compile(
    r"(next steps?|what'?s (?:left|next)|remaining|to-?dos?|follow[- ]?ups?|known (?:issues?|limitations?)|"
    r"limitations|open (?:questions|items|issues)|not (?:yet )?(?:done|implemented|covered)|blockers?|"
    r"future work|deferred|out of scope|caveats|still to do|action items)", re.I)
HEADINGISH = re.compile(r"^\s*(#{1,6}\s+|\*\*[^*]+\*\*\s*:?\s*$|[A-Z][^.!?]{2,60}:\s*$)")
BULLET = re.compile(r"^\s*(?:[-*+•]|\d+[.)])\s+(.+)$")
FOLLOW = re.compile(
    r"\b(next step|to ?do\b|fixme|still (?:need|needs|to)|not (?:yet|done|implemented|wired|hooked|tested|verified)|"
    r"left to|follow[- ]?up|deferred|postponed|out of scope|known issue|blocked|couldn'?t|could not|unable to|"
    r"didn'?t get to|(?:has|have|hasn'?t|haven'?t)(?: not)? been (?:run|tested|verified|applied|deployed|pushed|merged|committed)|haven'?t (?:yet )?(?:been )?(?:tested|run|verified|implemented|done)|you(?:'ll| will)? need to|"
    r"once you|tbd|wip|placeholder|stub(?:bed)?|hard-?coded for now|for now)\b", re.I)
USER_DEFER = re.compile(
    r"\b(later|next time|tomorrow|afterwards|after that|todo|remind me|park (?:this|that|it)|for now|"
    r"eventually|another session|separately|come back to|follow[- ]?up|in a separate)\b", re.I)


def checkbox_items(md: str) -> list[tuple[str, str]]:
    out = []
    for ln in md.splitlines():
        m = CHECKBOX.match(ln)
        if m:
            mark = m.group("m")
            st = "completed" if mark in "xX" else "in_progress" if mark in "/~>" else \
                 "cancelled" if mark == "-" else "pending"
            out.append((m.group("t").strip(), st))
    return out


def section_bullets(text: str) -> list[tuple[str, str]]:
    out, cur = [], None
    for ln in text.splitlines():
        if HEADINGISH.match(ln) or (ln.strip().endswith(":") and len(ln) < 80):
            m = SECTION.search(ln)
            cur = m.group(1).lower() if m else None
            continue
        if cur:
            b = BULLET.match(ln)
            if b:
                cb = CHECKBOX.match(ln)
                if cb and cb.group("m") in "xX":
                    continue
                out.append((cur, re.sub(r"^\[[ /~]\]\s*", "", b.group(1)).strip()))
            elif ln.strip() == "":
                continue
            elif not ln.startswith((" ", "\t")):
                cur = None
    return out


def sentences_with(rx: re.Pattern, text: str, limit: int = 6) -> list[str]:
    out = []
    for chunk in re.split(r"(?<=[.!?])\s+|\n+", text):
        c = chunk.strip(" -*•\t")
        if 12 <= len(c) <= 400 and rx.search(c) and not c.startswith(("```", "|")):
            out.append(c)
            if len(out) >= limit:
                break
    return out


def extract(s: Session) -> list[dict]:
    c: list[dict] = []

    def add(kind, text, who, weight, ctx=""):
        t = one_line(re.sub(r"[*_`]{1,3}", "", text), SNIP)
        if len(t) >= 6:
            c.append({"kind": kind, "text": t, "by": who, "weight": weight, "context": one_line(ctx, 200)})

    for t, st in s.todos:
        if str(st).lower() not in DONE and t:
            add("todo-" + (str(st).lower() or "pending"), t, "agent-plan", 5)
    for name, md in s.plan_docs:
        for t, st in checkbox_items(md):
            if st not in DONE and name != "task.md":  # task.md already in todos
                add(f"plan-checkbox:{name}", t, "agent-plan", 4)
        if name in ("walkthrough.md", "plan"):
            for sec, b in section_bullets(md):
                add(f"plan-section:{sec}", b, "agent", 3)
    users = [t for t in s.turns if t.role == "user"]
    asst = [t for t in s.turns if t.role == "assistant"]
    for t in asst[-3:]:
        for sec, b in section_bullets(t.text):
            add(f"agent-section:{sec}", b, "agent", 3)
    if asst:
        for sent in sentences_with(FOLLOW, asst[-1].text):
            add("agent-followup", sent, "agent", 2)
    for u in users:
        for sent in sentences_with(USER_DEFER, u.text, 3):
            add("user-deferred", sent, "user", 4, ctx=f"said {u.ts:%Y-%m-%d}" if u.ts else "")
    if s.turns and s.turns[-1].role == "user":
        add("unanswered-ask", s.turns[-1].text, "user", 5, ctx="session ended on this user message")
    if s.interrupted:
        last_u = users[-1].text if users else s.first_ask()
        add("interrupted", last_u, "user", 5, ctx="turn was interrupted/aborted")
    # within-session dedupe
    seen, uniq = set(), []
    for x in c:
        k = re.sub(r"\W+", " ", x["text"].lower()).strip()[:90]
        if k not in seen:
            seen.add(k)
            uniq.append(x)
    return uniq


STRONG = ("unanswered-ask", "interrupted", "todo-", "user-deferred", "plan-checkbox")


def thread_state(s: Session, cands: list[dict]) -> str:
    kinds = [c["kind"] for c in cands]
    if "interrupted" in kinds:
        return "interrupted"
    if "unanswered-ask" in kinds:
        return "awaiting-reply"
    if any(k.startswith("todo-in_progress") for k in kinds):
        return "mid-plan"
    if any(k.startswith(STRONG) for k in kinds):
        return "open-items"
    if cands:
        return "loose-ends"
    return "looks-finished"


def resume_cmd(s: Session) -> str | None:
    quote = (lambda value: "'" + value.replace("'", "''") + "'") if os.name == "nt" else shlex.quote
    cd = (("Set-Location -LiteralPath " if os.name == "nt" else "cd ") + quote(s.project) + ("; " if os.name == "nt" else " && ")) if s.project and os.path.isabs(s.project) else ""
    if s.tool == "claude-code":
        return f"{cd}claude --resume {quote(s.sid)}"
    if s.tool == "codex":
        return f"{cd}codex resume {quote(s.sid)}"
    if s.tool == "antigravity-cli":
        return f"{cd}agy --conversation {quote(s.sid)}"
    if s.tool == "cursor":
        return f'Cursor: open the project, then the chat named "{one_line(s.title or "", 60)}" in chat history'
    if s.tool == "antigravity":
        return f'Antigravity: open the conversation "{one_line(s.title or "", 60)}" in the Agent Manager'
    return None


# --------------------------------------------------------------------------- git context

def git_context(project: str, since: datetime) -> dict | None:
    if not project or not os.path.isdir(project) or not shutil.which("git"):
        return None

    def g(*args):
        try:
            r = subprocess.run(["git", "-C", project, *args], capture_output=True, text=True, timeout=10)
            return r.stdout.strip() if r.returncode == 0 else ""
        except Exception:
            return ""

    top = g("rev-parse", "--show-toplevel")
    if not top:
        return None
    return {
        "root": top,
        "branch": g("rev-parse", "--abbrev-ref", "HEAD"),
        "dirty_files": len([l for l in g("status", "--porcelain").splitlines() if l.strip()]),
        "unpushed": len(g("log", "--oneline", "@{u}..").splitlines()),
        "stashes": len(g("stash", "list").splitlines()),
        "recent_commits": g("log", f"--since={since.isoformat()}", "--date=short",
                            "--pretty=%h %ad %s", "-n", "25").splitlines(),
    }


# --------------------------------------------------------------------------- main

_ROOTS: dict[str, str] = {}


def repo_root(p: str | None) -> str | None:
    """Collapse a cwd/file path to its git repo root when it exists on this machine."""
    if not p:
        return p
    if p in _ROOTS:
        return _ROOTS[p]
    cur = Path(p).expanduser()
    res = p
    try:
        probe_dir = cur if cur.is_dir() else cur.parent
        for d in [probe_dir, *probe_dir.parents]:
            if (d / ".git").exists():
                res = str(d)
                break
    except OSError:
        pass
    _ROOTS[p] = res
    return res


def project_key(p: str | None) -> str:
    if not p:
        return "(unknown project)"
    p = p.rstrip("/\\")
    return p.replace("\\", "/")


def under(p: str | None, roots: list[str]) -> bool:
    if not roots:
        return True
    if not p:
        return False
    pn = os.path.normcase(os.path.abspath(os.path.expanduser(p)))
    for r in roots:
        rn = os.path.normcase(os.path.abspath(os.path.expanduser(r)))
        if pn == rn or pn.startswith(rn.rstrip(os.sep) + os.sep) or rn.startswith(pn.rstrip(os.sep) + os.sep):
            return True
    return False


def probe() -> dict:
    rep = {}
    rep["claude-code"] = [str(r / "projects") for r in claude_roots()]
    cx = []
    for h in homes():
        for sub in ("sessions", "archived_sessions"):
            if (h / ".codex" / sub).is_dir():
                cx.append(str(h / ".codex" / sub))
    if os.environ.get("CODEX_HOME"):
        cx.append(os.environ["CODEX_HOME"] + "/sessions")
    rep["codex"] = cx
    rep["cursor"] = [str(d / "globalStorage/state.vscdb") for d in cursor_user_dirs()]
    rep["cursor-agent"] = sorted({str(Path(p).parents[1]) for h in homes()
                                  for p in glob.glob(str(h / ".cursor/chats/*/*/store.db"))})
    ag = []
    for h in homes():
        for r in (".gemini/antigravity", ".gemini/antigravity-cli"):
            if (h / r / "brain").is_dir():
                ag.append(str(h / r / "brain"))
    rep["antigravity"] = ag
    return rep


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--days", type=float, default=7, help="look-back window in days (default 7)")
    ap.add_argument("--since", help="ISO date/time; overrides --days")
    ap.add_argument("--project", action="append", default=[], help="only sessions in/under this path (repeatable)")
    ap.add_argument("--profile-root", action="append", default=[], help="additional explicitly authorized user profile (repeatable)")
    ap.add_argument("--md", action="append", default=[], help="exported transcript file or folder (repeatable)")
    ap.add_argument("--sources", default="claude,codex,cursor,cursor-agent,antigravity,md",
                    help="comma list of sources to read")
    ap.add_argument("--out", default="resume-task-out", help="output folder")
    ap.add_argument("--grep", action="append", default=[], help="keep only sessions mentioning this text (repeatable, any match)")
    ap.add_argument("--turns", type=int, default=6, help="recent turns per session kept in candidates.json")
    ap.add_argument("--max-sessions", type=int, default=150, help="newest N sessions to keep")
    ap.add_argument("--include-subagents", action="store_true", help="also read Claude Code subagent logs")
    ap.add_argument("--no-git", action="store_true", help="skip git context for projects")
    ap.add_argument("--probe", action="store_true", help="only list which stores exist, then exit")
    a = ap.parse_args()
    PROFILE_ROOTS[:] = [Path(value).expanduser().resolve() for value in a.profile_root]
    if a.days < 0 or a.turns < 0 or a.max_sessions < 1:
        ap.error("days/turns must be nonnegative and max-sessions must be positive")

    if a.probe:
        print(json.dumps(probe(), indent=2))
        return

    since = parse_time(a.since) if a.since else datetime.now(UTC) - timedelta(days=a.days)
    if since is None:
        ap.error("--since must be a valid ISO date/time")
    src = {x.strip() for x in a.sources.split(",")}
    sessions: list[Session] = []
    errors: list[str] = []

    def run(name, fn):
        try:
            got = fn()
            sessions.extend(got)
            return len(got)
        except Exception as e:  # one broken store must not sink the rest
            errors.append(f"{name}: {type(e).__name__}: {e}")
            return 0

    counts = {}
    if "claude" in src:
        counts["claude-code"] = run("claude-code", lambda: [x for x in (parse_claude(p) for p in claude_files(since, a.include_subagents)) if x])
    if "codex" in src:
        counts["codex"] = run("codex", lambda: [x for x in (parse_codex(p) for p in codex_files(since)) if x])
    if "cursor" in src:
        counts["cursor"] = run("cursor", lambda: parse_cursor_ide(since))
    if "cursor-agent" in src:
        counts["cursor-agent"] = run("cursor-agent", lambda: parse_cursor_cli(since))
    if "antigravity" in src:
        counts["antigravity"] = run("antigravity", lambda: parse_antigravity(since))
    if "md" in src:
        counts["md-exports"] = run("md", lambda: [x for x in (parse_md(f, pr) for f, pr in md_files(a.md, a.project, since, True)) if x])

    for s in sessions:
        s.project = repo_root(s.project)
    if "md" in src:
        known = {s.project for s in sessions if s.project and os.path.isdir(s.project)} - {
            str(Path(x).expanduser().resolve()) for x in a.project}
        extra = [x for x in (parse_md(f, pr) for f, pr in md_files([], sorted(known), since, False)) if x]
        sessions.extend(extra)
        counts["md-exports"] = counts.get("md-exports", 0) + len(extra)

    sessions = [s for s in sessions if under(s.project, a.project) or (a.md and s.tool.endswith(("export", "specstory", ")")))]
    sessions = [s for s in sessions if not s.updated or s.updated >= since or s.tool.endswith(("export", ")"))]
    if a.grep:
        pats = [g.lower() for g in a.grep]
        def hit(s):
            blob = " ".join([s.title or "", s.project or ""] + [t.text for t in s.turns] +
                            [t for t, _ in s.todos] + [m for _, m in s.plan_docs]).lower()
            return any(p in blob for p in pats)
        sessions = [s for s in sessions if hit(s)]
    sessions.sort(key=lambda s: s.updated or datetime.fromtimestamp(0, UTC), reverse=True)
    sessions = sessions[: a.max_sessions]

    by_proj: dict[str, list[Session]] = defaultdict(list)
    for s in sessions:
        by_proj[project_key(s.project)].append(s)

    out = Path(a.out)
    resolved = out.resolve()
    protected = [home / directory for home in homes() for directory in (".claude", ".codex", ".cursor", ".gemini", "AppData/Roaming/Cursor/User", ".config/Cursor/User", "Library/Application Support/Cursor/User")]
    protected += [Path(os.environ[key]).expanduser() for key in ("CODEX_HOME", "CLAUDE_CONFIG_DIR") if os.environ.get(key)]
    if any(resolved == directory.resolve() or resolved.is_relative_to(directory.resolve()) for directory in protected):
        ap.error("output must be outside harness configuration and session stores")
    outputs = {(out / name).resolve() for name in ("digest.md", "candidates.json")}
    if any(Path(session.path.split('#', 1)[0]).resolve() in outputs for session in sessions):
        ap.error("output may not overwrite an input transcript")
    out.mkdir(parents=True, exist_ok=True)
    data = {"generated": datetime.now(UTC).isoformat(), "since": since.isoformat(),
            "counts": counts, "errors": errors, "probe": probe(), "projects": []}
    md = [f"# resume-task digest", "",
          f"Window: since {since:%Y-%m-%d %H:%M} UTC · sessions: " +
          ", ".join(f"{k} {v}" for k, v in counts.items()) +
          (f" · errors: {'; '.join(errors)}" if errors else ""), "",
          "State per session: interrupted > awaiting-reply > mid-plan > open-items > loose-ends > looks-finished. "
          "Full last turns are in candidates.json (`recent_turns`). Candidate kinds, strongest first: unanswered-ask, interrupted, todo-* (agent's own plan), "
          "user-deferred (user said later/todo), plan-checkbox, *-section (\"Next steps\" etc.), agent-followup "
          "(heuristic sentence). `by` says who raised it. Later sessions and commits may already resolve "
          "earlier items — check before listing.", ""]
    total = 0
    order = sorted(by_proj.items(), key=lambda kv: max((s.updated or since) for s in kv[1]), reverse=True)
    for proj, ss in order:
        gc = None if a.no_git else git_context(proj if proj != "(unknown project)" else None, since)
        pj = {"project": proj, "git": gc, "sessions": []}
        md += [f"## {proj}", ""]
        if gc:
            md.append(f"git: branch `{gc['branch']}`, {gc['dirty_files']} uncommitted file(s), "
                      f"{gc['unpushed']} unpushed, {gc['stashes']} stash(es)")
            if gc["recent_commits"]:
                md.append("recent commits: " + " | ".join(one_line(c, 90) for c in gc["recent_commits"][:12]))
            md.append("")
        for s in ss:
            cands = extract(s)
            total += len(cands)
            last_u = next((t.text for t in reversed(s.turns) if t.role == "user"), "")
            last_a = next((t.text for t in reversed(s.turns) if t.role == "assistant"), "")
            sj = {"tool": s.tool, "id": s.sid, "path": s.path, "title": s.title,
                  "started": s.started.isoformat() if s.started else None,
                  "updated": s.updated.isoformat() if s.updated else None,
                  "turns": len(s.turns), "first_ask": one_line(s.first_ask()),
                  "last_user": one_line(last_u), "last_assistant_tail": tail(last_a),
                  "todos": [{"text": one_line(t, 200), "status": st} for t, st in s.todos],
                  "interrupted": s.interrupted, "note": s.note, "candidates": cands,
                  "state": thread_state(s, cands), "resume_cmd": resume_cmd(s),
                  "files_touched": s.files[-40:],
                  "recent_turns": [{"role": t.role, "ts": t.ts.isoformat() if t.ts else None,
                                    "text": one_line(t.text, 1200)} for t in s.turns[-a.turns:]]}
            pj["sessions"].append(sj)
            when = f"{s.updated:%Y-%m-%d %H:%M}" if s.updated else "?"
            md.append(f"### [{s.tool}] {one_line(s.title or s.first_ask(), 100)} — {when} · {len(s.turns)} turns · "
                      f"**{sj['state']}** · `{s.sid[:12]}`")
            md.append(f"- goal: {one_line(s.first_ask(), 220)}")
            if last_u and last_u != s.first_ask():
                md.append(f"- last user msg: {one_line(last_u, 220)}")
            if last_a:
                md.append(f"- agent's last words: {tail(last_a, 260)}")
            if s.todos:
                done = sum(1 for _, st in s.todos if str(st).lower() in DONE)
                md.append(f"- plan: {done}/{len(s.todos)} done")
            if s.files:
                shown = [os.path.relpath(f, proj) if os.path.isabs(proj) and f.startswith(proj) else f for f in s.files[-8:]]
                md.append(f"- files touched ({len(s.files)}): " + ", ".join(shown))
            if sj["resume_cmd"]:
                md.append(f"- resume: `{sj['resume_cmd']}`")
            if s.note:
                md.append(f"- note: {s.note}")
            for x in cands:
                md.append(f"  - **{x['kind']}** ({x['by']}): {x['text']}" + (f" _[{x['context']}]_" if x["context"] else ""))
            md.append("")
        data["projects"].append(pj)

    (out / "candidates.json").write_text(json.dumps(redact_report(data), indent=1, default=str), encoding="utf-8")
    (out / "digest.md").write_text(redact("\n".join(md)), encoding="utf-8")
    print(json.dumps({"digest": str(out / "digest.md"), "json": str(out / "candidates.json"),
                      "sessions": len(sessions), "projects": len(by_proj), "candidates": total,
                      "counts": counts, "errors": errors}, indent=1))


if __name__ == "__main__":
    main()
