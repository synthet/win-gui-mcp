# MCP tool reference

Tool responses retain their readable JSON text content and also return the same object as MCP `structuredContent`. Tool execution errors set MCP `isError: true`; an embedded `ok: false` alone is insufficient. Each tool advertises an object output schema and read/write annotations. These annotations describe behavior and do not authorize it. Existing tool names and text payload shapes remain available.

Normative definitions for WinGuiMcp MCP tools. Phase 1 tools are **required for MVP**; Phase 2+ tools are specified now but implemented in later phases (see [phases.md](phases.md)).

UEM node shapes: [data-model.md](data-model.md). Errors: [errors.md](errors.md).

## Common response contract

### Success envelope

Every successful tool call returns JSON with `"ok": true` plus tool-specific fields:

```json
{
  "ok": true
}
```

Failed calls use the error envelope in [errors.md](errors.md) (`"ok": false`, `"code"`, `"message"`, optional `"details"`).

### Window handles (HWND)

- JSON **integer** (signed 32-bit), e.g. `12345678` — not hex strings like `"0x00123456"`.
- Server rejects `handle: 0` with `InvalidArgument`.

### `includeSnapshot` on action tools

Action tools (`click`, `type_text`, `set_value`, `hotkey`) accept optional `includeSnapshot` (boolean, default `false`).

When `true`, the response includes a **full tree refresh** (not a diff) for the target window:

```json
{
  "ok": true,
  "method": "InvokePattern",
  "snapshotDelta": {
    "snapshotId": "550e8400-e29b-41d4-a716-446655440000",
    "handle": 12345678,
    "title": "Calculator",
    "processName": "CalculatorApp",
    "timestamp": "2026-06-06T12:00:00Z",
    "tree": { }
  }
}
```

The `tree` field is a UEM root node per [data-model.md](data-model.md). Taking a `snapshotDelta` invalidates prior refs for that window (same as `snapshot`).

Diff-only snapshots are a Phase 2 optimization; MVP always returns the full pruned tree.

Post-action `_warning` fields (see [errors.md](errors.md)) may appear alongside `snapshotDelta`. Warning emission is **specified now**; implementation is Phase 2.

---

## Phase 1 — MVP tools

### `list_windows`

Returns top-level visible windows.

**Input**

```json
{
  "type": "object",
  "properties": {
    "filter": {
      "type": "string",
      "description": "Case-insensitive substring match on title or process name"
    },
    "includeMinimized": {
      "type": "boolean",
      "default": false
    }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "windows": [
    {
      "handle": 12345678,
      "title": "Calculator",
      "processName": "CalculatorApp",
      "pid": 9012,
      "isForeground": true,
      "isMinimized": false
    }
  ]
}
```

**Execution:** `EnumWindows` + UIA root validation; filter in-process. Bbox omitted (agent uses `handle`).

---

### `launch_app`

Starts an application by executable path or Application User Model ID (AUMID).

**Input**

```json
{
  "type": "object",
  "oneOf": [
    { "required": ["path"] },
    { "required": ["appUserModelId"] }
  ],
  "properties": {
    "path": { "type": "string" },
    "appUserModelId": { "type": "string" },
    "args": { "type": "string" },
    "waitForWindowMs": {
      "type": "integer",
      "default": 10000,
      "minimum": 0
    }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "handle": 12345678,
  "title": "Notepad",
  "pid": 3456
}
```

**Execution:** `Process.Start` or shell `ShellExecute` for AUMID; poll `list_windows` until match or timeout → `LaunchFailed` / `Timeout`.

---

### `focus_window`

Brings a window to the foreground.

**Input**

```json
{
  "type": "object",
  "required": ["handle"],
  "properties": {
    "handle": { "type": "integer", "description": "HWND" }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "handle": 12345678,
  "focused": true
}
```

**Execution:** `SetForegroundWindow` with `AttachThreadInput` fallback if needed. Errors: `WindowGone`, `FocusFailed`, `UIPIBlocked`.

---

### `snapshot`

Returns a pruned UIA accessibility tree with stable refs.

**Input**

```json
{
  "type": "object",
  "required": ["handle"],
  "properties": {
    "handle": { "type": "integer" },
    "scope": {
      "type": "string",
      "enum": ["window", "focused-control"],
      "default": "window"
    },
    "detail": {
      "type": "string",
      "enum": ["control", "content"],
      "default": "control"
    },
    "includeOffscreen": {
      "type": "boolean",
      "default": false
    },
    "useVision": {
      "type": "boolean",
      "default": false,
      "description": "Phase 3: enable CV fallback"
    },
    "rootRef": {
      "type": "string",
      "description": "Optional root ref to scope snapshot to a specific subtree (e.g. dialog or container)"
    },
    "maxDepth": {
      "type": "integer",
      "default": 32,
      "minimum": 1,
      "description": "Maximum tree depth to traverse"
    }
  },
  "additionalProperties": false
}
```

**Output:** Snapshot envelope per [data-model.md](data-model.md).

**Execution:**

1. Resolve HWND → FlaUI `AutomationElement`.
2. Build `CacheRequest` with Name, ControlType, AutomationId, IsEnabled, BoundingRectangle, supported patterns.
3. Tree walk with pruning rules; assign refs; populate session table.
4. Phase 3: if sparse tree or `useVision`, run CV fusion.

Invalidates prior refs for the same window index.

---

### `find_element`

Locates an element in the snapshot index for the given window.

**Prerequisites:** A warm snapshot for `handle`. If none exists, the server **auto-runs `snapshot`** with default parameters before searching (same invalidation rules as an explicit snapshot).

**Input**

```json
{
  "type": "object",
  "required": ["handle", "by", "value"],
  "properties": {
    "handle": { "type": "integer" },
    "by": {
      "type": "string",
      "enum": ["name", "role", "automationId", "className", "text"]
    },
    "value": { "type": "string" },
    "nth": {
      "type": "integer",
      "default": 0,
      "minimum": 0,
      "description": "Zero-based index when multiple matches"
    },
    "match": {
      "type": "string",
      "enum": ["exact", "contains", "regex"],
      "default": "contains"
    }
  },
  "additionalProperties": false
}
```

**Output (single match)**

```json
{
  "ok": true,
  "ref": "w1e12",
  "role": "Button",
  "name": "Save"
}
```

**Output (multiple — only when caller uses list mode in Phase 2; MVP returns single)**

Errors: `ElementNotFound`, `Ambiguous`.

**Execution:** Query in-memory UEM index from the latest snapshot for `handle`; `text` matches `name` or `value`. Auto-snapshot when no snapshot exists for that handle.

---

### `get_properties`

Returns full UEM record for a ref (still no bbox in agent JSON).

**Input**

```json
{
  "type": "object",
  "required": ["ref"],
  "properties": {
    "ref": { "type": "string", "pattern": "^w\\d+e\\d+$" }
  },
  "additionalProperties": false
}
```

**Output:** UEM node object per [data-model.md](data-model.md).

**Execution:** Resolve the ref, then read current UIA value, range limits, toggle/selection/expand state, visibility and enabled properties. This is live readback, not the cached snapshot record. Protected control values are omitted. Error: `ElementNotFound`.

---

### `click`

Clicks an element by ref or physical point.

**Input**

```json
{
  "type": "object",
  "oneOf": [
    { "required": ["ref"] },
    { "required": ["point"] }
  ],
  "properties": {
    "ref": { "type": "string" },
    "point": {
      "type": "object",
      "required": ["x", "y"],
      "properties": {
        "x": { "type": "integer" },
        "y": { "type": "integer" }
      }
    },
    "button": {
      "type": "string",
      "enum": ["left", "right", "middle"],
      "default": "left"
    },
    "clickCount": {
      "type": "integer",
      "default": 1,
      "minimum": 1
    },
    "includeSnapshot": { "type": "boolean", "default": false }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "method": "InvokePattern"
}
```

`method` values: `InvokePattern`, `TogglePattern`, `SendInput`.

**Execution (ref path):**

1. Resolve ref → element + bbox.
2. UIA-sourced: try `InvokePattern` (or `TogglePattern` for checkboxes).
3. Fallback: `focus_window` → `SendInput` click at bbox center (physical px).
4. CV-sourced (Phase 3): always SendInput at bbox center.
5. Optional post-action delta + `_warning` if state unchanged.

**Point path:** SendInput only (escape hatch for debugging).

Never use deprecated `mouse_event`.

---

### `type_text`

Types text into the focused control or a specific ref.

**Input**

```json
{
  "type": "object",
  "required": ["text"],
  "properties": {
    "ref": { "type": "string" },
    "text": { "type": "string" },
    "clearFirst": { "type": "boolean", "default": false },
    "submit": {
      "type": "boolean",
      "default": false,
      "description": "Send Enter after text"
    },
    "includeSnapshot": { "type": "boolean", "default": false }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "method": "ValuePattern"
}
```

**Execution:**

1. If `ref`: focus element via UIA `SetFocus`.
2. **ValuePattern path** — use `ValuePattern.SetValue` when the element supports ValuePattern AND either:
   - `clearFirst: true`, or
   - the operation replaces the entire field value (empty field or full replace semantics).
3. **SendInput path** — use Unicode keystrokes when ValuePattern is unavailable, or when appending/editing without `clearFirst`.
4. If `submit`: SendInput Enter.

Do not use `PostMessage` for keyboard simulation.

---

### `set_value`

Sets control value directly via UIA ValuePattern.

**Input**

```json
{
  "type": "object",
  "required": ["ref", "value"],
  "properties": {
    "ref": { "type": "string" },
    "value": { "type": "string" },
    "includeSnapshot": { "type": "boolean", "default": false }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true
}
```

**Execution:** `ValuePattern.SetValue`. Error: `PatternNotSupported`.

---

### `hotkey`

Sends a key chord to the foreground window.

**Input**

```json
{
  "type": "object",
  "required": ["keys"],
  "properties": {
    "keys": {
      "type": "string",
      "description": "Chord such as ctrl+s, alt+f4, ctrl+shift+escape"
    },
    "handle": {
      "type": "integer",
      "description": "Optional HWND to focus before sending the chord"
    },
    "includeSnapshot": { "type": "boolean", "default": false }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "method": "SendInput"
}
```

**Execution:** If `handle` is supplied, focus that window first. Parse modifier
and key names, then send the sequence through SendInput. Error:
`InvalidArgument`, `FocusFailed`, `UIPIBlocked`.

Do not use `PostMessage` for keyboard simulation.

---

### `screenshot`

Captures a window or full screen.

**Input**

```json
{
  "type": "object",
  "properties": {
    "handle": {
      "type": "integer",
      "description": "Omit for full screen"
    },
    "region": {
      "type": "object",
      "properties": {
        "x": { "type": "integer" },
        "y": { "type": "integer" },
        "width": { "type": "integer" },
        "height": { "type": "integer" }
      }
    },
    "scale": {
      "type": "number",
      "default": 1.0,
      "minimum": 0.1,
      "maximum": 1.0,
      "description": "Downscale factor for token/storage economy"
    }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "imageBase64": "…",
  "mimeType": "image/png",
  "width": 1024,
  "height": 768
}
```

**Execution:** Windows.Graphics.Capture (WGC) per-window; BitBlt fallback Phase 1 if WGC unavailable. Phase 2: DXGI for full-screen games.

---

### `wait_for`

Waits until a condition holds or timeout.

**Input**

```json
{
  "type": "object",
  "required": ["handle", "condition"],
  "properties": {
    "handle": {
      "type": "integer",
      "description": "Target window handle. Use 0 for OS-level conditions (process/file waits)."
    },
    "condition": {
      "type": "string",
      "enum": [
        "element-exists",
        "element-enabled",
        "text-visible",
        "window-active",
        "window-closed",
        "element-gone",
        "element-hidden",
        "value-equals",
        "process-exists",
        "file-exists",
        "file-stable"
      ]
    },
    "target": {
      "type": "string",
      "description": "Locator (UI conditions) or process name / file path (OS conditions: process-exists, file-exists, file-stable)"
    },
    "timeoutMs": {
      "type": "integer",
      "default": 5000,
      "minimum": 0, "maximum": 3600000
    },
    "settleMs": { "type": "integer", "default": 200, "minimum": 0, "maximum": 60000 },
    "minBytes": { "type": "integer", "default": 1, "minimum": 0 },
    "changedSinceUtc": { "type": "string", "format": "date-time" }
  },
  "additionalProperties": false
}
```

**Output**

```json
{
  "ok": true,
  "ref": "w1e8",
  "elapsedMs": 340
}
```

**Execution:** Poll asynchronously with exponential backoff (50 ms → 500 ms cap), a bounded deadline, and request cancellation. OS-level conditions ignore `handle` (pass `0`). `file-stable` requires at least `minBytes` and unchanged size/last-write timestamp throughout `settleMs`; optional `changedSinceUtc` requires a later last-write timestamp. Old files can pass when freshness is not requested. This is a file-readiness predicate, not proof of application export success or image validity. Successful element waits return registered numeric refs. OS/window/absence waits return `condition` and `target` without an actionable ref. Error: `Timeout`, `WindowGone`, `InvalidArgument`.

---

## Phase 2+ — specified, not MVP

Brief definitions; implement in Phase 2 unless noted.

### Selector resolution (Phase 2+)

`find_element` covers MVP locators (`name`, `role`, `automationId`, `className`, `text`). Phase 2 may add a richer query string (research: `role=Button[name="OK"]`, `automationId=saveBtn`, tree `path=`). Resolution order matches [architecture.md](architecture.md#control-stack-resolution-order):

1. UIA properties and patterns (current snapshot index)
2. MSAA / IAccessible2 bridge when UIA is incomplete
3. Win32 context (HWND ancestry, class name)
4. Vision fallback — OCR text or icon match on window ROI (Phase 3)
5. Coordinate-only — last resort via `click.point` or `inspect_at`

Vision selectors (research: `ocr:"Save As"`, `icon:"trash"`) are Phase 3 additions to `find_element` or a future `query` tool.

### `attach`

Alias for `focus_window` + initial `snapshot`. Input: `handleOrTitleRegex`. Returns `{ handle, snapshotId, tree }`.

### `toggle` (Implemented)

Input: `{ ref, includeSnapshot? }`. Uses `TogglePattern.Toggle()`. Returns `{ ok: true, method: "TogglePattern" }`.

### `expand` / `collapse` (Implemented)

Input: `{ ref, includeSnapshot? }`. Uses `ExpandCollapsePattern.Expand()` or `Collapse()`. Returns `{ ok: true, method: "ExpandCollapsePattern" }`.

### `select` (Implemented)

Input: `{ ref, includeSnapshot? }`. Uses `SelectionItemPattern.Select()`. Returns `{ ok: true, method: "SelectionItemPattern" }`.

### `get_text` (Implemented)

Input: `{ ref }`. Uses `TextPattern`, `ValuePattern`, or element `Name`. Returns `{ ok: true, ref, text }`.

### `inspect_at` (Implemented)

Input: `{ x, y }`. Uses `AutomationElement.FromPoint` for interactive element debugging. Returns `{ ok: true, role, name, automationId, className, isEnabled, isOffscreen }`.

### `scroll` (Implemented)

Input: `{ ref?, x?, y?, direction: "up"|"down"|"left"|"right", amount: number, includeSnapshot? }`. Uses `ScrollPattern` or mouse wheel SendInput.

### `drag` (Implemented)

Input: `{ sourceRef?, startX?, startY?, targetRef?, endX?, endY?, button?, includeSnapshot? }`. SendInput drag sequence.

### `close_window` (Implemented)

Input: `{ handle }`. Closes a window cleanly via `WindowPattern.Close()` or `WM_CLOSE`.

### `set_window_state` (Implemented)

Input: `{ handle, state: "minimize"|"maximize"|"restore"|"normal", x?, y?, width?, height? }`. Controls window state and optional placement/dimensions.

### `get_active_modal` (Implemented)

Input: `{ handle }`. Checks whether a window is disabled by or owns an active modal dialog popup (`GetLastActivePopup` + `IsWindowEnabled`).

### `press_key` (Implemented)

Input: `{ key, handle?, count?, includeSnapshot? }`. Sends a single key (`Tab`, `Enter`, `Escape`, `ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight`, `Delete`, `Backspace`, `F1`-`F12`) one or more times.

### `search_tools` (Implemented)

Input: `{ query, category?, readOnlyOnly?, includeSchemas?, limit? }`. Searches the WinGuiMcp tool catalog, built-in/external plugins (`window`, `perception`, `action`, `capture`, `discovery`), UIA pattern aliases (e.g. `RangeValuePattern.SetValue`, `SelectionItemPattern.Select`, `InvokePattern.Invoke`), and UIA property locators (`AutomationId`, `Name`, `ControlType`, `ClassName`).

### `describe_tool` (Implemented)

Input: `{ tool }`. Returns full `inputSchema`, plugin metadata, tool aliases, and UIA parameter aliases for any canonical tool name or alias (e.g. `find.AutomationId`, `SelectionItemPattern.Select`).

### `execute_tool` (Implemented)

Input: `{ tool, arguments?, dryRun? }`. Validates and dispatches any WinGuiMcp tool by canonical name or UIA pattern/property alias, automatically normalizing parameter aliases (`hwnd` → `handle`, `elementRef` → `ref`, `AutomationId` → `automationId`). Pass `--compact-tools` or `MCP_TOOL_SEARCH_MODE=compact` at startup to expose only `search_tools`, `describe_tool`, and `execute_tool` in `tools/list`.

---

## Playwright mapping

| Playwright MCP | WinGuiMcp |
|----------------|-----------|
| `browser_snapshot` | `snapshot` |
| `browser_click` | `click { ref }` |
| `browser_fill` | `set_value` / `type_text` |
| keyboard shortcut | `hotkey { keys }` |
| `browser_wait_for` | `wait_for` |
| `page.screenshot` | `screenshot` |

---

## Agent workflow (recommended)

```
list_windows → focus_window → snapshot → find_element / traverse tree
→ click | set_value | type_text | hotkey → wait_for → snapshot (verify)
```

Example (Notepad save): `type_text` → `hotkey { keys: "ctrl+s" }` → `wait_for` dialog → `click` Save.

Re-snapshot after navigation or when receiving `ElementNotFound` with `stale: true`.

## UIA execution boundary

MCP tool calls serialize desktop operations on one dedicated background MTA worker. UIA automation is initialized lazily on first use. OS-only file waits use asynchronous polling; UI waits release the worker between polls. Each queued/provider operation has a 15-second deadline. A queued timeout or cancellation prevents that operation from starting. Cancellation does not revoke an already running native call. A running timeout reports `Timeout` with `outcomeUnknown: true` and quarantines the worker: inspect application state and restart the server before retrying. The server does not claim to terminate a blocked COM provider.
