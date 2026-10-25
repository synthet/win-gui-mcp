# Agent memory — Cursor / Claude usage

Local, auditable memory for AI coding sessions. Does **not** modify model weights; only external markdown/YAML artifacts.

## Session start prompt

Copy into a new chat or rely on the `agent-memory` rule:

> Before modifying code, read `.agent-memory/memory.md` if present. Treat it as helpful but not infallible. Prefer current repository evidence over memory if they conflict. If you discover durable project facts, user preferences, recurring issues, or successful patterns, add them to the session log instead of editing memory directly.

## 1. Log a session

After significant work (or end of day):

```powershell
python scripts/agent-memory/log_session.py --summary "Short task title" --outcome "What shipped"
```

Add memory candidates (repeat `--candidate`):

```text
--candidate "text|category|confidence"
```

Example:

```powershell
python scripts/agent-memory/log_session.py `
  --summary "Agent memory CLI" `
  --outcome "Tests pass" `
  --candidate "Dream never overwrites memory.md|working_rule|high" `
  --file "scripts/agent_memory/consolidate.py"
```

Interactive mode: `python scripts/agent-memory/log_session.py`

From JSON stdin:

```powershell
'{"task_summary":"x","memory_candidates":[{"text":"y","category":"working_rule","confidence":"high"}]}' | python scripts/agent-memory/log_session.py --stdin
```

Raw logs land in `.agent-memory/raw-sessions/` (gitignored by default).

## 2. Dream (consolidate)

Produces a **proposal** only — does not change `memory.md`:

```powershell
python scripts/agent-memory/dream.py
```

Outputs:

- `.agent-memory/dreams/YYYY-MM-DD-HHMM.md` — proposed memory
- `.agent-memory/dreams/YYYY-MM-DD-HHMM-changelog.md` — added/updated/removed/uncertain

Review the changelog and diff against current `memory.md`.

Optional: `--repo-root` for tests; `--max-sessions N` overrides config.

## 3. Promote (after human review)

```powershell
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/2026-06-03-1430.md
```

- Archives previous `memory.md` under `.agent-memory/dreams/archive/`
- Replaces `memory.md` with the dream body (strips YAML front matter)
- Requires matching `-changelog.md` unless `--force`

## 4. Load memory for a new session

```powershell
python scripts/agent-memory/context.py
```

Prints a compact block (respects `max_context_chars` in `config.json`). Paste into chat or `@.agent-memory/memory.md`.

## Safety

- Scripts **reject** content that matches secret patterns (API keys, Bearer tokens, PEM, `*_PASSWORD=`, etc.).
- Do not log `secrets.json`, `.env`, passwords, or machine-specific paths unless necessary (prefer redaction).
- Never edit `memory.md` by hand during a session; use log → dream → promote.

## Limitations

- Consolidation merges structured `memory_candidates` deterministically (no LLM inside scripts).
- `raw-sessions/` and `dreams/` are gitignored; only the approved `memory.md` and docs are shared by default.
- This is **personal/project** memory. For team-shared facts, curate `memory.md`; keep machine-local notes out of git.

## Reference

- Format: [schema.md](schema.md)
- Config: [config.json](config.json)
- Slash commands: `/log-session`, `/dream-memory`, `/promote-memory`, `/memory-context`
- Skill: `.claude/skills/agent-memory/SKILL.md` (mirrored to `.cursor/skills/agent-memory/SKILL.md`)
