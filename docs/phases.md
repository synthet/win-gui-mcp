# Implementation phases

Phased delivery plan with acceptance gates. Tool definitions: [tools.md](tools.md). Architecture modules: [architecture.md](architecture.md).

## Overview

| Phase | Focus | Ship criteria |
|-------|-------|---------------|
| 1 | MVP — UIA-only core | Manual gate apps pass |
| 2 | Robustness — patterns, events, metrics | Office-style workflows stable |
| 3 | Vision — CV fallback + fusion | Blind-canvas demo works |
| 4 | Security — elevation, hardening | UIPI errors actionable |

---

## Phase 1 — MVP (UIA-only)

**Goal:** Playwright-style snapshot + ref workflow on standard Windows apps without vision or coordinate-first interaction.

### Tools to implement

`list_windows`, `launch_app`, `focus_window`, `snapshot`, `find_element`, `get_properties`, `click`, `type_text`, `set_value`, `hotkey`, `screenshot`, `wait_for`

### Modules to build

- MCP host + tool registration
- `SnapshotSession` / `RefTable`
- `UiaSnapshotBuilder` with cache + pruning
- `PatternExecutor` (Invoke, Value) + `InputSimulator`
- `WindowCapture` (WGC with BitBlt fallback)
- `DpiManager` (Per-Monitor-V2 manifest)

### Acceptance gate

An agent (or manual script invoking tools) completes **multi-step flows without coordinate clicks** on:

| App | Sample flow |
|-----|-------------|
| Calculator | Launch → click digits → equals → read result via snapshot |
| Notepad | Launch → type text → save dialog navigate (Ctrl+S) |
| File Explorer | Open folder → select file → rename |
| Settings | Navigate to a settings page and toggle a switch |
| WinForms sample | Button click + text box fill |
| WPF sample | Menu navigation + dialog |

**Metrics (informational):**

- Snapshot latency < 2 s on Settings tree
- Snapshot JSON < 8k tokens for Calculator window

See [Performance budgets](#performance-budgets) for p95 targets from landscape research.

### Out of scope

Vision, `scroll`/`drag`, UIA events, trajectory logging, HTTP transport, post-action `_warning` emission (contract spec'd in [errors.md](errors.md); implement Phase 2).

---

## Phase 1 acceptance checklist

Run manually or via `/phase-gate`. Each flow must complete **without coordinate-only clicks** (`click.point`).

### Calculator

1. `launch_app` `{ path: "calc.exe" }` or focus existing window
2. `focus_window` → `snapshot`
3. `click` digit buttons via refs (e.g. `2`, `+`, `3`, `=`)
4. `snapshot` → read result from tree `name`/`value`

### Notepad

1. `launch_app` `{ path: "notepad.exe" }` → `snapshot`
2. `type_text` `{ text: "Hello WinGuiMcp" }`
3. `hotkey` `{ keys: "ctrl+s" }`
4. `wait_for` `{ condition: "element-exists", target: "Save As" }` (or dialog title)
5. `find_element` / `set_value` filename → `click` Save
6. `snapshot` verify saved title or content

### File Explorer

1. `launch_app` or focus Explorer → `snapshot`
2. `click` folder in tree/list via ref
3. `wait_for` `{ condition: "window-active" }` or content update
4. `click` file → `hotkey` `{ keys: "f2" }` or menu Rename
5. `type_text` / `set_value` new name → confirm
6. `snapshot` verify new name visible

### Settings

1. Open Settings app → `snapshot`
2. `find_element` / `click` navigate to a page (e.g. System)
3. `click` toggle or button via ref
4. `snapshot` verify state change in tree

### WinForms sample

1. Launch sample app → `snapshot`
2. `click` button via ref
3. `set_value` or `type_text` on text box
4. `snapshot` verify label or output updated

### WPF sample

1. Launch sample app → `snapshot`
2. `click` menu item via ref
3. `wait_for` dialog → `click` or `set_value` in dialog
4. `snapshot` verify dialog result

---

## Phase 2 — Robustness

**Goal:** Production-grade patterns, waits, and observability.

### Add tools

`toggle`, `expand`, `collapse`, `select`, `scroll`, `drag`, `get_text`, `inspect_at`, `attach`

### Enhancements

- Full control-pattern coverage in `PatternExecutor`
- UIA event subscriptions + `SetWinEventHook` for `wait_for` ([architecture.md](architecture.md)); polling remains the fallback
- Keep snapshot cache requests aligned with every pattern the walker probes; pruning already limits depth
- Idempotent `toggle` (ensure a state) when that tool lands; `click` stays a single activation
- Failure diagnostic bundle alongside trajectory logging: integrity, foreground HWND, local subtree, screenshot, DPI, and modal list
- **MSAA / IAccessible2 bridge** for legacy controls and Firefox-style gaps (per Windows-MCP precedent)
- Typed errors everywhere ([errors.md](errors.md))
- Post-action `_warning` when UI unchanged
- Trajectory logging (optional env flag)
- Snapshot latency + token size benchmarks documented
- DXGI capture for full-screen edge cases
- Measure against [performance budgets](#performance-budgets)

### Acceptance gate

- Multi-step **office-style workflow** (e.g., Notepad → WordPad: open, edit, format, save) without stale-ref failures
- `wait_for` succeeds on slow-loading dialog (< 10 s)
- Measure and record: p50 snapshot latency, median token count for File Explorer

---

## Phase 3 — Vision fallback

**Goal:** Hybrid UIA + CV for opaque UIs (custom renderers, games, sparse trees).

### Add capabilities

- `snapshot.useVision` and automatic sparse-tree detection
- **Three-tier perception** (structure → ROI OCR → full CV) per [architecture.md](architecture.md)
- Windows.Media.Ocr and/or RapidOCR (ONNX); Tesseract as low-dependency ROI fallback
- OmniParser-v2-style detector + captioner (note AGPL licensing on YOLO weights — see [security.md](security.md))
- IoU fusion: discard CV nodes with >10% overlap with UIA (UFO2 rule)
- `get_text` OCR fallback
- CV-sourced refs actuated via SendInput only

### Acceptance gate

- Locate and click a control in a **UIA-blind window** (e.g., simple canvas demo or known game menu) using vision-generated refs
- Document added latency per vision snapshot (target < 3 s on GPU-class hardware; CPU-only documented separately)
- Fusion metrics logged: `uiaNodeCount`, `cvNodeCount`, `fusedNodeCount`

### Licensing decision required

Before shipping CV models: resolve OmniParser icon_detect AGPL vs permissive retrain/replacement.

---

## Phase 4 — Security and hardening

**Goal:** Safe deployment and clear failure modes for elevation boundaries.

### Deliverables

- Structured `UIPIBlocked` and `SecureDesktop` errors in all action paths
- Documentation: running elevated, optional `uiAccess=true` manifest path
- Human-in-the-loop hook for destructive actions (delete confirmations, format drive patterns) — config flag
- Deployment guide in [security.md](security.md)
- Future transport auth spec (Bearer + IP allowlist) — design only unless implemented

### Acceptance gate

- Non-elevated server receives `UIPIBlocked` when targeting elevated Task Manager
- Elevated server successfully automates non-elevated Notepad
- Secure desktop (UAC prompt) returns `SecureDesktop`, not hang

### Evaluation (optional stretch)

Run subset of [Windows Agent Arena](https://github.com/microsoft/WindowsAgentArena) tasks; compare to UFO2 baseline (research context only—not a SLA).

---

## Performance budgets

Proposed p95 targets on a local desktop (informational, not SLA). Source: [research/deep-research-report.md](research/deep-research-report.md) / landscape PDF. Measure and record in Phase 2.

| Operation | Proposed p95 |
|-----------|--------------|
| `list_windows` | < 30 ms |
| UIA tree walk (modest tree, cached) | < 100 ms |
| `screenshot` (single window, WGC) | < 40 ms |
| ROI OCR on small crop | < 120 ms (Phase 3) |
| End-to-end `click` dispatch (excl. app response) | < 250 ms |
| Full `snapshot` (tree + optional screenshot) | < 200 ms on business apps |

Hardware, DPI, and app framework affect results; document environment when publishing benchmarks.

---

## Risk mitigations (from landscape research)

| Risk | Mitigation in WinGuiMcp |
|------|-------------------------|
| Incomplete UIA coverage | MSAA/IA2 bridge (Phase 2) + CV fallback (Phase 3) |
| UIPI / elevation | Structured `UIPIBlocked`; separate elevated helper option ([security.md](security.md)) |
| Focus races before SendInput | Re-check foreground; prefer UIA patterns |
| DPI / scaling / localization | Per-Monitor-V2; semantic refs first; language-aware OCR config (Phase 3) |
| Browser / Firefox gaps | IA2 bridge Phase 2; not in Phase 1 scope |
| Remote abuse surface | Stdio-only MVP; auth + IP allowlist for future HTTP transport |

---

## Module mapping by phase

| Module | P1 | P2 | P3 | P4 |
|--------|:--:|:--:|:--:|:--:|
| Transport/Tools | ✓ | ✓ | ✓ | ✓ |
| Session/RefTable | ✓ | ✓ | ✓ | ✓ |
| UiaSnapshotBuilder | ✓ | ✓ | ✓ | ✓ |
| PatternExecutor | partial | ✓ | ✓ | ✓ |
| InputSimulator | ✓ | ✓ | ✓ | ✓ |
| WindowCapture WGC | ✓ | ✓ | ✓ | ✓ |
| DpiManager | ✓ | ✓ | ✓ | ✓ |
| Events/SetWinEventHook | | ✓ | ✓ | ✓ |
| MSAA/IA2 bridge | | ✓ | ✓ | ✓ |
| Auto-wait events | | ✓ | ✓ | ✓ |
| Observability | | ✓ | ✓ | ✓ |
| VisionFusion | | | ✓ | ✓ |
| Security elevation docs | | | | ✓ |

---

## Decision thresholds

| Observation | Response |
|-------------|----------|
| >30% target apps UIA-blind | Promote CV to co-primary; invest in permissive detector |
| Snapshot p95 > 3 s | Optimize cache scope; add subtree targeting |
| Python ecosystem required | Out of scope — stay .NET |
| Sub-200 ms perception required | Consider Rust capture core behind .NET service |
