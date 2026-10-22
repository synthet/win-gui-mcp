---
capability: "analyze agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/analyze`. When customizing, keep in sync with `.cursor/commands/analyze.md`.

# /analyze — Cross-artifact consistency and coverage check

Use after `/tasks` and before `/implement` for non-trivial work. This Spec Kit-inspired quality gate checks that the spec, plan, and task list agree before any implementation begins.

## Inputs

- Spec with stable `AC-n` acceptance criteria.
- Implementation plan.
- Task list with `T-n` task IDs.
- Relevant project rules, package boundaries, and security requirements.

## Output

1. **Coverage matrix** — Each `AC-n` mapped to plan sections, `T-n` tasks, and verification commands.
2. **Consistency findings** — Contradictions, missing decisions, or scope drift across spec/plan/tasks.
3. **Constitution findings** — Violations of `AGENTS.md`, `.agent/SAFETY.md`, package boundaries, generated-file rules, or security policy.
4. **Task readiness findings** — Tasks that are too vague, unordered, untestable, or missing dependencies.
5. **Recommended fixes** — Minimal edits to spec, plan, or tasks before `/implement`.
6. **Implementation readiness** — `Ready`, `Ready with warnings`, or `Blocked`.

## Finding severity

- **Blocker:** Implementation would likely build the wrong thing, violate a hard rule, or lack a verification path.
- **Warning:** Implementation can proceed, but risk or follow-up should be documented.
- **Info:** Useful cleanup that should not block.

## Done when

- Every `AC-n` has a planned verification path.
- Every `T-n` maps to at least one `AC-n` or is clearly marked as setup/docs/validation.
- Scope additions are moved to out-of-scope follow-up or reflected in the spec.
- Blockers are fixed before `/implement` begins.
