# ADR-0002: Operator gaps from a GPU-rendered app session

**Status:** Accepted, 2026-11-26
**Accepts with changes:** [proposals/operator-gaps-2026-11-26.md](../proposals/operator-gaps-2026-11-26.md)
**Tasks:** [proposals/operator-gaps-2026-11-26.tasks.md](../proposals/operator-gaps-2026-11-26.tasks.md), epic `wgui-ab2hb`

## Context

One session drove Synth Term, a GPU-rendered app with no UIA tree below the window, through about 300 calls on a
4K display. Coordinate actions went to the wrong window after the foreground was stolen, screenshots could not be
mapped back to screen coordinates, a covered window was captured as whatever covered it, `launch_app` returned the
agent's own terminal, and every "did it change" check compared whole base64 strings. The proposal lists each gap
with its source location at 3ebd8b4.

## Decision

1. **The contract grows; implementation follows per ticket.** [tools.md](../tools.md), [errors.md](../errors.md)
   and [security.md](../security.md) now specify every change below. Each new field or parameter is marked
   *ADR-0002* there with the ticket that implements it; until that ticket is done, the server does not return it.
2. **Live schemas win over stale docs.** tools.md now documents `screenshot` with flat
   `regionX/regionY/regionWidth/regionHeight` and `click` with flat `x`, `y`, as the server, the golden
   tool-surface test, the scenario runner and the usage skill already do. No client changes.
3. **Foreground is reported, not assumed.** `focus_window` returns `foregroundHwnd` and `foregroundTitle`. When a
   switch was performed, the server checks the foreground again after 100 ms and fails with `FocusFailed` if it
   moved. Every tool that focuses first (`hotkey`, `press_key`, ref actions, coordinate actions with `handle`)
   inherits the check.
4. **Coordinates become mappable.** `screenshot` returns `origin`, `scale`, `captureMethod`, `windowRect` (handle
   captures), `occluded` and `frameHash`.
5. **Coordinate actions can be pinned to a window.** `click`, `scroll` and `drag` accept `handle` on their `x,y`
   forms with the same focus contract as `hotkey`.
6. **`outputPath` reaches the MCP tool**, confined to a capture root. Scenario `screenshot` steps get the same confinement.
7. **`wait_for` gains `region-stable` and `region-changed`**, which ADR-0001 deferred. Two sessions (DxO PhotoLab,
   Synth Term) needed a pixel-settle wait.
8. **The server's host windows are protected.** Input to them fails with `TargetNotAllowed` unless the call passes
   `allowSelf: true`.
9. **`launch_app` prefers windows created by the launch**, and `list_windows` reports geometry.

## Changes from the proposal

*Decided by:* **user** means the owner chose it when asked on 2026-11-26. **author** means the ADR author chose it
and the human reviewer of `wgui-pznrt` may change it.

| Proposal | Decision | Why | Decided by |
|---|---|---|---|
| §4: tools.md `screenshot` `region` drift | Document the live flat `regionX/Y/Width/Height` | No client change; code, golden test, scenarios and skill agree | user |
| (not in proposal) tools.md `click` `point` object | Document the live flat `x`, `y` | Same drift and the same reasoning as `region` | author — open to review |
| B1: `foreground: {handle,title}` | `foregroundHwnd`, `foregroundTitle` on the result; errors already carry the same names in `details` (`Phase1Tools.Execute`) | Reuse existing names | author — open to review |
| B1 option: re-check after a settle | Adopted: 100 ms, only when a switch was performed | Focus stolen right after the switch was the likely cause; the already-foreground path stays fast | author — open to review |
| B2: guard `inspect_at` properties | Not decided here; tracked by `wgui-ramlc` AC-6 | Already filed | — |
| B4: `occluded: true` *or* an `Occluded` error | `occluded` flag only; no new error code | A covered capture is still evidence; the agent decides | author — open to review |
| M1: `move_pointer` | Not decided here; tracked by `wgui-ramlc` AC-1 | Already filed | — |
| M2: `handle` on coordinate actions | Adopted; `handle` together with a `ref` from another window is `InvalidArgument` | A silent mismatch would hide the very mistake M2 targets | author — open to review |
| M3: `savePath` | `outputPath`, the name the scenario provider already uses | One name for one behaviour | author — open to review |
| M3: "an allowed root" | `%LOCALAPPDATA%\WinGuiMcp\captures`, overridable with `WINGUI_CAPTURE_ROOT` | Same convention as `WINGUI_SCENARIO_RUNS` | user |
| M3: file rules | `.png` only; an existing file is overwritten | Keeps the root to captures; repeat runs reuse names | author — open to review |
| M3: scenario steps | Confined like the MCP tool: `.png` only; relative paths resolve in the run folder as before and may not escape it; an absolute path must lie inside the capture root, else `InvalidArgument` and nothing is written (`wgui-bbf78`) | `screenshot` is a read-only step, so `run_scenario` never asks for `allowWrites` before a screenshot writes a file (`ScenarioEngine.cs` checks `allowWrites` only for non-read-only steps). The earlier "accepted exception" rested on that wrong premise | owner, 2026-11-28 (after independent review; Jev recommendation `confine_like_mcp` p 0.97) |
| M4: dHash | 256-bit dHash (17×16 grayscale), 64 hex chars; Hamming distance ≤ 3 counts as unchanged | Fine enough for one-notch scrolls, coarse enough for a blinking caret. `wgui-4mx5e` verifies both and may tune the size or threshold; it then updates tools.md | author — open to review |
| M4: `wait_for region-stable` / `region-changed` | Accepted (ADR-0001 had deferred them) | Two sessions needed a pixel-settle wait | user |
| M5: document units | Adopted, and the wheel path sends `amount` separate 120-unit inputs instead of one large delta (**behaviour change**) | Makes "one unit = one notch" true; some apps handle one large delta differently | author — open to review |
| M6: `protectedWindows` or a `hostWindow` hint | Both: windows of the server's ancestor processes, plus `WINGUI_PROTECTED_WINDOWS` (HWNDs or process names) | Covers the default case with no configuration | user |
| M6: which ancestor windows | Only those that exist **at server start**; the walk stops at `explorer.exe` or another session | In the session the agent's terminal and the target were windows of the *same* single-instance process; protecting the whole process would block the task. Stopping at `explorer.exe` keeps File Explorer flows usable | author — open to review (refines the user's choice) |
| M6: new error | `TargetNotAllowed` (existing) | Same meaning as the scenario target guard | author — open to review |
| M6: where `allowSelf` applies | All 13 actuating tools (listed in tools.md). Keyboard input without a target (`type_text` without `ref`, `hotkey`/`press_key` without `handle`) is checked against the foreground window | Any of them can hit the host window; target-less keyboard input goes to the foreground, the likeliest way to type into the host terminal | author; target-less keyboard rule: owner, 2026-11-28 (after independent review; Jev recommendation `guard_foreground` p 1.0) |
| B5: return only new windows; `Timeout` if none | New windows first; after `waitForWindowMs` with none, an existing window of the expected process with `isNew: false` (never a protected window); else `Timeout` | Windows 11 Notepad can open a tab in an existing window; a hard `Timeout` would break the Phase 1 gate. Cost: such launches wait the full timeout | author — open to review |
| §4: usage-skill fixes | Out of scope | The skill is user-level, not in this repo | author — open to review |
| §5: app-profile `hazards` | Deferred | No normative profile format exists ([DXO_BOUNDARIES_CONSTITUTION.md](../cross-project/DXO_BOUNDARIES_CONSTITUTION.md) §9) | author — open to review |
| (not in proposal) phase placement | **Phase 1 hardening** (defined in [phases.md](../phases.md#phase-1-hardening)): `wgui-0vgx7` (B1), `wgui-cy6wh` (B3/B4), `wgui-tjexv` (B5), `wgui-sl5an` (M6), `wgui-bbf78` (scenario `outputPath`). **Phase 2:** `wgui-xs15j` (M2; `scroll` and `drag` are Phase 2 tools), `wgui-7zzf7` (M3), `wgui-4mx5e` (M4 hash), `wgui-xm3lj` (M5), `wgui-2x3n5` (region waits), `wgui-mquv0` (M7) | Hardening fixes existing Phase 1 tools that misreport or mis-target; the rest are new capabilities, and AGENTS.md puts Phase 2 work after the Phase 1 gate (currently partial) | Jev `split` (p 0.91, confidence 0.88), delegated by the user; tier defined in phases.md and `wgui-xs15j` moved to Phase 2: owner, 2026-11-28 (after independent review; Jev recommendation `amend_phases_move_xs15j` p 0.96) |

## Consequences

- tools.md describes fields the server does not return yet. Each is marked with its ticket; agents must not rely on
  a marked field until that ticket is done.
- Every tool-surface change updates `tests/WinGuiMcp.Tests/golden/tool-surface.json` in the same PR.
- `region-changed` and `scroll.changed` cost one capture before and one after. `changed` is `null` when no
  capture was possible.
- The protected set is computed once per server process. A host window opened after the server started is not
  protected; list it in `WINGUI_PROTECTED_WINDOWS` if needed.
- `launch_app` for an app that reuses its window now waits the full `waitForWindowMs` before returning that window.
- Phase 2 tickets carry the label `phase:2` and, per AGENTS.md, wait for the Phase 1 gate; hardening tickets carry
  `phase:1-hardening`. The gate itself has no ticket yet.
- `docs/requirements/` still lists the Phase 1 field sets as minimums. Each implementing ticket adds or adjusts the
  matching `TR-*` entry; this ADR only aligns the `region` wording there.
