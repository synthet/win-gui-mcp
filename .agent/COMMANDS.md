# Command quick reference — win-gui-mcp

From [AGENTS.md](../AGENTS.md). Run from **repo root** unless noted.

## Setup

```bash
dotnet build WinGuiMcp.sln
```

## MCP local dev

1. `dotnet build WinGuiMcp.sln`
2. Copy `.cursor/mcp.example.json` → `.cursor/mcp.json`
3. Reload MCP in Cursor (server key: `win-gui-mcp`)

## Run server (stdio)

```bash
dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj
```

## Verify (before PR)

```bash
dotnet build WinGuiMcp.sln
dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj
```

Gate tests launch real Windows apps (Calculator, Notepad, Explorer, Settings, samples). Run on an interactive desktop session; suite uses `[Collection("Phase1Gate")]` with no parallelization.

If `dotnet build` fails because `WinGuiMcp.dll` is locked, reload/stop the MCP server in Cursor first.

## Agent slash commands

Type `/` in Cursor chat. Full list: [AGENTS.md](../AGENTS.md#slash-commands).

**SDLC:** `/spec`, `/plan`, `/implement`, `/test-and-fix`, `/pr-ready`, `/release-notes`

**Domain:** `/implement-tool`, `/bootstrap-phase1`, `/phase-gate`, `/mcp-tool-review`

## Docs

- [docs/README.md](../docs/README.md) — spec reading order
- [docs/CANONICAL_SOURCES.md](../docs/CANONICAL_SOURCES.md) — authority map

## Safety

- [.agent/SAFETY.md](SAFETY.md)
- [docs/security.md](../docs/security.md)
