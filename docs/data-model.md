# Unified Element Model (UEM)

The Unified Element Model is the JSON representation of UI elements returned by `snapshot`, `find_element`, and `get_properties`. It merges UIA accessibility data (Phase 1+) and computer-vision detections (Phase 3+) into one tree the agent can act on via refs.

See [tools.md](tools.md) for tool I/O and [architecture.md](architecture.md) for how snapshots are built.

## Design principles

1. **Text-first, token-efficient** — Omit bounding boxes and internal HWNDs from agent-facing JSON. The server retains physical coordinates in a session table keyed by ref.
2. **Stable within a snapshot** — Refs are valid until the next `snapshot` on the same window (or session reset). See [Session invalidation](#session-invalidation).
3. **Pruned tree** — Non-interactive structural containers (layout panes, invisible splitters, empty groups) are dropped unless they carry semantic meaning.
4. **Source transparency** — Each node records whether it came from UIA or CV so the action layer can choose the right actuation path.

## Ref format

```
w<windowIndex>e<elementIndex>
```

| Part | Meaning |
|------|---------|
| `windowIndex` | 1-based index into the server's window registry for the current session (not the raw HWND). |
| `elementIndex` | Monotonically allocated positive index for the server lifetime; never recycled after snapshot refresh. |

Examples: `w1e1` (window root), `w1e42`, `w2e7`.

Refs are **not** stable across snapshots. After `snapshot`, the agent must use refs from the latest response.

Window registry indices also are not recycled on session clear. An invalidated ref must fail resolution, rather than later naming a different element. `get_properties` reads current UIA properties through the ref; snapshot nodes remain a point-in-time view.

## Window handles (HWND)

Used in tool inputs (`handle`) and snapshot envelopes:

- JSON **integer** (signed 32-bit), e.g. `12345678`
- Not hex strings (`"0x00123456"`) or strings
- Invalid `0` → `InvalidArgument`

The `windowIndex` in refs (`w1e42`) is a session registry index, not the raw HWND.

### Internal session table (server-only)

For each ref the server stores:

| Field | Type | Purpose |
|-------|------|---------|
| `hwnd` | integer | Target top-level window |
| `automationElementId` | opaque | FlaUI element handle or weak reference |
| `bbox` | `{x, y, width, height}` | Physical pixels (Per-Monitor-V2 aware) |
| `source` | `"uia"` \| `"cv"` | Actuation routing |
| `patterns` | string[] | Supported UIA patterns at snapshot time |
| `snapshotId` | UUID | Generation that created this ref |

## UEM node schema

Each node in a snapshot tree conforms to this shape:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://wingui-mcp.dev/schemas/uem-node.json",
  "type": "object",
  "required": ["ref", "role", "source"],
  "properties": {
    "ref": {
      "type": "string",
      "pattern": "^w\\d+e\\d+$",
      "description": "Stable ref within the current snapshot"
    },
    "source": {
      "type": "string",
      "enum": ["uia", "cv"]
    },
    "role": {
      "type": "string",
      "description": "Semantic control type (Button, Edit, MenuItem, Window, …)"
    },
    "name": {
      "type": "string",
      "description": "Accessibility name, label, or CV caption"
    },
    "automationId": {
      "type": "string",
      "description": "Developer-assigned AutomationId when present"
    },
    "className": {
      "type": "string",
      "description": "Win32 class name or framework class"
    },
    "value": {
      "type": "string",
      "description": "Current value from ValuePattern or OCR text"
    },
    "enabled": {
      "type": "boolean"
    },
    "states": {
      "type": "array",
      "items": { "type": "string" },
      "description": "e.g. focused, selected, expanded, collapsed, offscreen"
    },
    "patterns": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Supported interaction patterns: Invoke, Value, Toggle, ExpandCollapse, Scroll, Selection, …"
    },
    "patternValues": {
      "type": "object",
      "properties": {
        "rangeValue": {
          "type": "object",
          "properties": {
            "value": { "type": "number" },
            "minimum": { "type": "number" },
            "maximum": { "type": "number" },
            "smallChange": { "type": "number" },
            "readOnly": { "type": "boolean" }
          },
          "additionalProperties": false
        },
        "toggle": { "type": "string" },
        "selected": { "type": "boolean" },
        "expandCollapse": { "type": "string" }
      },
      "additionalProperties": false
    },
    "children": {
      "type": "array",
      "items": { "$ref": "#" }
    }
  },
  "additionalProperties": false
}
```

### Field mapping from UIA

| UIA property | UEM field | Notes |
|--------------|-----------|-------|
| ControlType | `role` | Serialized as string (e.g. `"Button"`) |
| Name | `name` | Empty string omitted |
| AutomationId | `automationId` | Omitted if empty |
| ClassName | `className` | Omitted if empty |
| IsEnabled | `enabled` | Omitted if true (default) |
| ValuePattern.Value | `value` | Text fields, combos |
| RangeValuePattern.Value | `value`, `patternValues.rangeValue` | Numeric value string plus numeric limits, step, and read-only state |
| Toggle / SelectionItem / ExpandCollapse | `patternValues` | Current pattern-specific state, if supported |

| Supported patterns | `patterns` | Listed by name |
| BoundingRectangle | *(server only)* | Never sent to agent |

Values and pattern values are omitted for protected (`IsPassword`) controls.

### CV nodes (Phase 3)

CV-sourced nodes use `source: "cv"`. Additional server-only fields: detection confidence, OCR confidence. Agent-facing fields match UIA nodes; `role` may be inferred (`"Button"`, `"Text"`, `"Icon"`) from detector class + caption.

## Snapshot response envelope

Returned by `snapshot` and embedded as `snapshotDelta` on action tools when `includeSnapshot: true` ([tools.md](tools.md)).

```json
{
  "ok": true,
  "snapshotId": "550e8400-e29b-41d4-a716-446655440000",
  "handle": 12345678,
  "title": "Calculator",
  "processName": "CalculatorApp",
  "timestamp": "2026-06-06T12:00:00Z",
  "tree": { }
}
```

The `tree` field is the UEM root node for the window.

Optional fields when `useVision: true` (Phase 3):

```json
{
  "visionUsed": true,
  "uiaNodeCount": 12,
  "cvNodeCount": 5,
  "fusedNodeCount": 14
}
```

## Tree pruning rules

Applied during UIA tree walk before serialization:

| Rule | Action |
|------|--------|
| Control type is Pane/Group with no name, no patterns, single child | Hoist child; drop pane |
| `IsOffscreen` and `includeOffscreen: false` | Exclude node and subtree |
| No name, no automationId, no patterns, not a known interactive type | Exclude |
| Duplicate sibling nodes (same name + role + automationId) | Keep first; log warning |
| Depth exceeds server max (default 32) | Truncate; append `states: ["truncated"]` on parent |

**Interactive types always kept:** Button, CheckBox, ComboBox, Edit, Hyperlink, ListItem, MenuItem, RadioButton, Slider, Spinner, TabItem, TreeItem, DataItem, Document, and types exposing Invoke, Value, Toggle, Selection, or ExpandCollapse patterns.

### Control view vs content view

`snapshot.detail`:

- `"control"` (default) — Control view; smaller tree, interaction-focused.
- `"content"` — Content view; includes text labels and read-only content useful for reading UI state.

## CV fusion (Phase 3)

When vision fallback runs:

1. Capture window region via WGC.
2. Run OCR + icon detector + captioner.
3. Convert detections to UEM nodes with `source: "cv"`.
4. **Dedup:** Discard CV nodes with IoU > 10% against any UIA node (UFO2 rule; UIA wins).
5. Assign refs from the shared namespace; append CV-only nodes to the tree (often under a canvas parent).

## Session invalidation

Refs become stale when:

- A new `snapshot` is taken on the same window
- The target window is closed (`WindowGone`)
- Session timeout (implementation default: 30 minutes idle; configurable)

Tools that accept `ref` must return `ElementNotFound` with `stale: true` when the ref's `snapshotId` does not match the current snapshot for that ref's window. See [errors.md](errors.md).

## Playwright mapping

| Playwright snapshot | WinGuiMcp UEM |
|---------------------|---------------|
| `[ref=e5]` | `"ref": "w1e5"` |
| `role=button` | `"role": "Button"` |
| `name=` | `"name"` |
| `testId=` | `"automationId"` |

Bounding boxes are intentionally omitted from agent JSON (Playwright MCP also keeps layout server-side).

## Element resolution order

When locating or acting on elements, the server follows the control stack in [architecture.md](architecture.md). UEM nodes record `source: "uia" | "cv"` so the action layer routes correctly. Staleness and confidence metadata stay server-side in the ref table (Phase 2: expose `confidence` in `get_properties` for CV nodes only).
