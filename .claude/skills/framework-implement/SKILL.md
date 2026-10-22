---
name: framework-implement
description: "Explicit framework workflow: implement"
disable-model-invocation: true
---

# /implement — Execute an approved plan

Use when the user has approved a plan or given a small, explicit task.

## Inputs

- Approved plan or task list.
- **AGENTS.md** for build commands.

## Steps

1. Implement in **minimal diffs**; match existing style and [docs/architecture.md](../../docs/architecture.md) module layout.
2. Add or update tests when behavior changes.
3. Run `dotnet build WinGuiMcp.sln`; fix failures.
4. Summarize what changed and where.

## Done when

- All agreed items are implemented.
- `dotnet build` passes (or failures are explained with next steps).

## Checklist

- [ ] No unrelated refactors
- [ ] No secrets committed
- [ ] Tool names/schemas match [docs/tools.md](../../docs/tools.md) if MCP surface changed
- [ ] Error codes match [docs/errors.md](../../docs/errors.md)

## Related

- Skill: `win-gui-mcp-implementation`
- Command: `/implement-tool` for single-tool work
