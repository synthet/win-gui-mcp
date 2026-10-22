---
name: win-gui-mcp-implementation
description: Scaffold and implement WinGuiMcp Phase 1 modules per docs/architecture.md — MCP host, session/refs, UIA perception, action layer.
---

# WinGuiMcp implementation

## Use this skill when

- Starting Phase 1 from stub code
- User runs `/bootstrap-phase1` or `/implement`
- Adding new modules under `src/WinGuiMcp/`

## Authority

- Module layout: [docs/architecture.md](../../../docs/architecture.md)
- Phase 1 scope: [docs/phases.md](../../../docs/phases.md)
- Stack: .NET 9, FlaUI 5, `ModelContextProtocol` SDK, `Microsoft.Extensions.Hosting`

## Module map

| Folder | Responsibility |
|--------|----------------|
| `Transport/` | MCP stdio host, JSON-RPC, tool registration |
| `Tools/` | Tool handler dispatch |
| `Session/` | `SnapshotSession`, `RefTable`, invalidation |
| `Perception/` | `UiaSnapshotBuilder`, UIA tree walk, pruning |
| `Action/` | `PatternExecutor`, `InputSimulator` (SendInput) |
| `Capture/` | `WindowCapture` — WGC with BitBlt fallback |
| `Platform/` | `DpiManager`, Win32 interop |

## Bootstrap procedure

1. Create folders and minimal types per architecture doc.
2. Replace Hello World `Program.cs` with hosted MCP server entry.
3. Register Phase 1 tools (stubs OK initially).
4. Verify `dotnet build` and `dotnet run` starts stdio MCP.
5. Implement tools in order: `list_windows` → `focus_window` → `snapshot` → `click` → remaining.

## Implementation discipline

- Match existing C# style in touched files.
- One tool per focused PR when possible.
- Update normative docs before changing public tool contracts.

## Done criteria

- Layout matches `docs/architecture.md`.
- Phase 1 tools registered and building.
- MCP server listable in Cursor after `mcp.example.json` → `mcp.json` copy.

## Related

- Command: `/bootstrap-phase1`, `/implement`, `/implement-tool`
- Skill: `win-gui-mcp-tools`, `mcp-server-design`
