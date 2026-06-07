# Technology stack

Normative stack decisions for WinGuiMcp. Module layout: [architecture.md](architecture.md). Phase scope: [phases.md](phases.md).

**Summary:** Implement in **C# on .NET 9** with **FlaUI 5 (UIA3)**, the official **`ModelContextProtocol` SDK**, **`Microsoft.Extensions.Hosting`**, and **P/Invoke `SendInput`** as actuation fallback.

## Language and runtime

| Decision | Choice |
|----------|--------|
| Language | **C#** |
| Runtime | **.NET 9** |
| Host model | Console exe, `Microsoft.Extensions.Hosting` for DI and lifecycle |
| Deployment | `dotnet run` (dev); self-contained publish (Phase 4+) |

### Why C# / .NET 9

Microsoft UI Automation (UIA) is a COM API. .NET provides first-class, strongly typed bindings via `System.Windows.Automation` and the actively maintained [FlaUI](https://github.com/FlaUIO/FlaUI) wrapper. The landscape survey in [research/deep-research-report.md](research/deep-research-report.md) and the design notes in [research/design-research.md](research/design-research.md) both conclude that accessibility-first Windows automation maturity clusters around UIA wrappers—not around MCP-native or vision-first tools.

| Criterion | C# / .NET 9 | Primary alternatives |
|-----------|-------------|----------------------|
| UIA / COM access | Native bindings + FlaUI 5 | Python (comtypes, pywinauto): viable but COM marshalling and GIL overhead on large tree walks |
| Mature UIA wrapper | FlaUI — UIA3 primary, UIA2 fallback | No equivalent in Node; pywinauto mature in Python |
| MCP SDK | Official `ModelContextProtocol` NuGet | Python/TypeScript SDKs exist; do not add UIA depth |
| Win32 interop | P/Invoke for `SendInput`, WGC, DXGI, hooks | Same via ctypes/FFI with more friction |
| Prior art | [shanselman/FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) — Playwright-style refs | Windows-MCP (Python), MCPControl (Node): thinner input/capture wrappers |
| Phase 3 CV | ONNX Runtime, `Windows.Media.Ocr`, WinRT capture | Python stronger for ML prototyping; .NET sufficient for inference |
| Performance | Sub-second snapshots on typical dialogs (Phase 1 target) | Python acceptable for prototypes; weaker at scale for cached UIA walks |
| Packaging | Single self-contained exe, no Python venv | Python + CV deps heavier for end-user install |

### Alternatives not chosen as primary

| Stack | Role | Why not primary |
|-------|------|-----------------|
| **Python** (pywinauto, uiautomation, Windows-MCP) | Second choice if embedding in an existing Python agent loop | Weaker performance on large UIA snapshots; heavier CV packaging |
| **TypeScript / Node** (MCPControl, RobotJS) | Input/capture building blocks only | No mature UIA element model or control-pattern layer |
| **C++** (WinAppDriver-style service) | Remote WebDriver driver | Overkill for local stdio MCP server |
| **Rust** (windows-rs, uiautomation crate) | Optional future capture/snapshot core | UIA + MCP ecosystem less mature than .NET today |
| **Image-first** (PyAutoGUI, SikuliX) | Out of scope | Contradicts pattern-before-pixels and snapshot + ref model |

**Exception paths (document only, not Phase 1):**

- Python-centric deployment that must share code with Windows-MCP / Windows-Use → accept Python despite COM/GIL trade-offs.
- Sub-200 ms perception at scale → consider a Rust capture core behind the .NET MCP service (Phase 3+ optimization).

## Stack by layer

| Layer | Technology | Phase | Notes |
|-------|------------|-------|-------|
| Language / runtime | C# / .NET 9 | 1 | `net9.0` target |
| DI / lifecycle | `Microsoft.Extensions.Hosting` | 1 | Tool handlers, session, platform services |
| MCP transport | `ModelContextProtocol` SDK, stdio JSON-RPC 2.0 | 1 | HTTP/SSE + auth in Phase 4 |
| UIA perception | FlaUI 5 — **UIA3 primary**, UIA2 fallback | 1 | UIA2 for legacy WinForms only |
| Session / refs | In-process `SnapshotSession` + `RefTable` | 1 | Ref format: [data-model.md](data-model.md) |
| Actuation | UIA control patterns → P/Invoke `SendInput` | 1 | No PostMessage keyboard; no `mouse_event` |
| Capture | Windows.Graphics.Capture (WGC), BitBlt fallback | 1 | Screenshots adjunct to snapshots |
| Capture (advanced) | DXGI Desktop Duplication | 2 | Games / exclusive fullscreen |
| DPI | Per-Monitor-V2 application manifest | 1 | Bboxes in physical pixels |
| Events | UIA events + `SetWinEventHook` | 2 | Event-driven `wait_for` |
| CV perception | ONNX Runtime + `Windows.Media.Ocr` / RapidOCR | 3 | ROI-first; full-screen CV last resort |
| MSAA / IA2 bridge | Accessibility adapter module | 2 | Firefox-style gaps |

## NuGet packages (Phase 1 baseline)

Declared in `src/WinGuiMcp/WinGuiMcp.csproj`:

| Package | Version (baseline) | Purpose |
|---------|-------------------|---------|
| `FlaUI.Core` | 5.0.0 | UIA abstraction |
| `FlaUI.UIA3` | 5.0.0 | UIA3 backend |
| `ModelContextProtocol` | 1.4.0 | MCP server + stdio transport |
| `Microsoft.Extensions.Hosting` | 10.0.x | Host bootstrap, DI |
| `System.Drawing.Common` | 9.0.x | PNG encode for screenshots |
| `Vortice.Direct3D11` | 3.6.x | D3D11 for WGC capture |
| `Vortice.DXGI` | 3.6.x | DXGI interop for WGC |

Phase 2+ packages (add when implementing): ONNX Runtime, additional WinRT/OCR packages as needed.

## Architectural decisions aligned with research

The landscape survey ([research/deep-research-report.md](research/deep-research-report.md)) recommended a **two-layer** design: a deterministic Windows controller service (WebDriver-like) plus an MCP shim. WinGuiMcp **adopts the research conclusions** but **collapses to MCP-first**:

| Research conclusion | WinGuiMcp decision |
|--------------------|-------------------|
| Hybrid accessibility-first controller | UIA patterns + refs primary; CV in Phase 3 |
| MCP as agent transport | stdio MCP in Phase 1; HTTP in Phase 4 |
| Non-elevated default | Standard user integrity; `UIPIBlocked` on elevation mismatch |
| ROI-first perception | UIA tree first; OCR on bbox crops; full CV last |
| Structured error model | Typed codes in [errors.md](errors.md) |
| Separate WebDriver controller | **Rejected for MVP** — MCP is the sole agent interface (Phase 1–4) |

## Control resolution order

Use the highest layer that works; fall downward only when required. Full table: [architecture.md#control-stack-resolution-order](architecture.md#control-stack-resolution-order).

1. UIA control patterns (Invoke, Value, Toggle, …)
2. UIA tree + refs (snapshot, `find_element`)
3. MSAA / IAccessible2 bridge (Phase 2+)
4. Win32 window APIs (`list_windows`, focus)
5. SendMessage (known contracts only, Phase 2+, opt-in)
6. SendInput at cached bbox center
7. Capture + ROI OCR / CV (Phase 3)

**Out of scope for all phases:** kernel drivers, virtual HID, `SetWindowsHookEx` injection, PostMessage keyboard, primary coordinate navigation.

## Prior art

| Project | Relevance |
|---------|-----------|
| [shanselman/FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) | Closest existing Playwright-style Windows MCP; FlaUI snapshot + ref mapping |
| [CursorTouch/Windows-MCP](https://github.com/CursorTouch/Windows-MCP) | Python MCP server; UIA + PyAutoGUI; useful comparison, not implementation source |
| [pywinauto](https://github.com/pywinauto/pywinauto) | Mature Python UIA/Win32 library; validates accessibility-first approach |
| [FlaUI](https://github.com/FlaUIO/FlaUI) | Core UIA wrapper WinGuiMcp builds on |

When borrowing patterns from prior art, reconcile tool names and schemas with [tools.md](tools.md) before implementing.

## Research references (background only)

Do not implement from research files directly. They informed stack selection:

- [research/deep-research-report.md](research/deep-research-report.md) — open-source landscape, API stack, security boundaries
- [research/design-research.md](research/design-research.md) — C#/FlaUI recommendation, CV tier design, prior-art inventory
