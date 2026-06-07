# Phase 1 gate results

Recorded: 2026-06-06 (automated partial + build verification)

## Build / test

| Check | Result |
|-------|--------|
| `dotnet build WinGuiMcp.sln` | pass |
| `dotnet test WinGuiMcp.sln` | pass (10 tests) |

## Gate flows

| App | Pass | Notes |
|-----|------|-------|
| Calculator | partial | Automated test: launch + snapshot + list_windows + optional digit click; full 2+3= flow manual |
| Notepad | manual | Use `launch_app` `{ path: "notepad.exe" }` |
| File Explorer | manual | Focus existing Explorer or launch via shell |
| Settings | manual | `start ms-settings:` then snapshot |
| WinForms sample | manual | `dotnet run --project samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj` |
| WPF sample | manual | `dotnet run --project samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj` |

## Sample app paths

- WinForms: `samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj`
- WPF: `samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj`

## MCP setup

1. `dotnet build WinGuiMcp.sln`
2. Copy `.cursor/mcp.example.json` → `.cursor/mcp.json`
3. Reload MCP in Cursor (server key: `win-gui-mcp`)

## Informational metrics (to measure on manual gate)

- Settings snapshot latency target: < 2 s
- Calculator tree token target: < 8k tokens
