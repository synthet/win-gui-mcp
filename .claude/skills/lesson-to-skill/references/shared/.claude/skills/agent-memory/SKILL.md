---
name: agent-memory
description: Use when starting or ending an agent session, loading or improving repo memory, consolidating dream outputs, promoting reviewed memory, or when the user mentions agent memory, dream-memory, log-session, promote-memory, context, recall, durable learnings, or memory.md.
capability: "agent-memory agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "python scripts/agent-memory/*.py"
output_schema: "Session YAML, dream proposal/changelog, or compact context block"
risk_class: high
---

# Agent memory (compiled harness)

Already compiled: deterministic CLIs under `scripts/agent-memory/`. This skill is a bootloader.

Local consolidation for **${PROJECT_NAME}** — external artifacts only (no model training).

## Invoke

```powershell
python scripts/agent-memory/log_session.py --summary "..." --outcome "..." --candidate "text|working_rule|high"
python scripts/agent-memory/dream.py
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/<timestamp>.md
python scripts/agent-memory/context.py
```

Slash commands: `/log-session`, `/dream-memory`, `/promote-memory`, `/memory-context`.

## When to use

| Moment | Action |
|--------|--------|
| Session start | `context.py` or read `.agent-memory/memory.md`; prefer repo evidence on conflict |
| Session end / milestone | `log_session.py` with durable candidates |
| Periodic | `dream.py` → human review changelog → `promote_dream.py` |

## LLM judgment slots

- Phrase durable `memory_candidates` (`text|category|confidence`).
- Review dream changelog **Uncertain** / **Stale** before promote.

Categories: `stable_fact`, `user_preference`, `working_rule`, `recurring_issue`, `successful_pattern`, `open_question`, `deprecated`. Confidence: `low`|`medium`|`high`.

## Safety (code-enforced)

- Scripts block common secret patterns on write.
- Never edit `memory.md` by hand during implementation — log → dream → promote.
- Do not log `.env`, `secrets.json`, tokens, or personal machine paths.

## Reference

- [`.agent-memory/CURSOR_USAGE.md`](../../../.agent-memory/CURSOR_USAGE.md)
- [`.agent-memory/schema.md`](../../../.agent-memory/schema.md)
- Detail table: [references/categories.md](references/categories.md)
