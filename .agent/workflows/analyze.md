# /analyze — Cross-artifact consistency and coverage check

Use after `/tasks` and before `/implement` for non-trivial work. This Spec Kit-inspired gate checks that the spec, plan, and task list agree before any code changes begin.

## Inputs

- Spec with stable `AC-n` acceptance criteria.
- Implementation plan.
- Task list with `T-n` task IDs.
- Relevant project rules, package boundaries, and security requirements.

## Steps

1. Build a coverage matrix mapping every `AC-n` to plan sections, `T-n` tasks, and verification commands.
2. Flag contradictions, missing decisions, and scope drift across spec, plan, and tasks.
3. Check for violations of `AGENTS.md`, `.agent/SAFETY.md`, package boundaries, generated-file rules, and security policy.
4. Identify tasks that are too vague, unordered, untestable, or missing dependencies.
5. Recommend minimal artifact edits before `/implement`.
6. Mark implementation readiness as `Ready`, `Ready with warnings`, or `Blocked`.

## Done when

- Every `AC-n` has a planned verification path.
- Every `T-n` maps to at least one `AC-n` or is marked setup/docs/validation.
- Scope additions are moved to out-of-scope follow-up or reflected in the spec.
- Blockers are fixed before `/implement` begins.
