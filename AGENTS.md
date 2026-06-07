# AGENTS.md — WinGuiMcp

Instructions for coding agents working in this repository.

## Overview

**WinGuiMcp** is a [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server that gives AI agents a Playwright-style interface for controlling the Windows desktop. Agents work from structured accessibility snapshots with stable element refs (e.g. `w1e42`), not pixel coordinates.

| Layer | Technology |
|-------|------------|
| Language | C# |
| Runtime | .NET 9 |
| UIA | FlaUI 5 (UIA3 primary, UIA2 fallback) |
| MCP | `ModelContextProtocol` SDK, stdio JSON-RPC 2.0 |
| Input | UIA control patterns first; P/Invoke `SendInput` fallback |

Stack rationale and alternatives: [`docs/stack.md`](docs/stack.md).

## Implementation status

**Phase 1 MVP is implemented** in [`src/WinGuiMcp/`](src/WinGuiMcp/): MCP host, all Phase 1 tools, UIA snapshot/refs, pattern actuation, WGC capture (BitBlt fallback), and gate tests under [`tests/WinGuiMcp.Tests/`](tests/WinGuiMcp.Tests/).

Gate status: [`.agent/PHASE1_GATE_RESULTS.md`](.agent/PHASE1_GATE_RESULTS.md) — automated coverage is **partial** for all six gate apps; optional steps exit early when UIA layout/locale differs.

```bash
dotnet build WinGuiMcp.sln
dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj
```

If the MCP server is running in Cursor, stop/reload it before building (locks `bin/` output).

## Documentation (read first)

**Do not invent tool names, JSON schemas, error codes, or module APIs.** Canonical specs:

| Topic | File |
|-------|------|
| Doc index + reading order | [`docs/README.md`](docs/README.md) |
| Authority map for agents | [`docs/CANONICAL_SOURCES.md`](docs/CANONICAL_SOURCES.md) |
| Executive spec | [`docs/SPEC.md`](docs/SPEC.md) |
| Stack (language, runtime, NuGet) | [`docs/stack.md`](docs/stack.md) |
| Architecture + modules | [`docs/architecture.md`](docs/architecture.md) |
| MCP tool reference | [`docs/tools.md`](docs/tools.md) |
| UEM / ref format | [`docs/data-model.md`](docs/data-model.md) |
| Phases + acceptance gates | [`docs/phases.md`](docs/phases.md) |
| Error codes | [`docs/errors.md`](docs/errors.md) |
| Security | [`docs/security.md`](docs/security.md) |
| Technical requirements | [`docs/requirements/README.md`](docs/requirements/README.md) |

**Background only (non-normative):** [`docs/research/`](docs/research/) — do not implement from these files.

## SDLC / agent-sdlc

This repo vendors **[agent-sdlc](https://github.com/synthet/agent-sdlc)**-style Cursor rules (`.cursor/rules/`), slash commands (`.cursor/commands/`), and project skills (`.cursor/skills/`). **This `AGENTS.md` file** is the source of truth for commands, layout, and boundaries.

**Inventory:** [`.agent/AGENT_INFRA_INVENTORY.md`](.agent/AGENT_INFRA_INVENTORY.md), [`.agent/SKILL_INVENTORY.md`](.agent/SKILL_INVENTORY.md)

### Slash commands

Type `/` in Cursor chat:

| Command | File | Purpose |
|---------|------|---------|
| `/spec` | [spec.md](.cursor/commands/spec.md) | Feature or change specification |
| `/plan` | [plan.md](.cursor/commands/plan.md) | Implementation plan after spec |
| `/implement` | [implement.md](.cursor/commands/implement.md) | Execute an approved plan |
| `/test-and-fix` | [test-and-fix.md](.cursor/commands/test-and-fix.md) | Run tests and repair failures |
| `/pr-ready` | [pr-ready.md](.cursor/commands/pr-ready.md) | Prepare merge-ready PR |
| `/release-notes` | [release-notes.md](.cursor/commands/release-notes.md) | User-facing changelog slice |
| `/implement-tool` | [implement-tool.md](.cursor/commands/implement-tool.md) | Implement one MCP tool from `docs/tools.md` |
| `/bootstrap-phase1` | [bootstrap-phase1.md](.cursor/commands/bootstrap-phase1.md) | Scaffold Phase 1 module layout |
| `/phase-gate` | [phase-gate.md](.cursor/commands/phase-gate.md) | Run Phase 1 acceptance gate flows |
| `/mcp-tool-review` | [mcp-tool-review.md](.cursor/commands/mcp-tool-review.md) | Review MCP tool changes for safety |

### Skills

| Skill | Trigger |
|-------|---------|
| `win-gui-mcp-tools` | MCP tool work, `/implement-tool` |
| `win-gui-mcp-implementation` | `/bootstrap-phase1`, `/implement` |
| `mcp-server-design` | `/mcp-tool-review`, MCP server changes |
| `security-review` | Security-sensitive changes |
| `commit-conventions` | `/pr-ready`, commit/PR messaging |

Details: [`.agent/SKILL_INVENTORY.md`](.agent/SKILL_INVENTORY.md)

### Subagents

| Agent | Purpose |
|-------|---------|
| `pr-ready-hygiene` | [`.cursor/agents/pr-ready-hygiene.md`](.cursor/agents/pr-ready-hygiene.md) — `dotnet build`, diff scan, merge-ready summary |

## Build and verify

From repo root:

```bash
dotnet build WinGuiMcp.sln
dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj
dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj
```

Quick reference: [`.agent/COMMANDS.md`](.agent/COMMANDS.md)

## MCP self-config (local dev)

1. `dotnet build WinGuiMcp.sln`
2. Copy [`.cursor/mcp.example.json`](.cursor/mcp.example.json) → **`.cursor/mcp.json`** (gitignored; never commit your local file).
3. Reload MCP in Cursor.

Server key: **`win-gui-mcp`** (stdio via `dotnet run`).

## Boundaries

- **Tool names and JSON schemas** — only from [`docs/tools.md`](docs/tools.md). Update the spec before adding or renaming tools.
- **Refs and UEM** — only from [`docs/data-model.md`](docs/data-model.md).
- **Errors** — typed codes and recovery hints from [`docs/errors.md`](docs/errors.md).
- **Phase scope** — Phase 1 gate apps pass before Phase 2 work ([`docs/phases.md`](docs/phases.md)).
- **Security** — respect UIPI, elevation, and deployment constraints ([`docs/security.md`](docs/security.md), [`.agent/SAFETY.md`](.agent/SAFETY.md)).
- **Restricted system apps** — agents never launch or actuate Control Panel, Windows Settings (`ms-settings:`), or other targets in [`.agent/SAFETY.md`](.agent/SAFETY.md#restricted-targets--agents-must-not-open-or-actuate).
- **Actuation** — UIA control patterns first; coordinate clicks only as documented fallback.

## Code style

- C# / .NET 9; match module names and layering in [`docs/architecture.md`](docs/architecture.md).
- FlaUI UIA3 primary; UIA2 fallback for legacy WinForms only.
- Small modules; dependency injection via `Microsoft.Extensions.Hosting`.
- Structured tool errors with codes from [`docs/errors.md`](docs/errors.md).

## Agent infra hub

- [`.agent/AGENT_INFRA_INVENTORY.md`](.agent/AGENT_INFRA_INVENTORY.md) — rules, commands, skills, agents
- [`.agent/COMMANDS.md`](.agent/COMMANDS.md) — operator command quick reference
- [`.agent/SAFETY.md`](.agent/SAFETY.md) — Windows UI automation guardrails
