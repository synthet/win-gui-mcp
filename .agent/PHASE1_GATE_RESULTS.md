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

## Manual MCP gate (2026-06-07, run 2)

Run via live `win-gui-mcp` MCP tools (refs + UIA patterns only; no coordinate clicks). Settings skipped per [SAFETY.md](SAFETY.md).

| App | Pass | Notes / error codes |
|-----|------|---------------------|
| Calculator | **yes** | 2+3= → `wait_for` text-visible `5` (168 ms). All clicks `InvokePattern`. Initial `FocusFailed`; window still focused in snapshot. |
| Notepad | **partial** | `ValuePattern` type into Document ref → Ctrl+S → Save As dialog → `set_value` filename + Invoke Save. Saved as garbled Desktop filename, not target temp path. |
| File Explorer | **partial** | Temp folder opened; `gate-rename-me` in list; file click `InvokePattern`. F2 / Rename toolbar / `set_value` → `PatternNotSupported` or `InternalError` (0x80040201); file unchanged on disk. |
| WinForms sample | **yes** | `set_value` InputTextBox → click → `wait_for` `Output: Phase gate MCP`. |
| WPF sample | **yes** | Alt+F/O → dialog `set_value` + OK → snapshot `Dialog result: Gate OK`. |

**Gate result:** **partial pass** (3/5 full, 2/5 partial).

**Remediation (addressed 2026-06-07):**

1. `TypeText` prefers `ValuePattern` when supported; `SetValue` focuses first; `Click` tolerates missing `IsOffscreen`.
2. Win11 Notepad: use **`Ctrl+Shift+S`** (real `#32770` Save As), not `Ctrl+S` (inline rename). Avoid `"Save"` title match — it hits Notepad tabs.
3. Save dialog: target `FileNameControlHost` / `Имя файла:`; click **Сохранить** by name (not `automationId` `1` — collides with list items).
4. Explorer rename: try ribbon **Rename** / **Переименовать** before F2; `Ctrl+A` SendInput fallback.
5. **Screenshot debug:** gate tests write PNGs to `%TEMP%\\WinGuiMcpGateDebug` via `GateTestHelpers.TrySaveDebugScreenshot` on failure steps (Notepad save, Explorer rename).

Re-run `/phase-gate` after MCP reload to confirm manual MCP pass.

**Remediation (open):**

1. Notepad: launch fresh instance (or close prior tabs); navigate Save As to temp folder via address bar ref before `set_value`.
2. Explorer rename: Phase 2 `select` + inline-edit detection; F2 rename mode may need event-driven `wait_for` before typing.

## Manual MCP gate (2026-06-07, run 1 — superseded)

## Sample app paths

- WinForms: `samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj`
- WPF: `samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj`

## MCP setup

1. `dotnet build WinGuiMcp.sln`
2. Copy `.cursor/mcp.example.json` → `.cursor/mcp.json`
3. Reload MCP in Cursor (server key: `win-gui-mcp`)

## Informational metrics (measured in tests)

- Calculator tree token target: < 8k tokens (char-length heuristic in test)
