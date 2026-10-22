---
name: framework-promote-memory
description: "Explicit framework workflow: promote-memory"
disable-model-invocation: true
---

> **Claude Code:** Same intent as Cursor `/promote-memory`. Mirror: `.cursor/commands/promote-memory.md`.

# /promote-memory — Approve dream into active memory

After human review of a dream proposal, promote it to `.agent-memory/memory.md`.

## Inputs

- Path to reviewed dream file (from `/dream-memory` output).

## Steps

```powershell
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/<timestamp>.md
```

Requires matching `*-changelog.md` unless user confirms `--force`.

## Done when

- `memory.md` updated
- Previous memory archived under `.agent-memory/dreams/archive/`

## Skill

`.claude/skills/agent-memory/SKILL.md`
