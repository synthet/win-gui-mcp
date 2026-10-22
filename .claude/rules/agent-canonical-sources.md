# Agent canonical sources

## Spec authority

Do **not** invent MCP tool names, JSON schemas, error codes, ref formats, or module APIs. Use [docs/CANONICAL_SOURCES.md](../../docs/CANONICAL_SOURCES.md) and the normative specs it lists:

- [docs/stack.md](../../docs/stack.md) — language, runtime, NuGet baseline
- [docs/tools.md](../../docs/tools.md) — tool definitions
- [docs/data-model.md](../../docs/data-model.md) — UEM and refs
- [docs/architecture.md](../../docs/architecture.md) — module layout
- [docs/errors.md](../../docs/errors.md) — typed errors
- [docs/phases.md](../../docs/phases.md) — phase scope
- [docs/security.md](../../docs/security.md) — deployment constraints

## Research folder

[docs/research/](../../docs/research/) is **background only**. Do not implement from research files directly.

## Commands (verify frequently)

- `dotnet build WinGuiMcp.sln`
- `dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj`

## Agent infra index

- [AGENTS.md](../../AGENTS.md)
- [.agent/AGENT_INFRA_INVENTORY.md](../../.agent/AGENT_INFRA_INVENTORY.md)
- [.agent/COMMANDS.md](../../.agent/COMMANDS.md)
- [.agent/SAFETY.md](../../.agent/SAFETY.md)
