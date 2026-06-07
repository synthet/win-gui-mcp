# WinGuiMcp Technical Specification

Executive summary for the WinGuiMcp MCP server. Detailed specs live in linked sub-documents.

**Documentation index:** [README.md](README.md)

## Overview

WinGuiMcp is a Model Context Protocol (MCP) server that provides an AI-friendly interface for controlling the Windows desktop. It follows a **snapshot + ref** model inspired by Playwright MCP: agents receive a structured accessibility tree with stable refs (e.g. `w1e42`) and act on refs—not pixel coordinates.

## Design principles

1. **Accessibility-first** — Microsoft UI Automation (UIA) is the primary perception and actuation path.
2. **Vision fallback** — OCR + element detection when UIA coverage is insufficient (Phase 3).
3. **Pattern before pixels** — Invoke/Value/Toggle patterns before SendInput; never primary `mouse_event` or PostMessage keyboard.
4. **Token-efficient** — Text trees to the agent; bounding boxes stay server-side.

## Stack

| Component | Choice |
|-----------|--------|
| Language | C# |
| Runtime | .NET 9 |
| Host | `Microsoft.Extensions.Hosting` |
| UIA | FlaUI 5 (UIA3 primary, UIA2 fallback) |
| MCP | `ModelContextProtocol` SDK, stdio JSON-RPC 2.0 |
| Input fallback | P/Invoke `SendInput` |
| Capture | WGC (Phase 1), DXGI (Phase 2+) |
| Perception | UIA cached tree + optional CV fusion (Phase 3) |

**Rationale, alternatives considered, and NuGet baseline:** [stack.md](stack.md).

See [architecture.md](architecture.md) for layers and module layout.

## Unified Element Model

Snapshots return a pruned JSON tree ([data-model.md](data-model.md)):

- Each node: `ref`, `role`, `name`, `automationId`, `patterns`, `children`, …
- Ref format: `w<windowIndex>e<elementIndex>` — valid until the next `snapshot` on that window
- UIA and CV nodes share one namespace after IoU fusion (Phase 3)

## MCP tools

### Phase 1 MVP

| Category | Tools |
|----------|-------|
| Navigation | `list_windows`, `launch_app`, `focus_window` |
| Inspection | `snapshot`, `find_element`, `get_properties` |
| Action | `click`, `type_text`, `set_value`, `hotkey` |
| Utility | `screenshot`, `wait_for` |

Full schemas and execution strategies: [tools.md](tools.md).

### Phase 2+

`toggle`, `expand`, `collapse`, `select`, `scroll`, `drag`, `get_text`, `inspect_at`, `attach` — specified in [tools.md](tools.md), implemented per [phases.md](phases.md).

## Errors

Structured error codes (`ElementNotFound`, `UIPIBlocked`, `Timeout`, …) with agent recovery hints: [errors.md](errors.md).

Post-action `_warning` when UI state is unchanged after an action.

## Security

- Default: user's integrity level (non-elevated)
- Elevated targets require an elevated server
- UIPI and secure-desktop failures are hard errors, not hangs

Details: [security.md](security.md).

## DPI

Per-Monitor-V2 DPI awareness required. All internal bboxes in physical pixels.

## Implementation roadmap

| Phase | Scope |
|-------|-------|
| 1 | UIA-only MVP — snapshot, ref, core actions |
| 2 | Full patterns, events, logging, metrics |
| 3 | CV fallback + IoU fusion |
| 4 | Elevation hardening, confirmation gates |

Acceptance gates: [phases.md](phases.md).

## Non-goals (MVP)

- Multi-agent orchestration (UFO HostAgent)
- Record-and-replay script export
- Remote/cross-OS desktop control
- HTTP MCP transport (Phase 1)
- Separate WebDriver/JSON-RPC controller layer (MCP is the agent interface)
- Kernel / virtual HID / driver-level input

## Research basis

Landscape analysis: [research/deep-research-report.md](research/deep-research-report.md) (optional PDF in [research/](research/)). Conclusions adopted: hybrid accessibility-first design, MCP as agent transport, non-elevated default, ROI-first perception, structured error model. Stack evaluation and language choice: [stack.md](stack.md).

## Prior art

[shanselman/FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) — closest existing Playwright-style Windows MCP implementation.

Background research (not normative): [research/](research/).
