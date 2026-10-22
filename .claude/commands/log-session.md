---
capability: "log-session agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/log-session`. Mirror: `.cursor/commands/log-session.md`.

# /log-session — Log agent session to raw memory

Record what was done in this session for later consolidation. Does **not** update approved `memory.md`.

## Steps

1. Gather: task summary, files touched, commands/tests run, decisions, blockers, outcome.
2. Add `--candidate "text|category|confidence"` for durable learnings (see `.agent-memory/schema.md`).
3. Run from repo root:

```powershell
python scripts/agent-memory/log_session.py --summary "<one line>" --outcome "<result>" --candidate "..."
```

Repeat `--file`, `--command`, `--test`, `--decision`, `--error` as needed.

## Done when

- CLI prints path to new `.agent-memory/raw-sessions/*.yaml`
- No secret patterns in logged content

## Skill

`.claude/skills/agent-memory/SKILL.md`
