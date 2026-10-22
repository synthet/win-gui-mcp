---
name: framework-implement-tool
description: "Explicit framework workflow: implement-tool"
disable-model-invocation: true
---

# /implement-tool — Implement one MCP tool

Use when adding or updating a single MCP tool handler from the normative spec.

## Inputs

- Tool name (must exist in [docs/tools.md](../../docs/tools.md)).
- Target phase ([docs/phases.md](../../docs/phases.md)) — default Phase 1.
- **AGENTS.md** for build commands.

## Steps

1. Read the tool definition in [docs/tools.md](../../docs/tools.md): parameters, return shape, error cases.
2. Identify the owning module in [docs/architecture.md](../../docs/architecture.md) (`Tools/`, `Session/`, `Perception/`, `Action/`, etc.).
3. Register the tool with the MCP host (`ModelContextProtocol` SDK).
4. Implement handler using UIA patterns first; coordinate fallback only where spec allows.
5. Return typed errors from [docs/errors.md](../../docs/errors.md) with recovery hints.
6. Run `dotnet build WinGuiMcp.sln`.

## Output

- Files changed with brief rationale.
- Manual verification steps (which app/window to test).
- Any spec gaps found (do not invent — flag for human update).

## Done when

- Tool name, input/output JSON, and error codes match [docs/tools.md](../../docs/tools.md) and [docs/errors.md](../../docs/errors.md).
- Build passes.

## Related

- Skill: `win-gui-mcp-tools`
- Docs: [docs/data-model.md](../../docs/data-model.md) for ref/UEM semantics
