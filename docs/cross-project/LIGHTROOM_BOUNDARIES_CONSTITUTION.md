# Cross-project constitution: WinGuiMcp ↔ lightroom-mcp (Lightroom Desktop)

**Status:** Normative for agents working across both repositories. Modelled on [DXO_BOUNDARIES_CONSTITUTION.md](DXO_BOUNDARIES_CONSTITUTION.md).
**Parties:** `win-gui-mcp` (generic Windows UIA MCP and `WinGuiMcp.Core`) and `lightroom-mcp` (local checkout `<projects-root>\lightroom-mcp`: an Adobe **Lightroom Desktop (cloud)** domain server).
**Not a party:** `lightroom-classic-mcp` (`<projects-root>\lightroom-classic-mcp`) automates **Lightroom Classic** through a Lua plug-in. It is a different product and a different repo.

## 1. Charters

| Concern | win-gui-mcp | lightroom-mcp |
|---|---|---|
| Mission | Generic UIA, input, capture and session layer; scenario engine | Lightroom Desktop develop and mask control, persistence proof, Lightroom scenarios |
| MCP server key | `win-gui-mcp` | `lightroom-mcp` |
| Tool names | [tools.md](../tools.md) names only | `lr_*` only (`lr_run_scenario`, not `run_scenario`) |
| Live control channel | UIA patterns, SendInput fallback | Adobe Lightroom Controller API: WebSocket `127.0.0.1:<port>` from `%LOCALAPPDATA%\Adobe\Lightroom CC\Connections\connections.json`, user-approved registration |
| Persistence proof | n/a | Saved-settings XMP under `Data\<catalog>\settings\` and a read-only catalog copy |

## 2. How lightroom-mcp uses WinGuiMcp.Core

- **By reference.** `ProjectReference` to `..\..\..\win-gui-mcp\src\WinGuiMcp.Core\WinGuiMcp.Core.csproj` from a sibling checkout. CI checks out both repos side by side.
- **MUST NOT** fork or vendor Core source, or copy win-gui tool names into its own MCP surface.
- **MAY** register `lr.*` scenario steps in its own process, and expose the engine under `lr_*` tool names.
- **MUST** ship its own PerMonitorV2 `app.manifest`.

## 3. Layer rule

Prefer the lowest correct layer. Sliders and masks go through the Controller API. Saved state is proved from the XMP and catalog. Only UI with no API (for example the Escape-to-Grid save trip, or brush size and feather) uses Core pixel or UIA actions, behind `guard:` checks.

## 4. Change management

- A Core change that lightroom-mcp needs is made in win-gui-mcp first. The tool-surface golden test must stay green, or be regenerated with a matching tools.md change.
- A new `lr_*` tool is added to `lightroom-mcp/docs/tools.md` before code.
- Changes to this file need a matching note in `lightroom-mcp/AGENTS.md`.
