---
name: win-gui-mcp-tools
description: Implement and verify WinGuiMcp MCP tools from docs/tools.md — UIA-patterns-first, snapshot+ref workflow, typed errors.
---

# WinGuiMcp tools

## Use this skill when

- Implementing or changing an MCP tool handler
- User runs `/implement-tool` or `/phase-gate`
- Debugging tool behavior against the normative spec

## Authority

- Tool names, parameters, returns: [docs/tools.md](../../../docs/tools.md)
- Ref format and UEM: [docs/data-model.md](../../../docs/data-model.md)
- Error codes: [docs/errors.md](../../../docs/errors.md)
- Phase scope: [docs/phases.md](../../../docs/phases.md)

## Procedure

1. Read the tool entry in `docs/tools.md` before writing code.
2. Obtain element refs from `snapshot` or `find_element` — never invent refs.
3. Prefer UIA control patterns (`Invoke`, `Value`, `Toggle`, etc.) via FlaUI UIA3.
4. Use SendInput at bbox center only when patterns unavailable (document why).
5. Return structured errors with recovery hints from `docs/errors.md`.
6. Run `dotnet build WinGuiMcp.sln` after changes.

## Phase 1 tool catalog

Navigation: `list_windows`, `launch_app`, `focus_window`

Inspection: `snapshot`, `find_element`, `get_properties`

Action: `click`, `type_text`, `set_value`, `hotkey`

Utility: `screenshot`, `wait_for`

## Guardrails

- Do not add tools not listed in `docs/tools.md` without spec update.
- Do not expose raw pixel coordinates to agents as primary API.
- Bboxes stay server-side; agents receive text trees with refs.

## Done criteria

- Tool behavior matches `docs/tools.md` JSON schemas.
- Phase-gate flows pass without coordinate-first clicks where patterns exist.

## Related

- Command: `/implement-tool`, `/phase-gate`
- Skill: `win-gui-mcp-implementation`, `mcp-server-design`
