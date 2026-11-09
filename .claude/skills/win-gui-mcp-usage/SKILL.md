---
name: win-gui-mcp-usage
description: Drive Windows desktop apps through the win-gui-mcp MCP server (mcp__win-gui-mcp__* tools) — launching apps, snapshots and refs, typing, hotkeys, screenshots, coordinate clicks, waits. Use whenever a task needs to see or operate a Windows GUI, verify a desktop app change in the running app, or read a win-gui-mcp screenshot. Not for implementing the server itself (use win-gui-mcp-tools).
---

# Using win-gui-mcp

Operator guide for agents that **call** win-gui-mcp. Every rule here comes from a failure seen in real agent transcripts (Claude Code, Cursor, Codex, Antigravity), October–November 2026.

Tool contracts: [docs/tools.md](../../../docs/tools.md). Errors: [docs/errors.md](../../../docs/errors.md). Restricted targets: [.agent/SAFETY.md](../../../.agent/SAFETY.md).

## The loop

1. **Find the window.** `launch_app` returns the main window's `handle`. For apps already running, use `list_windows` with a `filter`.
2. **Look.** Use `snapshot` (refs) when the app exposes UIA. Otherwise use `screenshot`.
3. **Act** by ref (`click`, `type_text`, `set_value`, `toggle`, `select`, `expand`). Use keys or coordinates only when no ref exists.
4. **Verify** with `get_text`, a fresh `snapshot`, or a screenshot. Do not assume `ok: true` means the app reacted.

Send **one input action at a time**, then verify. Don't batch `focus_window` + `hotkey` + `type_text` in one parallel tool-call block.

## Keyboard input

- **Always pass `handle`** to `type_text`, `hotkey` and `press_key`. The server then focuses that window and checks it holds the foreground before every key.
- **`FocusFailed` means nothing was sent.** Retry `focus_window` once. If it still fails, stop and tell the user something else is holding the foreground.
- **Without `handle` (or `ref`), keys go to whatever window is in front.** In past sessions that was the agent's own terminal or a console window.
- **Check `target` in the result.** It names the window that actually received the input. If it isn't the window you meant, stop typing.
- **Some apps drop `type_text` characters.** GPUI, games and some custom-drawn apps ignore the Unicode key events that `type_text` sends. If text doesn't appear, send the characters one at a time with `press_key` (letters work there).
- **Windows Terminal intercepts some chords** (e.g. `ctrl+shift+p`) before they reach the app running inside it. The server delivered them; the terminal consumed them.

## Windows

- **Confirm the window after launch.** `launch_app` skips zero-size helper windows and ranks real, titled windows first. Launcher stubs such as `wt.exe` and `explorer.exe` return the window that appeared after the launch. Still check `title`; if it looks wrong, run `list_windows` before sending input.
- **Apps that restore their last session** (Windows 11 Notepad, browsers, editors) may open old windows, unsaved buffers included, and no new blank window. Check the returned `title` before typing, and never use `clearFirst` on a window you didn't create: it replaces the whole document.
- **Untitled windows get the process name as their title** in `list_windows`. A window titled `synthet-commander` next to one titled `Synthet Commander` is usually the hidden helper; pick the real title.
- **Dialogs:** use `get_active_modal(handle)` to find a modal owned by the app. Save and Open pickers are separate windows; look them up with `list_windows`.
- **Popup and context menus** may not show up in `list_windows`. Take a full-screen screenshot (omit `handle`) to read them.
- **Tray icons** have no tool. Use a full-screen screenshot with a region crop on the notification area, then a coordinate click.

## Refs go stale

- Refs belong to the latest `snapshot` of their window. Moving or resizing the window, navigating, or opening a dialog can invalidate them, or leave them pointing at old coordinates.
- **`find_element` searches the last snapshot.** After a change, call `snapshot` again before `find_element`.
- **`VisibilityMismatch`** ("bbox center is outside the captured window frame" or "offscreen"): re-snapshot, scroll the element into view, or fall back to coordinates.
- **`ElementNotFound` on a name you can see:** the snapshot is pruned (`detail: "control"`, offscreen items excluded). Retry with `detail: "full"` or `includeOffscreen: true`, or scope with `rootRef`.

## Screenshots and coordinates

- **`screenshot` returns an image block plus metadata:** `width`, `height`, `origin` and `scale`.
- **Map an image pixel to a screen point:**

  ```
  x = origin.x + round(px / scale)
  y = origin.y + round(py / scale)
  ```

  Use that point for `click`, `drag`, `scroll` and `inspect_at`. All coordinates are physical pixels.
- **Keep images small:** `scale` 0.3–0.5. Use `regionX/Y/Width/Height` (relative to the captured image) to zoom into one area.
- **Check before you click.** Call `inspect_at(x, y)` on a computed point when a wrong click would matter.
- **Black frames:** GPU-rendered windows (GPUI, DirectX, some Electron builds) can capture black when covered by another window. Call `focus_window` first, or capture the full screen.
- **Little or no UIA in some apps** (GPUI, Electron canvases, Lightroom panels, custom WPF controls). Work with keyboard shortcuts plus screenshots, and use coordinate clicks verified with `inspect_at`.

## Waiting

- **`wait_for` condition names are hyphenated:** `element-exists`, `element-enabled`, `text-visible`, `element-gone`, `element-hidden`, `value-equals`, `window-active`, `window-closed`, `process-exists`, `file-exists`, `file-stable`.
- **These names don't exist and fail with `InvalidArgument`:** `window_title_contains`, `window_idle`.
- **OS-level conditions** (`process-*`, `file-*`) take `handle: 0` and a `target`.
- **Waiting for a title change** (e.g. a splash screen becoming the main window): there is no condition for it. Poll `list_windows` with a `filter`.
- **To wait for an export file:** use `file-stable` with `changedSinceUtc`, so an old file can't satisfy the wait.

## Errors → what to do

| Code | Meaning | Next step |
|------|---------|-----------|
| `FocusFailed` | Target couldn't take or keep the foreground; nothing more was sent | `focus_window` once, then ask the user |
| `WindowGone` | Handle closed or replaced (dialogs, relaunches) | `list_windows` for the new handle |
| `ElementNotFound` | Ref stale or locator not in the last snapshot | `snapshot` again |
| `VisibilityMismatch` | Element offscreen or bbox outside the window | re-snapshot or scroll; else coordinates |
| `Timeout` on `launch_app` | No window matched in time | `list_windows`; raise `waitForWindowMs` for slow apps |
| `InternalError` | Unmapped provider/COM failure | don't retry blindly; re-snapshot and inspect |
| `Timeout` from the UIA worker ("quarantined") | A provider call hung | the server must be restarted; tell the user |

## Safety

- **Never** launch or actuate Windows Settings (`ms-settings:`), Control Panel, or other restricted targets in [.agent/SAFETY.md](../../../.agent/SAFETY.md).
- **Warn the user before destructive UI actions** (delete, send, overwrite, "Yes to all", uninstall). Read dialogs before answering them.
- **Close what you opened** (`close_window`) when you're done. Leave the user's own windows as they were.
- **Respect the user's desktop.** If the user is working on the same machine, say what you're about to click or type.

## If the server is unavailable

The global registration runs the repo's Debug build. If the tools are missing or the connection closed, the exe may be locked by a rebuild or out of date. Tell the user. Don't fall back to ad-hoc PowerShell `SendKeys` without saying so.
