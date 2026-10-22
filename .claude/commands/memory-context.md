---
capability: "memory-context agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/memory-context`. Mirror: `.cursor/commands/memory-context.md`.

# /memory-context — Print memory for a new session

Output compact approved memory for paste or `@.agent-memory/memory.md`.

## Steps

```powershell
python scripts/agent-memory/context.py
```

Or instruct the user to reference `.agent-memory/memory.md` directly.

## Session-start prompt

> Before modifying code, read `.agent-memory/memory.md` if present. Treat it as helpful but not infallible. Prefer current repository evidence over memory if they conflict. If you discover durable project facts, user preferences, recurring issues, or successful patterns, add them to the session log instead of editing memory directly.

## Done when

- Memory block shown (truncated per `config.json` if needed)

## Skill

`.claude/skills/agent-memory/SKILL.md`
