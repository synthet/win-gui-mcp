# MCP tool reference

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

**Execution:** Session table lookup. Error: `ElementNotFound`.

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
    "handle": { "type": "integer" },
    "condition": {
      "type": "string",
      "enum": [
        "element-exists",
        "element-enabled",
        "text-visible",
        "window-active"
      ]
    },
    "target": {
      "type": "string",
      "description": "Locator value: name, automationId, or text depending on condition"
    },
    "timeoutMs": {
      "type": "integer",
      "default": 5000,
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
  "ref": "w1e8",
  "elapsedMs": 340
}
```

**Execution:** Poll UIA with exponential backoff (100 ms → 500 ms cap). Phase 2: UIA event subscriptions. Error: `Timeout`, `WindowGone`.

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

### `toggle`

Input: `{ ref }`. Uses `TogglePattern`. Phase 2.

### `expand` / `collapse`

Input: `{ ref }`. Uses `ExpandCollapsePattern`. Phase 2.

### `select`

Input: `{ ref }`. Uses `SelectionItemPattern.Select()`. Phase 2.

### `scroll`

Input: `{ ref?, point?, direction: "up"|"down"|"left"|"right", amount: number }`. Uses `ScrollPattern` or mouse wheel SendInput. Phase 2.

### `drag`

Input: `{ fromRef|fromPoint, toRef|toPoint }`. SendInput drag sequence. Phase 2.

### `get_text`

Input: `{ ref }`. Uses `TextPattern` or `ValuePattern`; OCR fallback Phase 3. Phase 2.

### `inspect_at`

Input: `{ x, y }`. `AutomationElement.FromPoint` for debugging. Phase 2.

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
