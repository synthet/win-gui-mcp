# Error catalog

WinGuiMcp tools return structured errors so agents can retry, re-snapshot, or escalate without parsing free-text messages.

## Error envelope

Failed tool calls return MCP tool errors with this JSON shape in the message content:

```json
{
  "ok": false,
  "code": "ElementNotFound",
  "message": "Ref w1e42 is stale; take a new snapshot.",
  "details": {
    "ref": "w1e42",
    "stale": true,
    "snapshotId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

Successful calls may include warnings (not failures):

```json
{
  "ok": true,
  "_warning": "Action executed via SendInput, but UI state remains unaltered. Verify target or wait for latency."
}
```

## Error codes

| Code | Severity | When | Agent recovery hint |
|------|----------|------|---------------------|
| `ElementNotFound` | recoverable | Ref unknown, stale, or locator matched nothing | Call `snapshot` or refine `find_element` criteria |
| `Ambiguous` | recoverable | Locator matched multiple nodes | Narrow locator (`automationId`, `nth`, parent context) |
| `PatternNotSupported` | recoverable | Requested action needs a UIA pattern the element lacks | Retry with `click` (SendInput fallback) or pick another element |
| `ElementNotEnabled` | recoverable | Target exists but `enabled: false` | Wait or find alternate control |
| `VisibilityMismatch` | recoverable | Target exists but offscreen, hidden, or zero-size bbox | Enable `includeOffscreen`, scroll into view, or pick visible sibling |
| `UIPIBlocked` | hard | Target process higher integrity than server | Relaunch server elevated; see [security.md](security.md) |
| `SecureDesktop` | hard | UAC / Winlogon / locked desktop | User must dismiss dialog or unlock session |
| `Timeout` | recoverable | `wait_for` exceeded | Increase timeout, re-snapshot, or change condition |
| `WindowGone` | recoverable | HWND invalid or window closed | `list_windows` and re-attach |
| `FocusFailed` | recoverable | Could not foreground target window | Call `focus_window`; check if minimized |
| `InvalidArgument` | client | Malformed params or unknown enum | Fix request schema |
| `CVLowConfidence` | recoverable | Phase 3: vision detection below threshold | Retry with `useVision: true` or manual `screenshot` |
| `CaptureFailed` | recoverable | Screenshot or WGC capture error | Retry; check if window is occluded |
| `LaunchFailed` | recoverable | `launch_app` could not start process | Verify path/AUMID and permissions |
| `ConfirmationRequired` | hard | Phase 4: destructive action blocked pending user ack | User confirms via client; retry with `confirmationId` |
| `InternalError` | hard | Unexpected server exception | Log and retry once; report if persistent |

### Severity

- **recoverable** — Agent should adapt strategy and retry.
- **hard** — Requires environment change (elevation, user action, config).
- **client** — Fix the tool call; no retry without changing input.

### Optional `details` fields (Phase 2+)

Errors may include machine-readable context for agents and trajectory logs:

| Field | When |
|-------|------|
| `selector` / `by`, `value` | Locator failures |
| `backend` | `"uia"`, `"msaa"`, `"cv"`, `"sendinput"` — last resolution path attempted |
| `candidates` | Short list of `{ ref, name, role }` on `Ambiguous` |
| `traceId` | Links to observability log entry |

### Research terminology aliases

Landscape survey ([research/deep-research-report.md](research/deep-research-report.md)) uses alternate names — map to WinGuiMcp codes:

| Research term | WinGuiMcp code |
|---------------|----------------|
| `AmbiguousMatch` | `Ambiguous` |
| `StaleElement` | `ElementNotFound` (`details.stale: true`) |
| `ActionNotSupported` | `PatternNotSupported` |
| `FocusDenied` | `FocusFailed` |
| `PrivilegeBoundary` | `UIPIBlocked` |
| `WindowClosed` | `WindowGone` |
| `LowConfidenceVisionMatch` | `CVLowConfidence` |
| `TimeoutExceeded` | `Timeout` |

## Per-tool error mapping

| Tool | Common codes |
|------|----------------|
| `snapshot` | `WindowGone`, `CaptureFailed`, `UIPIBlocked` |
| `find_element` | `ElementNotFound`, `Ambiguous`, `Timeout` |
| `get_properties` | `ElementNotFound` |
| `click`, `type_text`, `set_value` | `ElementNotFound`, `PatternNotSupported`, `ElementNotEnabled`, `VisibilityMismatch`, `UIPIBlocked`, `FocusFailed` |
| `hotkey` | `InvalidArgument`, `FocusFailed`, `UIPIBlocked` |
| `focus_window` | `WindowGone`, `FocusFailed`, `UIPIBlocked` |
| `launch_app` | `LaunchFailed` |
| `wait_for` | `Timeout`, `WindowGone` |
| `screenshot` | `WindowGone`, `CaptureFailed` |

## Post-action warnings

Inspired by VLAA-GUI completeness verification. **Specified in this catalog; emitted by the server starting Phase 2** (not required for Phase 1 MVP).

Emitted on success when:

| Condition | `_warning` text |
|-----------|-----------------|
| Click/type had no detectable UI tree delta within 500 ms | UI state unchanged after action |
| SendInput used instead of UIA pattern | Action used synthetic input fallback |
| Target was offscreen; scroll attempted first | Element was offscreen; scrolled into view |

Warnings do not set `ok: false`. Agents should treat them as signals to verify task completion before stopping.

## Ambiguous locator

When `find_element` matches multiple nodes and `nth` is not sufficient:

```json
{
  "ok": false,
  "code": "Ambiguous",
  "message": "Locator matched 3 elements; narrow criteria or set nth.",
  "details": {
    "matchCount": 3,
    "refs": ["w1e5", "w1e12", "w1e19"]
  }
}
```

## Stale ref detection

When `ref` was issued in snapshot `A` but the current session snapshot is `B`:

```json
{
  "ok": false,
  "code": "ElementNotFound",
  "message": "Ref w1e42 is stale; take a new snapshot.",
  "details": {
    "ref": "w1e42",
    "stale": true,
    "expectedSnapshotId": "…current…",
    "refSnapshotId": "…original…"
  }
}
```

## UIPI and elevation

When input or UIA cross-process call is blocked by integrity level:

```json
{
  "ok": false,
  "code": "UIPIBlocked",
  "message": "Cannot interact with elevated process 'Task Manager' (PID 1234). Run WinGuiMcp as Administrator.",
  "details": {
    "targetProcess": "Taskmgr.exe",
    "targetPid": 1234,
    "serverElevated": false
  }
}
```

See [security.md](security.md) for operational guidance.
