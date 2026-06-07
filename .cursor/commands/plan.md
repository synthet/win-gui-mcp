# /plan — Implementation plan

Use after a spec exists (or for small tasks, a verbal agreement). Prefer **plan mode** or explicit user approval before large edits.

## Inputs

- Approved spec or tight task description.
- Relevant files the user pointed at.
- **AGENTS.md** for build commands and module layout.

## Output

1. **Goal** — What "done" means.
2. **Files / areas to touch** — Paths or components per [docs/architecture.md](../../docs/architecture.md).
3. **Approach** — Steps in order; call out risky changes.
4. **Contracts** — For MCP tools: link [docs/tools.md](../../docs/tools.md) schemas and [docs/errors.md](../../docs/errors.md) codes.
5. **Tests** — `dotnet build`; future `dotnet test`; manual phase-gate flows if UI tools change.
6. **Rollback / flags** — If feature-flagged or migratory.

## Done when

- Another developer could execute the plan without guessing.
- Test plan matches project conventions in AGENTS.md.

## Note

If the user has not approved implementation, **do not** apply code changes until they confirm.

## Related

- Skill: `win-gui-mcp-implementation`
- Docs: [docs/architecture.md](../../docs/architecture.md), [docs/phases.md](../../docs/phases.md)
