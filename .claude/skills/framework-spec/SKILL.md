---
name: framework-spec
description: "Explicit framework workflow: spec"
disable-model-invocation: true
---

# /spec — Feature or change specification

Use when starting non-trivial work. Produce a **spec** the team can review before implementation.

## Inputs

- Problem statement or feature request (from user message or linked issue).
- Constraints: time, scope, tech stack (see **AGENTS.md**).
- For MCP tools: reference [docs/tools.md](../../docs/tools.md) and [docs/phases.md](../../docs/phases.md).

## Output

1. **Summary** — One paragraph.
2. **Users / stakeholders** — Who benefits.
3. **Non-goals** — What is explicitly out of scope.
4. **User stories** — Short "As a … I want … so that …" bullets.
5. **Acceptance criteria** — Checkable bullets (Given/When/Then or "When X, then Y").
6. **Open questions** — Unknowns and decisions needed from humans.

## Done when

- Criteria are testable without interpreting intent.
- Non-goals prevent scope creep.
- MCP tool changes identify which [docs/tools.md](../../docs/tools.md) entries are affected.

## Related

- Skill: none (use `/plan` next)
- Docs: [docs/CANONICAL_SOURCES.md](../../docs/CANONICAL_SOURCES.md)
