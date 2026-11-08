# Proposal: scenario runner and application profiles

**Status:** Accepted with changes on 2026-11-07. See [ADR-0001](../adr/0001-core-library-and-scenarios.md) for what was adopted, changed or deferred; [scenarios.md](../scenarios.md), [tools.md](../tools.md) and [errors.md](../errors.md) are now normative. This file is kept as the original proposal (authored by the image-scoring-skills session, d606a64e).
**Date:** 2026-11-07
**Related:** [phases.md](../phases.md), [requirements/modules/observability.md](../requirements/modules/observability.md) (trajectory logging), [cross-project/DXO_BOUNDARIES_CONSTITUTION.md](../cross-project/DXO_BOUNDARIES_CONSTITUTION.md) §9 (application profiles), DxO counterpart `dxo-photolab-mcp/docs/photolab-automation/SCENARIOS-proposal.md`

## 1. Summary

Agents driving desktop apps repeat the same sequences of calls: capture, find a field, type, wait, read back, save, and verify on disk. Each call is a separate model turn, so a long edit takes hundreds of turns. Each turn is also another chance to skip a check.

This proposal adds a Playwright-style **scenario runner**. It's a command-line tool that executes declarative step files against one or more MCP servers (WinGuiMcp plus siblings such as `dxo-photolab`), checks guards and expectations at every step, and writes a trace. It adds **application profiles**, which are per-app configuration for windows, anchors, hazards and save procedures. It also makes three small changes to existing tools that the runner needs.

The model stays in the loop where judgement matters: a `checkpoint` step stops the run and hands state back to the agent or user, and the run resumes from that step.

## 2. Evidence

These findings come from one session driving Lightroom Desktop (cloud) and DxO PhotoLab 10.2 on 2026-11-07, using about 380 WinGuiMcp calls plus many `dxo_*` calls. Every failure was a skipped check, not a missing tool:

| What went wrong | Missing check | Runner feature |
|---|---|---|
| About 70 minutes of Lightroom edits were never saved. The screen showed them, but the app never wrote them | Persisted state wasn't read before reporting done | `persist` profile procedure plus `expect` on file content |
| A missed value field turned typed digits into shortcuts: the label changed blue to green, the rating 5 to 4, and Enter closed a panel | Nobody confirmed the field had focus before typing | `type` requires `expectFocus`, plus watched-region guards |
| Clicks dropped while a heavy mask redrew, so batched entries landed elsewhere | No settle between dependent steps | `wait` `region-stable` after each submit |
| A DxO local Temperature set to "5300" displayed 2440 K, because the slider uses an internal scale | The displayed value wasn't read back | `expect` on readback after every set |
| A mask list rearranged and a fixed-coordinate click selected the wrong mask | The selection wasn't verified | Anchors plus post-click `expect` |
| `screenshot` overflowed the tool result (373k to 4.1M characters) | No file output | `screenshot.outputPath` |
| `wait_for` had no condition for "the preview finished redrawing" | No pixel waits | `wait_for` `region-stable` and `region-changed` |

## 3. Users and stakeholders

- **Agents** using WinGuiMcp on apps with sparse UIA trees, such as Lightroom Desktop, or with heavy custom panels, such as DxO PhotoLab.
- **The user**, who reviews, approves and replays routine edits without supervising each click.
- **Sibling repos** (`dxo-photolab-mcp`), which supply domain steps and recipes but must not duplicate generic tools (constitution §4.3).

## 4. Non-goals

- No vision or OCR locators. Phase 3 owns those, and this proposal leaves room for them.
- No general scripting language. Scenarios are data: steps, variables and bounded loops. Arbitrary code runs only through `exec` of a reviewed script in the repo.
- No unattended destructive runs. Writes need explicit run-time authorization (§8).
- No `dxo_*` logic in this repo (constitution §4.2). DxO steps call the `dxo-photolab` server by name.
- No replacement for MCP tools. The runner is a client of the existing tools.

## 5. User stories

- As an agent, I want to run "set these five sliders, read each back, save, verify the file" as one call, so a routine edit costs one turn and can't skip a check.
- As an agent, I want a run to stop at a `checkpoint` with captures and measurements, so I make the judgement and then resume.
- As a user, I want every run to leave a trace (steps, timings, captures and a report), so I can see what happened, like a Playwright trace.
- As a maintainer, I want per-app hazards and save procedures in a profile, so every scenario for that app inherits them.
- As an agent, I want `--dry-run` to validate a scenario against the live servers' tool schemas without touching the desktop.

## 6. Design

### 6.1 Shape

```text
wingui-scenario run <file> [--profile <app>] [--var k=v ...] [--dry-run] [--step] [--allow-writes]
wingui-scenario resume <run-dir>
wingui-scenario draft --from-trajectory <log>      (stage S3)
```

- **Project:** a new .NET 10 console project, `src/WinGuiMcp.Scenarios`, using the `ModelContextProtocol` client SDK. It starts the servers named in a client config with the same shape as `dxo-photolab-mcp/config/mcp.dual-server.example.json`.
- **Not an MCP tool at first:** a tool that calls other servers' tools would break the composition rule (constitution §3). A `run_scenario` tool for single-server scenarios is a stage S3 option.
- **Exit codes:**

| Code | Meaning |
|---|---|
| 0 | Passed |
| 1 | An expectation failed |
| 2 | A guard was violated, so the run stopped before acting |
| 3 | Paused at a checkpoint |
| 4 | Invalid scenario |

### 6.2 Scenario format (JSON; YAML is an open question)

```json
{
  "scenario": "dxo-set-sky-exposure",
  "profile": "dxo-photolab",
  "servers": ["win-gui-mcp", "dxo-photolab"],
  "vars": { "image": "DSC_8752.NEF", "ev": -0.8 },
  "writes": ["sidecar:${image}"],
  "guards": ["foreground:dxo-photolab", "region-unchanged:ratingBar"],
  "steps": [
    { "call": "dxo-photolab/dxo_get_filmstrip_selection",
      "expect": { "$.activeImage": "${image}", "$.singleSelected": true } },
    { "call": "dxo-photolab/dxo_select_mask", "args": { "mask": "background" } },
    { "call": "dxo-photolab/dxo_set_local_slider", "args": { "slider": "Exposure", "value": "${ev}" },
      "expect": { "$.actualValue": "${ev}" } },
    { "wait": "region-stable", "anchor": "viewer", "settleMs": 800, "timeoutMs": 8000 },
    { "capture": "viewer", "as": "after.png" },
    { "exec": "measure-colour", "args": ["${capture:after.png}"], "as": "m" },
    { "checkpoint": "Sky now L=${m.sky.L}. Accept, or adjust ev and resume?" },
    { "use": "persist", "expect": { "file:${sidecarPath}": { "contains": "ExposureBias = ${ev}" } } }
  ]
}
```

### 6.3 Step kinds

| Step | Purpose |
|---|---|
| `call` | Calls `server/tool` with `args`. The result is stored for `expect` and later variables |
| `type` | Click or focus, then type, but only after `expectFocus` passes (§6.4) |
| `wait` | `wait_for` conditions, including the new `region-stable` and `region-changed` (§7) |
| `capture` | `screenshot` to the run directory through `outputPath` (§7), by anchor or region |
| `expect` | JSONPath, file-content and pixel assertions, with tolerances for numbers |
| `exec` | Runs an allow-listed script from the profile, with captures as inputs. JSON on stdout becomes variables |
| `checkpoint` | Saves state and exits with code 3. `resume` continues from the next step |
| `use` | Inlines a named fragment from the profile, such as `persist`, `openMask` or `saveAndVerify` |
| `loop` | A bounded `until` loop (`max` is required), for closed-loop tuning such as adjusting a value until a metric is within tolerance |

### 6.4 Guards, checked before every acting step

- **`foreground:<profile>`:** the foreground process and window match the profile. Otherwise the run stops with code 2 and captures a diagnostic bundle (the Phase 2 diagnostic bundle in [phases.md](../phases.md)).
- **`expectFocus`** on `type`: the focused element or foreground class must match. For Lightroom Desktop, a value field opens a `NonActivateWindow` popup in the Lightroom process. A dropped click then fails the guard instead of sending digits to the app.
- **`region-unchanged:<anchor>`:** a hash of a watched region, such as the rating and label bar, must match its baseline. This catches stray shortcuts within one step.
- **`window-rect`:** if the window moved or resized, anchors are recomputed or the run stops.

### 6.5 Application profiles (constitution §9)

`profiles/<app>.profile.json`:

- **Window match:** process, class and a title pattern.
- **Anchors:** named rectangles relative to the window, each with an optional verification capture. Coordinates are fallbacks; UIA refs win when they exist.
- **Hazards:** for example, digits are rating and label shortcuts, `Escape` with no field open changes the view, or a button is a toggle and not a display switch.
- **Fragments:** reusable step lists such as `persist`, `openValueField`, `selectMask` and `readBack`.
- **Allow-listed `exec` scripts:** paths inside the repo only.

Proposed sample profiles:
- **`lightroom-desktop`:** Lightroom has no domain server, so its profile lives here.
- **`dxo-photolab`:** generic UI anchors only. Its domain steps call `dxo_*` tools that live in the dxo repo.

### 6.6 Trace and artifacts

Each run creates `runs/<scenario>-<utc>/` containing:

- `report.json`: steps, arguments with refs only, results, timings and guard outcomes.
- Captures, named by step.
- Diagnostic bundles on failure.
- `state.json` for resume.

Like the trajectory log (TR-P2-MOD-Observability-001), the trace isn't sent to the agent by default. The agent gets a compact summary and the run directory path.

## 7. Changes to existing tools

These are listed for review. Each needs a matching update to `tools.md` and `requirements/tools/*.md` when accepted.

| Tool | Change | Why |
|---|---|---|
| `screenshot` | Optional `outputPath`: write a PNG and return `{path, width, height, sha256}` with no inline base64 | Full-window and 1:1 region captures overflow tool results |
| `wait_for` | New conditions: `region-stable` (pixel hash unchanged for `settleMs`), `region-changed` (differs from the baseline at call start), with `region` and `handle` | Previews and masks redraw without UIA events |
| `wait_for` | New `foreground-class` condition, with `target` as class and optional process | Confirms a value popup opened before typing |

## 8. Safety

- **Writes:** scenarios declare what they write (`writes:`). Without `--allow-writes`, write steps run as dry runs or stop at a checkpoint.
- **Allowed calls:** the runner refuses servers that aren't in the config, tools that aren't in their `tools/list`, and `exec` scripts the profile doesn't list.
- **Bounded:** no `force` or overwrite flags by default, and every `loop` has a `max`.
- **No secrets:** traces store refs and parameters, never clipboard contents or credentials.
- **Rules still apply:** [security.md](../security.md) and `.agent/SAFETY.md` stay in force. This proposal changes nothing about UIPI or elevation.

## 9. Acceptance criteria

1. Given a scenario calling a tool not in a server's `tools/list`, when it runs with `--dry-run`, then it exits 4 and names the step, without touching the desktop.
2. Given a `type` step whose `expectFocus` doesn't match, when it runs, then no keystrokes are sent, the exit code is 2, and a diagnostic bundle exists in the run directory.
3. Given a watched region that changes during a step, when the step completes, then the run stops with code 2 and records the region's before and after hashes.
4. Given a `checkpoint`, when it's reached, then the exit code is 3. `resume` then continues from the next step with the same variables, and doesn't repeat earlier write steps.
5. Given `screenshot` with `outputPath`, when called on a 1:1 3840-pixel-wide region, then the result has no base64, and the file exists with the reported size and SHA-256.
6. Given `wait_for` `region-stable` on a region that keeps changing, when `timeoutMs` elapses, then it returns the documented timeout error.
7. The Phase 1 Notepad and WPF sample gate flows run as scenarios with no coordinate clicks, and report 0.
8. A `lightroom-desktop` sample scenario sets two global sliders, saves through the profile's `persist` fragment, and passes an `expect` on the saved settings file. This is a manual gate, because desktop tests are excluded from headless CI.

## 10. Stages

| Stage | Scope | Gate |
|---|---|---|
| S0 | §7 tool changes | AC 5 and 6, plus unit tests for hashing and timeouts |
| S1 | Runner MVP: `call`, `type`, `wait`, `capture`, `expect`, `checkpoint`; JSON format; trace; dry run; exit codes | AC 1–4 and 7 |
| S2 | Profiles, anchors, fragments, `exec`, `loop`; the `lightroom-desktop` and `dxo-photolab` samples | AC 8 |
| S3 | `draft` from trajectory logs (codegen equivalent); optional single-server `run_scenario` tool | Draft replays the Notepad gate |

## 11. Open questions

1. **Format:** JSON only, using `System.Text.Json`, or YAML too, which adds YamlDotNet?
2. **Packaging:** should the runner be a separate executable, or a `wingui scenario` subcommand of the server binary?
3. **Resume:** should it restart the process and re-validate guards (proposed), or keep a live session?
4. **Pixel hashing:** exact, or tolerant (perceptual hash with a threshold)? Exact is simpler but trips on cursor blink and animations.
5. **Lightroom Desktop:** confirm its profile lives here, since it has no domain server.
