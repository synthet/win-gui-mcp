---
capability: "dream-memory agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/dream-memory`. Mirror: `.cursor/commands/dream-memory.md`.

# /dream-memory — Propose consolidated project memory

Merge recent session logs with current `memory.md` into a **proposal only**.

## Steps

1. Run:

```powershell
python scripts/agent-memory/dream.py
```

2. Open the printed `dreams/YYYY-MM-DD-HHMM.md` and matching `*-changelog.md`.
3. Summarize for the user: **Added**, **Uncertain / needs review**, and any **Removed**.
4. Do **not** promote without explicit user approval.

## Done when

- Dream and changelog exist under `.agent-memory/dreams/`
- `memory.md` is unchanged
- User has a clear review summary

## Skill

`.claude/skills/agent-memory/SKILL.md`
