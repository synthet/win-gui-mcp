---
type: Research Note
title: Agent transcript findings — win-gui-mcp usage (Oct–Nov 2026)
description: What went wrong when coding agents drove Windows apps through win-gui-mcp, mined from Claude Code, Cursor, Codex and Antigravity transcripts, with root causes and fix status.
resource: docs/research/agent-transcript-findings-2026-11.md
tags: [research, transcripts, agents, ergonomics, okf]
timestamp: 2026-11-09T00:00:00Z
okf_version: 0.1
---

# Agent transcript findings — win-gui-mcp usage (Oct–Nov 2026)

Background note, not normative. Contracts live in [../tools.md](../tools.md); fixes landed through PR #6 (`fix/agent-ergonomics-from-transcripts`).

## Method

- **Sources:** local transcripts from Claude Code, Cursor, Codex and Antigravity that mention win-gui-mcp.
- **Runtime calls found:** 1,081 win-gui-mcp tool calls. 957 come from 15 Claude Code sessions in 8 projects: DxO PhotoLab automation, a GPUI file manager, Electron and Tauri galleries, tray apps, Chrome. Another 124 come from Cursor gate testing in this repo.
- **Cursor transcripts** don't store tool results, so they show calls only.
- **Codex and Antigravity** sessions contain no win-gui-mcp calls; they only discuss developing the server.
- **Typed errors:** 36 calls returned `ok: false`. Most real problems were calls that returned `ok: true` but did the wrong thing.

## Usage profile

- **Coordinate clicks dominate:** 282 of 321 clicks used `x`/`y`, only 39 used a ref.
  - The apps involved expose little UIA: GPUI, Electron canvases, Lightroom panels, DxO custom WPF controls and tray icons.
  - Coordinates are the main path in practice, not the fallback.
- **Untargeted keyboard input:**
  - 164 of 167 `type_text` calls had no target.
  - 36 of 92 `press_key` calls had no `handle`.
- **Screenshots overflowed:** 56 of 70 screenshots exceeded the client's tool-result size limit, and 10 more came back as raw base64 text the model could not view.

## Findings and status

| # | Problem | Root cause | Status |
|---|---------|------------|--------|
| 1 | Screenshots unusable; agents wrote base64 decoders | PNG embedded as base64 in the JSON text content | Fixed in PR #6: MCP `image` block plus `origin`/`scale` metadata |
| 2 | Keys typed into the wrong window (the agent's own terminal, a console) | `type_text` had no `handle`; no foreground check before SendInput | Fixed in PR #6: `handle`, foreground verification, `target` in results |
| 3 | `launch_app` returned a zero-size helper window (GPUI) | First window owned by the process id was taken in z-order; empty titles replaced by the process name | Fixed in PR #6: candidate ranking |
| 4 | `launch_app` timed out on every `wt.exe` launch | Launcher stub exits; the window belongs to another process | Fixed in PR #6: accepts a titled window that appeared after the launch once the launcher exits |
| 5 | Stale bboxes after a window move (`drag` → `VisibilityMismatch`) | `find_element` reuses the last snapshot; pre-input check uses the cached bbox | Open |
| 6 | Electron ref click → `InternalError` (COM HRESULT) | No SendInput fallback when a pattern call throws | Open |
| 7 | GPUI dropped `type_text` letters | Text sent only as `KEYEVENTF_UNICODE` packets | Open (skill documents the `press_key` workaround) |
| 8 | Generic `InternalError` for invalid UIA state or unsupported properties | Exceptions not mapped to typed codes; `inspect_at` reads properties without fallbacks | Open |
| 9 | Agents guessed `wait_for` conditions (`window_title_contains`, `window_idle`) | Tool descriptions are one line and list no valid values | Mitigated: server instructions and skill list the conditions |
| 10 | Context menus and tray icons not reachable | Popup menus missing from `list_windows`; no tray tool | Open (skill documents the workarounds) |
| 11 | Snapshot crashed on `Infinity` coordinates | Non-finite doubles in bounding boxes | Fixed earlier (named float literals allowed); values still serialize as strings |
| 12 | `get_properties` and `find_element` return no bbox | Envelope omits bounding rectangles | Open |

## Environment lessons (not server bugs)

- **Windows 11 Notepad session restore** reopens previous windows, including unsaved buffers, instead of a fresh window.
  - With restore on, `launch_app("notepad.exe")` can return a restored document.
  - A test that types with `clearFirst` would then replace that document.
  - Gate machines should set Notepad to "Start new session and discard unsaved changes".
- **Foreground lock:** while the desktop is in active use, Windows can refuse to bring a background-launched window to the front, so `focus_window` returns `FocusFailed`.
  - The Notepad gate tests fail this way on `origin/main` too.
  - Run desktop gates on an idle desktop or a dedicated VM.
- **xUnit runs gate test classes in parallel.** A launch heuristic that accepts "any new window" can pick another test's window.
- **The global MCP registration runs the repo's Debug build.**
  - Rebuilding while sessions are open fails because the exe is locked, and those sessions keep running the old server.
  - Building to a separate output directory (`-p:OutDir=...`) avoids the lock during development.

## Skill and instruction changes

- **Server instructions:** `ServerInstructions` text is sent at `initialize`. It covers the work loop, always passing `handle`, stale refs, screenshot coordinate mapping, `wait_for` names and safety rules.
- **Usage skill:** the project skill `win-gui-mcp-usage` (`.claude/skills/`) is the operator guide for agents that call the server. It is separate from the implementation skills (`win-gui-mcp-tools`, `win-gui-mcp-implementation`).
