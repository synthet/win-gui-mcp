---
trigger: always_on
description: "WinGuiMcp product overview, architecture, and Phase 1 scope"
---

# WinGuiMcp overview

## Product

WinGuiMcp is an MCP server for **Playwright-style Windows desktop automation**:

- Agents receive **accessibility snapshots** with stable element refs (`w1e42`).
- Actions use **UIA control patterns** first; SendInput at bbox center is fallback only.
- Primary perception is **structured text trees**, not screenshots.

## Stack

- .NET 9, FlaUI 5 (UIA3), `ModelContextProtocol` SDK, stdio transport
- Modules per [docs/architecture.md](../../docs/architecture.md): `Transport/`, `Tools/`, `Session/`, `Perception/`, `Action/`, `Capture/`, `Platform/`

## Phase 1 scope (current target)

Tools: `list_windows`, `launch_app`, `focus_window`, `snapshot`, `find_element`, `get_properties`, `click`, `type_text`, `set_value`, `hotkey`, `screenshot`, `wait_for`

Acceptance: multi-step flows on Calculator, Notepad, File Explorer, Settings, WinForms/WPF samples — see [docs/phases.md](../../docs/phases.md).

**Out of scope for Phase 1:** vision, scroll/drag, UIA events, HTTP transport.

## Agent entry points

- [AGENTS.md](../../AGENTS.md) — commands, build, MCP setup
- [docs/README.md](../../docs/README.md) — spec reading order
- `/bootstrap-phase1` — scaffold modules when code is still stub-only
- `/implement-tool` — add one tool from [docs/tools.md](../../docs/tools.md)
