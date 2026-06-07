# Phase 1 gate results

Recorded: 2026-06-07 (automated gate suite)

## Build / test

| Check | Result |
|-------|--------|
| `dotnet build WinGuiMcp.sln` | pass |
| `dotnet test WinGuiMcp.sln` | pass (25 tests) |

## Gate flows

| App | Pass | Notes |
|-----|------|-------|
| Calculator | partial | launch smoke, optional 2+3=, token budget metric |
| Notepad | partial | type_text, screenshot, Save dialog, optional save-to-disk |
| File Explorer | partial | folder launch + snapshot; optional F2 rename |
| Settings | partial | `ms-settings:` launch + snapshot timing; optional System nav + toggle |
| WinForms sample | partial | `WinFormsGate_LaunchClickAndFill` |
| WPF sample | partial | `WpfGate_MenuDialogAndVerify` |

## Gate test classes

| Class | Tests |
|-------|-------|
| `Phase1GateTests` | Calculator launch, arithmetic, token budget |
| `Phase1NotepadGateTests` | type, screenshot, save dialog, save-to-disk |
| `Phase1ExplorerGateTests` | folder snapshot, rename via F2 |
| `Phase1SettingsGateTests` | launch timing, System page navigation, toggle switch |
| `Phase1SampleGateTests` | WinForms + WPF ref flows |

Optional gate steps exit early when UI layout/locale differs (Calculator standard mode, Explorer file list UIA, Settings nav labels).

## Sample app paths

- WinForms: `samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj`
- WPF: `samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj`

## MCP setup

1. `dotnet build WinGuiMcp.sln`
2. Copy `.cursor/mcp.example.json` → `.cursor/mcp.json`
3. Reload MCP in Cursor (server key: `win-gui-mcp`)

## Informational metrics (measured in tests)

- Settings snapshot latency target: < 2 s (soft assert < 5 s in CI)
- Calculator tree token target: < 8k tokens (char-length heuristic in test)
