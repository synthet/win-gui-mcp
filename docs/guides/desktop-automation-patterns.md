# Desktop automation patterns (generic)

Reusable workflows for **any** Windows target app. These patterns were distilled from production-style integrations (including DxO PhotoLab) and belong in WinGuiMcp—not in domain MCP servers.

## Resolution order

1. **UIA** — `snapshot` → `find_element` / refs → `click`, `set_value`, `toggle`, patterns ([`architecture.md`](../architecture.md#control-stack-resolution-order)).
2. **Keyboard** — `hotkey` when shortcuts are documented and stable.
3. **OS waits** — `wait_for` with `process-exists`, `file-exists`, or `file-stable` when the signal is outside the UIA tree.
4. **SendInput fallback** — only when patterns are missing ([`tools.md`](../tools.md)).

## Launch and focus

```text
launch_app(path) → wait_for(handle, window-active) OR list_windows + focus_window
→ snapshot(handle)
```

Use `list_windows` with `processName` when multiple instances exist.

## Modal and banner dismissal

Poll `snapshot` for dialog `role` / name; `click` OK or `hotkey` `{ "keys": "escape" }`. Repeat until target chrome (e.g. main tabs) is visible. Domain recipes may name known banners; the mechanism is always generic UIA.

## Background processing

When a host app spawns a worker executable:

```json
{ "handle": 0, "condition": "process-exists", "target": "MyApp.Worker", "timeoutMs": 120000 }
```

When output is a file on disk (export, render, save):

```json
{ "handle": 0, "condition": "file-stable", "target": "D:\\Output\\image.jpg", "timeoutMs": 300000 }
```

Pair with `screenshot` or a second `snapshot` only if UI progress is exposed in UIA.

## Hybrid UI + files

| Step | Tool surface |
|------|----------------|
| Drive dialogs, tabs, sliders | WinGuiMcp UIA tools |
| Batch parameter files, sidecars | Domain MCP or scripts (see sibling repo) |
| Confirm render complete | `file-stable` or `process-exists` |

Cross-repo rules: [`cross-project/DXO_BOUNDARIES_CONSTITUTION.md`](../cross-project/DXO_BOUNDARIES_CONSTITUTION.md).

## Testing

- Gate flows: [`phases.md`](../phases.md).
- Unit-level OS waits: `tests/WinGuiMcp.Tests/OsWaitConditionsTests.cs`.
