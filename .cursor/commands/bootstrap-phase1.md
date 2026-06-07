# /bootstrap-phase1 — Scaffold Phase 1 module layout

Use when `src/WinGuiMcp/` is still stub-only and Phase 1 implementation is starting.

## Inputs

- [docs/architecture.md](../../docs/architecture.md) — proposed module layout.
- [docs/phases.md](../../docs/phases.md) — Phase 1 tools and acceptance gates.
- **AGENTS.md** for stack and boundaries.

## Steps

1. Create folder structure under `src/WinGuiMcp/`:
   - `Transport/` — MCP stdio host, tool registration
   - `Tools/` — tool handler dispatch
   - `Session/` — `SnapshotSession`, `RefTable`
   - `Perception/` — `UiaSnapshotBuilder`, tree walk
   - `Action/` — `PatternExecutor`, `InputSimulator`
   - `Capture/` — `WindowCapture` (WGC + BitBlt fallback)
   - `Platform/` — `DpiManager`, Win32 interop
2. Wire `Program.cs` to `Microsoft.Extensions.Hosting` + MCP transport (no Hello World).
3. Add stub tool handlers for Phase 1 tools from [docs/tools.md](../../docs/tools.md) (throw `NotImplementedException` or return structured "not implemented" until filled in).
4. Ensure `dotnet build WinGuiMcp.sln` passes.
5. Update [`.cursor/mcp.example.json`](../mcp.example.json) if entrypoint changes.

## Output

- Module map: folder → responsibility.
- List of stub tools registered.
- Recommended `/implement-tool` order (suggest: `list_windows` → `snapshot` → `click`).

## Done when

- Solution builds.
- MCP server starts via `dotnet run` and exposes tool list (stubs OK).
- Layout matches [docs/architecture.md](../../docs/architecture.md).

## Related

- Skill: `win-gui-mcp-implementation`
- Command: `/implement-tool` for each tool
