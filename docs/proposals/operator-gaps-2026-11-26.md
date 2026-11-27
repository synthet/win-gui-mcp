# Proposal: operator gaps found driving a GPU-rendered app (Synth Term)

**Status:** Accepted with changes on 2026-11-26. See [ADR-0002](../adr/0002-operator-gaps.md) for what was adopted, changed or deferred; [tools.md](../tools.md), [errors.md](../errors.md) and [security.md](../security.md) are now normative. This file is the proposal as revised after the 2026-11-26 source check; ADR-0002 records what changed on acceptance.
**Date:** 2026-11-26
**Related:** [tools.md](../tools.md), [errors.md](../errors.md), [scenario-runner.md](scenario-runner.md), user skill `win-gui-mcp-usage`

## 1. Summary

One session took 81 screenshots of every window, panel, menu and settings page of Synth Term (a WarpUI/GPU
app with no UIA tree below the window) on a 3840×2160 display, using about 300 win-gui-mcp calls. It worked, but
only through workarounds: an out-of-band PowerShell `GetWindowRect`, client-side base64 decoding, manual
image-equality loops to detect scroll end, and repeated refocusing.

**Evidence vs. source.** The *Evidence* column is what the session observed; it can't be reproduced from code
alone. The *Source* column was checked against `src/` at 3ebd8b4 on 2026-11-26 for B1–B5 and M1–M5 and M7; M6 was
checked only as far as the existing guards go. That pass corrected B1, B3, B4, B5, M1, M2, M3, M4 and M5 (the
first draft overstated or misplaced them); the corrections are inline. Statements marked *inference* or
*option* are not from the code.

**Overlap with tickets.** Several rows are already filed from the DxO PhotoLab session of 2026-11-22:
`wgui-ramlc` covers M1 (item 1 / AC-1), B3 (item 6 / AC-5) and B2 (item 7 / AC-6), and its item 8
(`ScrollPattern.SetScrollPercent`) touches M5; `wgui-q1qxw` (verify keyboard focus before sending keys) is
the keyboard-side twin of M2. New tickets should reference those rather than duplicate them.

## 2. Defects

B2 and B5 contradict [tools.md](../tools.md). B1 works as documented but checks too little. B3 and B4 match tools.md but contradict the user skill
`win-gui-mcp-usage` (SKILL.md lines 50–55 promise `origin` and `scale` and give the image→screen formula),
so they are contract gaps: the fix is to extend tools.md, and until then to correct the skill.

| # | Gap | Evidence (session) | Source (3ebd8b4) | Fix |
|---|---|---|---|---|
| B1 | `focus_window` success is one instantaneous check and names no foreground window | Returned `focused: true` while Chrome stayed in front; later captures showed Chrome. Cause unconfirmed: focus was lost after the check, or the handle was not Synth Term's (see B5) | `ToolService.cs:71` returns a literal `focused = true`, but only after `WindowEnumerator.FocusWindow` (`WindowEnumerator.cs:65`) passed: it returns early when the target is already foreground (`:74`) and throws `FocusFailed` when `GetForegroundWindow() != handle` right after `SetForegroundWindow` (`:97`). No later re-check; the foreground window is never reported. `hotkey`, `press_key` and ref actions in `PatternExecutor` (`:73`, `:270`) call the same method | Read `GetForegroundWindow()` after the attempt; return `focused` truthfully and `foreground: {handle,title}`; `FocusFailed` when it differs. Put it in `WindowEnumerator.FocusWindow` so every caller benefits. *Option:* a second check after a short settle, to catch focus stolen right after the switch |
| B2 | `inspect_at` throws `InternalError` on GPU apps | "The requested property 'AutomationId [#30011]' is not supported" | `ToolService.InspectAt` (`ToolService.cs:421–433`) reads all six properties unguarded; `AutomationId` (`:430`) is the one observed | Read each property through a guarded getter (the pattern of `UiaSnapshotBuilder.TryGetString`, `UiaSnapshotBuilder.cs:494`, which is private there); return `null` for unsupported ones. Already `wgui-ramlc` AC-6 |
| B3 | `screenshot` metadata lacks `origin` and `scale` | Mapping image px → screen px needed a PowerShell `GetWindowRect` (and `.ps1` files are blocked by execution policy) | `ToolService.cs:352–359` returns `ok, imageBase64, mimeType, width, height` — what tools.md documents. `Phase1Tools.Screenshot` (`Phase1Tools.cs:164–189`) passes it through `McpResults.Create`, which emits one JSON text block plus the same JSON as `structuredContent` (`McpResults.cs`). There is **no MCP image block**, contrary to SKILL.md line 50 | Return `origin {x,y}` (physical), `scale`, `windowRect`, `captureMethod` (see B4). Already `wgui-ramlc` AC-5 (full-screen case); extend to handle captures |
| B4 | Handle capture silently returns *what is on screen* when the window is covered | `screenshot(handle)` of Synth Term returned a Chrome page | `WindowCapture.CaptureBitmap` (`WindowCapture.cs:79`) tries WGC (`:89`), then `PrintWindow(PW_RENDERFULLCONTENT)` (`:102`, `:136`); only when both are blank (`IsBlankBitmap`) does it crop the desktop at the window rect (`:110`). Which method ran is not reported | PrintWindow is already preferred — no change there. Report `captureMethod` (`wgc` / `printWindow` / `desktop`); on the `desktop` path, test occlusion (e.g. `WindowFromPoint` samples over the rect) and return `occluded: true`, or fail with a new `Occluded` code |
| B5 | `launch_app` on a single-instance app returns a pre-existing window | Returned the handle of the agent's *own* terminal window (`ok:true`); the real new window appeared 2 s later | `FindLaunchedWindow` (`WindowEnumerator.cs:188`) tries the launcher pid, then **any window whose process name matches** (`:201`, foreground first, then highest handle), then a title hint. *Inference:* a single-instance launcher hands off and exits, so the pid branch misses and the process-name branch picks an existing window | Snapshot the window set before launch; accept only windows created after it; add `isNew`; `Timeout` if none |

## 3. Missing capabilities

| # | Gap | Evidence (session) | Source note | Proposal |
|---|---|---|---|---|
| M1 | No pointer **hover/move** tool | "New worktree config" submenu and tooltips only open on hover; a click highlighted but did not expand | No such tool in the catalog or tools.md. `InputSimulator.MoveTo` (`InputSimulator.cs:31`) already exists internally. Already `wgui-ramlc` AC-1 | `move_pointer {x,y \| ref, dwellMs}` |
| M2 | Coordinate `click` / `scroll` / `drag` take no `handle` | A Claude Code permission prompt stole the foreground mid-run; coordinate clicks silently went nowhere (the window rendered dimmed) | Ref-based pointer fallbacks already focus the ref's window first (`PatternExecutor.cs:73`, `:270`). The `x,y` path of `click` (`ToolService.cs:222–225`) goes straight to `InputSimulator.ClickAt`, which checks only UIPI and the secure desktop for the window under the point (`InputSimulator.cs:10–14`), not the foreground | Optional `handle` on the `x,y` forms: focus, verify foreground (B1), then act; `FocusFailed` and no input otherwise — the contract `hotkey` and `press_key` have with `handle`. Pointer-side twin of `wgui-q1qxw` |
| M3 | Screenshots only as inline base64 on the MCP tool | A single 0.4-scale 4K capture was ~325 k chars of JSON; every caller decodes by hand | `WindowCapture.CaptureToFile` (`WindowCapture.cs:46`) exists and scenario steps already accept `screenshot.outputPath` (`WinGuiToolStepProvider.cs:65`, [scenarios.md](../scenarios.md)); [ADR-0001](../adr/0001-core-library-and-scenarios.md) kept the tool contract unchanged "until tools.md adopts it". `CaptureToFile` calls `Path.GetFullPath` (`:59`) with no root check | Note: because of the text block plus `structuredContent` (B3), the base64 is sent twice per call. Adopt the existing name `outputPath` on the MCP tool (not a new `savePath`); return path + `sha256` + size; image block optional. **New requirement:** a configured output root, with paths outside it rejected as `InvalidArgument`. [security.md](../security.md) has no rule for file-writing tools yet |
| M4 | No "did anything change" signal | Scroll-to-end detection compared whole base64 strings; a blinking caret makes identical states differ | `wait_for region-stable` / `region-changed` were proposed in [scenario-runner.md](scenario-runner.md) §7 but **deferred** by ADR-0001; this re-opens them with new evidence | `screenshot` returns a perceptual hash (dHash) of the frame; then `wait_for region-stable` |
| M5 | `scroll` gives no proof of effect and `amount` units are undocumented | 15 notches skipped rows; 8/5 worked; a settings list kept its scroll position from an earlier visit | The two paths use different units. The ScrollPattern path calls `Scroll(SmallIncrement)` `amount` times (`PatternExecutor.cs:257`). The wheel fallback sends **one** input with delta `amount × WHEEL_DELTA` (120) (`InputSimulator.cs:89`, `:102`). Hypothesis, unverified: some apps handle one large delta differently from `amount` separate notches, which could explain the skipped rows. tools.md says only `amount: number` | Document units (wheel notches, 120 delta each) per path; return `changed` (hash before/after, M4). *Option:* send the wheel delta as separate 120-unit inputs |
| M6 | Self-window guard | The agent's own terminal is a window of the same app it was asked to drive | No such guard exists. `TargetGuard` (`Scenarios/Guards.cs:101`) applies only to scenario steps (`WinGuiToolStepProvider.cs:90`, `:108`). `InputBoundary.ThrowIfBlocked`, which every input path already calls, checks integrity and the secure desktop — a natural home for this check | Server option `protectedWindows` (pid/handle) or a `hostWindow` hint; input to it requires `allowSelf: true` |
| M7 | `list_windows` has no bounds or monitor | Needed for layout and for M2/B3 | Confirmed: `WindowInfo` carries handle, title, processName, pid, isForeground, isMinimized only (`WindowEnumerator.cs` `ListWindows`) | Add `rect`, `monitor`, `dpi`, `isCloaked` (virtual-desktop windows vanished from the list mid-run) |

## 4. Documentation / discoverability

- `set_window_state` exists (Phase1Tools) but the agent never found it: a client-side tool search capped at 25
  results hid it, so the window stayed maximized at 4K. Add it to the usage skill's loop ("pin size and position
  first"). `search_tools` already maps `resize_window` and `move_window` to it (`ToolCatalogRegistry.cs:118–119`).
- tools.md `screenshot` still documents a nested `region` object; the live schema (catalog and `Phase1Tools`)
  uses `regionX/Y/Width/Height`. AGENTS.md makes tools.md the schema authority, so which side changes is a
  decision, not a typo fix.
- errors.md: add `Occluded` (B4). Note that `focused: true` means "foreground at the moment of the check", not
  "still foreground" (B1).
- Usage skill: SKILL.md line 50 promises an image block and `origin`/`scale` that the server does not return (B3).
  Line 25 says to pass `handle` to `type_text`, but `type_text` has no `handle` parameter (`Phase1Tools.cs:128–135`);
  only `hotkey` and `press_key` take one.

## 5. Not tool gaps (app-specific, belong in app profiles)

- WarpUI popup menus ignore `Escape`; dismiss with a click on an empty area.
- Menus with destructive items (code review ⋮ → "Discard all") sit one pixel row from benign ones; an app profile
  `hazards` entry should mark them. Note: `hazards` comes from [scenario-runner.md](scenario-runner.md) §app
  profiles. ADR-0001 did not adopt general app profiles, and
  [DXO_BOUNDARIES_CONSTITUTION.md](../cross-project/DXO_BOUNDARIES_CONSTITUTION.md) §9 treats them as intent
  until `docs/application-profiles.md` exists, so there is no normative place for this yet.

## 6. Suggested order

Revised after the source check:

1. **B1, B2** — small, local fixes (one shared method; one guarded getter).
2. **B3 + B4** — same response object; `captureMethod` serves both.
3. **M2** — needs B1's verified foreground.
4. **M3** — mostly exposing existing Core code; the output-root rule is the real work.
5. **M4**, then **M5** (`changed` depends on M4's hash).
6. **B5, M6** (safety), then **M1** (already `wgui-ramlc`), **M7**.

## 7. Acceptance criteria (if accepted)

B2 and M1 are already tracked by `wgui-ramlc` (AC-6 and AC-1) and are not repeated here. Task breakdown:
[operator-gaps-2026-11-26.tasks.md](operator-gaps-2026-11-26.tasks.md).

- **AC-1**: WHEN `focus_window` (or any tool that focuses a window first) ends with another window in the foreground THEN it fails with `FocusFailed` and `details.foregroundHwnd` / `foregroundTitle` name that window, including when the foreground moves within 100 ms of a switch; WHEN it succeeds THEN the result's `foregroundHwnd` is the target.
- **AC-2**: WHEN `screenshot` succeeds THEN the result includes `origin {x,y}` (physical px), `scale` and `captureMethod` (plus `windowRect` for handle captures), and mapping a known element's image-space center with `origin + px / scale` lands within 2 px of its UIA bbox center.
- **AC-3**: WHEN `screenshot(handle)` falls through to the `desktop` method while another window covers the target THEN the result has `occluded: true`.
- **AC-4**: WHEN `launch_app` runs while a window of the same process already exists and a new window appears THEN the returned handle was not in the pre-launch window set and `isNew: true`; WHEN no new window appears within `waitForWindowMs` THEN it returns an existing window of that process with `isNew: false` (never a protected window) or fails with `Timeout`.
- **AC-5**: WHEN a coordinate `click`, `scroll` or `drag` is given `handle` and another window holds the foreground after focusing THEN it fails with `FocusFailed` and sends no input.
- **AC-6**: WHEN `screenshot` gets an `outputPath` inside the configured output root THEN it writes the PNG and returns path, `sha256` and size without base64; WHEN the path is outside the root THEN it fails with `InvalidArgument` and writes nothing.
- **AC-7**: WHEN `screenshot` succeeds THEN the result includes `frameHash`; two captures of an unchanged window (including a blinking caret) are within the documented distance, and a one-notch scroll in Notepad is not.
- **AC-8**: WHEN `scroll` completes THEN the result reports `changed` (frame hash before vs. after), and tools.md documents what one unit of `amount` means on each path.
- **AC-9**: WHEN `wait_for region-stable` watches a region that keeps changing THEN it fails with `Timeout` after `timeoutMs`; WHEN the region is unchanged for `settleMs` THEN it returns `ok: true`.
- **AC-10**: WHEN an input tool targets a protected window (the server's host window or a `WINGUI_PROTECTED_WINDOWS` entry) without `allowSelf: true` THEN it fails with `TargetNotAllowed` and sends no input.
- **AC-11**: WHEN `list_windows` returns THEN every window has `rect`, `monitor`, `dpi` and `isCloaked`.
- **AC-12**: WHEN tools.md, errors.md and security.md are read THEN they document every field, parameter and error code named in AC-1…AC-11, the `screenshot` region schema drift is resolved, and an ADR records what was accepted, changed or deferred.

**Non-goals:** vision/OCR fallback, app-specific profiles, elevation or UIPI changes, and any change to the
UIA-patterns-first order of actuation. The user-level `win-gui-mcp-usage` skill is outside this repository;
its corrections (§4) are not tracked here.
