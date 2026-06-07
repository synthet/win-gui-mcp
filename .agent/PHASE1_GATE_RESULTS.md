# Phase 1 gate results

Recorded: 2026-06-07 (automated gate suite + manual MCP gate)

## Build / test

| Check | Result |
|-------|--------|
| `dotnet build WinGuiMcp.sln` | pass |
| `dotnet test WinGuiMcp.sln` | pass (22 tests) |

## Gate flows (agent-automated)

| App | Pass | Notes |
|-----|------|-------|
| Calculator | partial | launch smoke, optional 2+3=, token budget metric |
| Notepad | partial | type_text, screenshot, Save dialog, optional save-to-disk |
| File Explorer | partial | folder launch + snapshot; optional F2 rename |
| WinForms sample | partial | `WinFormsGate_LaunchClickAndFill` |
| WPF sample | partial | `WpfGate_MenuDialogAndVerify` |

**Settings** — excluded from agent automation per [.agent/SAFETY.md](SAFETY.md#restricted-targets--agents-must-not-open-or-actuate). Manual operator verification on a VM only; see [docs/phases.md](../docs/phases.md) and [docs/requirements/gates/phase-1-settings.md](../docs/requirements/gates/phase-1-settings.md).

## Gate test classes

| Class | Tests |
|-------|-------|
| `Phase1GateTests` | Calculator launch, arithmetic, token budget |
| `Phase1NotepadGateTests` | type, screenshot, save dialog, save-to-disk |
| `Phase1ExplorerGateTests` | folder snapshot, rename via F2 |
| `Phase1SampleGateTests` | WinForms + WPF ref flows |

Optional gate steps exit early when UI layout/locale differs (Calculator standard mode, Explorer file list UIA).

## Manual MCP gate (2026-06-07)

Run via live `win-gui-mcp` MCP tools (refs + UIA patterns only; no coordinate clicks).

| App | Pass | Notes / error codes |
|-----|------|---------------------|
| Calculator | **yes** | Launch → snapshot → click 2+3= (InvokePattern) → result `Отображать как 5`. Tree well under 8k-token budget. |
| Notepad | **partial** | Save As dialog + `set_value` + Save; filename garbled (typing leaked into dialog field). |
| File Explorer | **partial** | Folder snapshot + file click; F2 rename did not update file on disk. |
| Settings | **skipped** | Agent policy: do not automate Settings / Control Panel ([SAFETY.md](SAFETY.md)). |
| WinForms sample | **yes** | `set_value` + click → `Output: Phase gate text`. |
| WPF sample | **yes** | Menu → dialog → `Dialog result: Gate OK`. |

**Manual gate result (agent scope):** **partial pass** (3/5 full, 2/5 partial; Settings N/A).

## Sample app paths

- WinForms: `samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj`
- WPF: `samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj`

## MCP setup

1. `dotnet build WinGuiMcp.sln`
2. Copy `.cursor/mcp.example.json` → `.cursor/mcp.json`
3. Reload MCP in Cursor (server key: `win-gui-mcp`)

## Informational metrics (measured in tests)

- Calculator tree token target: < 8k tokens (char-length heuristic in test)
