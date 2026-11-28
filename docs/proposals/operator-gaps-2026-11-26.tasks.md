# Tasks: operator gaps (Synth Term session)

Spec: [operator-gaps-2026-11-26.md](operator-gaps-2026-11-26.md) §7. Each task is one PR. Every code task updates
`tests/WinGuiMcp.Tests/golden/tool-surface.json` when the tool surface changes, and its own tests come first
(red, then green) in the same PR.

Phase placement ([ADR-0002](../adr/0002-operator-gaps.md), owner decision 2026-11-28): T-2, T-3, T-9 and T-10 are Phase 1 hardening (label `phase:1-hardening`), plus `wgui-bbf78` (scenario `outputPath` confinement), which was filed from the review and has no T-number. T-4, T-5, T-6, T-7, T-8 and T-11 are Phase 2 (label `phase:2`) and wait for the Phase 1 gate; T-4 moved there because `scroll` and `drag` are Phase 2 tools.

T-1 is the acceptance gate: [CANONICAL_SOURCES.md](../CANONICAL_SOURCES.md) forbids implementing a proposal before
tools.md adopts it, so every code task depends on T-1. A human reviews T-1.

## Traceability matrix

| AC | Tasks | Verification |
|----|-------|--------------|
| AC-1 | T-2 | dotnet test |
| AC-2 | T-3 | dotnet test (desktop gate) |
| AC-3 | T-3 | dotnet test (desktop gate) |
| AC-4 | T-9 | dotnet test (desktop gate) |
| AC-5 | T-4 | dotnet test |
| AC-6 | T-5 | dotnet test |
| AC-7 | T-6 | dotnet test |
| AC-8 | T-7 | dotnet test |
| AC-9 | T-8 | dotnet test |
| AC-10 | T-10 | dotnet test |
| AC-11 | T-11 | dotnet test |
| AC-12 | T-1 | review of docs diff |

## Task table

| ID | Task | Owner role | Files / areas | Depends on | ACs | Verification |
|----|------|------------|---------------|------------|-----|--------------|
| T-1 | Accept operator-gaps proposal into tools.md, errors.md and security.md with ADR-0002 | docs | docs/adr/0002-operator-gaps.md, docs/tools.md, docs/errors.md, docs/security.md, docs/proposals/operator-gaps-2026-11-26.md | - | AC-12 | Human review of the docs diff |
| T-2 | focus_window and FocusWindow callers report and verify the foreground window | backend | src/WinGuiMcp.Core/Platform/WindowEnumerator.cs, src/WinGuiMcp.Core/Tools/ToolService.cs, tests/WinGuiMcp.Tests/ActuationGuardTests.cs | T-1 | AC-1 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-3 | screenshot returns origin, scale, windowRect, captureMethod and occlusion | backend | src/WinGuiMcp.Core/Capture/WindowCapture.cs, src/WinGuiMcp.Core/Tools/ToolService.cs, tests/WinGuiMcp.Tests/McpResultContractTests.cs | T-1 | AC-2, AC-3 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-4 | Coordinate click, scroll and drag accept handle and verify the foreground | backend | src/WinGuiMcp.Core/Tools/ToolService.cs, src/WinGuiMcp/Tools/Phase1Tools.cs, src/WinGuiMcp.Core/Tools/ToolCatalogRegistry.cs, tests/WinGuiMcp.Tests/golden/tool-surface.json | T-1, T-2 | AC-5 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-5 | screenshot outputPath on the MCP tool, confined to an output root | backend | src/WinGuiMcp/Tools/Phase1Tools.cs, src/WinGuiMcp.Core/Tools/ToolService.cs, src/WinGuiMcp.Core/Capture/WindowCapture.cs, tests/WinGuiMcp.Tests/golden/tool-surface.json | T-1, T-3 | AC-6 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-6 | screenshot returns a perceptual frame hash | backend | src/WinGuiMcp.Core/Capture/WindowCapture.cs, src/WinGuiMcp.Core/Tools/ToolService.cs | T-1, T-3 | AC-7 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-7 | Document scroll amount units per path and report changed | backend | src/WinGuiMcp.Core/Action/PatternExecutor.cs, src/WinGuiMcp.Core/Action/InputSimulator.cs, src/WinGuiMcp.Core/Tools/ToolService.cs | T-6 | AC-8 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-8 | wait_for region-stable and region-changed conditions | backend | src/WinGuiMcp.Core/Tools/ToolService.cs, src/WinGuiMcp.Core/Tools/ToolCatalogRegistry.cs, tests/WinGuiMcp.Tests/WaitForRegressionTests.cs | T-6 | AC-9 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-9 | launch_app returns only windows created after the launch | backend | src/WinGuiMcp.Core/Platform/WindowEnumerator.cs, src/WinGuiMcp.Core/Platform/LaunchResult.cs, src/WinGuiMcp.Core/Tools/ToolService.cs | T-1, T-10 | AC-4 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj; Calculator and Notepad gate tests still pass |
| T-10 | Refuse input to protected windows unless allowSelf is set | backend | src/WinGuiMcp.Core/Platform/InputBoundary.cs, src/WinGuiMcp.Core/WinGuiCoreServiceCollectionExtensions.cs, src/WinGuiMcp.Core/Tools/ToolService.cs, src/WinGuiMcp.Core/Tools/ToolCatalogRegistry.cs, src/WinGuiMcp/Tools/Phase1Tools.cs, tests/WinGuiMcp.Tests/ActuationGuardTests.cs, tests/WinGuiMcp.Tests/golden/tool-surface.json | T-1 | AC-10 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |
| T-11 | list_windows reports rect, monitor, dpi and isCloaked | backend | src/WinGuiMcp.Core/Platform/WindowEnumerator.cs, src/WinGuiMcp.Core/Perception/UemModels.cs, src/WinGuiMcp.Core/Platform/NativeMethods.cs | T-1 | AC-11 | dotnet build WinGuiMcp.sln; dotnet test tests/WinGuiMcp.Tests/WinGuiMcp.Tests.csproj |

## Task notes (pattern to follow, risks)

- **T-1.** Decide, and record in the ADR: which side of the `region` drift changes; `occluded` flag vs.
  `Occluded` error; the hash algorithm and its distance threshold; whether `region-stable` (deferred by
  ADR-0001) is now accepted; the M6 error code and configuration; the output root for `outputPath` and a
  security.md rule for file-writing tools. Pattern: [ADR-0001](../adr/0001-core-library-and-scenarios.md)
  (adopted / changed / deferred table). Set the proposal's Status line.
- **T-2.** Pattern: `InputBoundary.CreateUipiBlocked` for a `ToolException` with a `details` dictionary.
  `hotkey`, `press_key` and `PatternExecutor` call `FocusWindow`, so they inherit the check. Coordinate
  with `wgui-q1qxw` (keyboard focus), which touches the same callers.
- **T-3.** Also satisfies `wgui-ramlc` AC-5; note it on that ticket. Pattern: `CaptureFileResult` for adding
  fields to a capture result. Occlusion test only on the `desktop` path (`WindowCapture.cs:110`).
- **T-4.** Pattern: the `handle` handling of `ToolService.Hotkey` / `PressKey`. Ref-based paths already
  focus; change only the `x,y` paths.
- **T-5.** Reuse `WindowCapture.CaptureToFile` and the parameter name `outputPath` from
  `WinGuiToolStepProvider`. The root check is new; reject before capturing.
- **T-6.** Hash computed in `WindowCapture` on the final (cropped, scaled) bitmap; pure function, unit-testable
  without a desktop.
- **T-7.** Risk: sending the wheel delta as separate notches changes behaviour for existing callers; only do it
  if T-1 accepts it.
- **T-8.** Pattern: existing `wait_for` conditions in `ToolService.WaitForAsync` and
  `WaitForRegressionTests`.
- **T-9.** Depends on T-10: it must never return a protected window. Risk: Calculator and Settings are matched through `ApplicationFrameHost`, which may already exist
  before launch; keep the Phase 1 gate tests green.
- **T-10.** Pattern: `InputBoundary.ThrowIfBlocked`, which every input path already calls.
- **T-11.** Pattern: the existing `WindowInfo` fields set in `WindowEnumerator.ListWindows`.
