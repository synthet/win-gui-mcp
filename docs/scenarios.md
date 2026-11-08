# Scenarios

A scenario is a declarative step file that WinGuiMcp runs as one call, like a Playwright test. The point is semi-automation: routine steps run unattended with guards and read-backs, and `checkpoint` steps hand control back to the agent or the user wherever judgement is needed. Design record: [ADR-0001](adr/0001-core-library-and-scenarios.md). Schema for editors: [`schemas/scenario.schema.json`](../schemas/scenario.schema.json).

## Running

| Surface | Command |
|---|---|
| MCP | `run_scenario { scenario, params?, dryRun?, allowWrites? }`, `resume_scenario { resumeToken, approve?, secretParams? }`, `validate_scenario`, `list_scenarios` |
| CLI | `WinGuiMcp scenario run <file\|name> [--param k=v]... [--dry-run] [--allow-writes]` |
| | `WinGuiMcp scenario resume <token> [--reject] [--param secret=v]` |
| | `WinGuiMcp scenario validate <file> [--param k=v]...` |
| | `WinGuiMcp scenario list` |

Names resolve under the scenario roots: `./scenarios` and `<exe>/scenarios`. `--param` values parse as JSON when they look like JSON (`{...}`, `[...]`, numbers, `true`/`false`).

| Exit code / status | Meaning |
|---|---|
| 0 `passed` | Every step ran and every assertion held |
| 1 `failed` / `aborted` | A step failed, an assertion or `expect` was false, or a checkpoint was rejected |
| 2 `blocked` | A guard stopped the run before acting: `WritesNotAllowed`, `TargetNotAllowed`, `GuardFailed` |
| 3 `paused` | Stopped at a `checkpoint`; resume with the returned `resumeToken` |
| 4 `invalid` | Structure, param or unknown-step error; nothing ran |

Every run writes `receipt.json`, plus any captures, to `%LOCALAPPDATA%\WinGuiMcp\runs\<runId>\` (override with `WINGUI_SCENARIO_RUNS`). The receipt records each step's interpolated arguments, outputs (truncated, with images stripped), timing and status.

## File format

```yaml
scenario: notepad-roundtrip         # required name
version: 1
description: ...
target: { processes: [notepad.exe] }  # required for any step that actuates a window
params:
  file: { type: string, required: true, description: ... }
  ev:   { type: number, default: 0.2, min: -4, max: 4 }
  token: { type: string, secret: true }   # never written to receipts or pause state
writes: ["notepad:${params.file}"]  # documentation of what the run changes
on_error: { screenshot: true }      # PNG of the screen into the run folder on failure
steps:
  - id: launch                      # optional; outputs are reachable as steps.launch.output
    launch_app: { path: notepad.exe, args: "\"${params.file}\"" }
    save: { hwnd: $.handle }        # variable ← JSON path into the step result
  - type_text: { ref: "${doc}", text: "${params.text}" }
    guard: { process: notepad.exe, titleLike: "wingui-scenario", waitMs: 2000 }
    retry: { attempts: 3, backoffMs: [200, 500] }
  - checkpoint: { message: "Close without saving?" }
```

Each step has exactly one **action key** plus optional modifiers.

### Actions

| Action | Meaning |
|---|---|
| any win-gui tool | `click`, `find_element`, `wait_for`, `screenshot`, ... ([tools.md](tools.md)). `screenshot` with `outputPath` writes a PNG (relative paths go in the run folder) instead of base64 |
| `ns.step` | A sibling provider's step, e.g. `lr.set_values` in lightroom-mcp |
| `set` | `{ name: value-or-${expr} }` assigns variables |
| `assert` | `"expr"` or `{ expression, message }`. Not evaluated in a dry run |
| `wait_until` | `{ do: { <one step> }, until: "expr", timeoutMs?, intervalMs? }` repeats the step until the expression holds; the outer `save` applies on each try |
| `checkpoint` | `"message"` or `{ message }`. Pauses with a single-use resume token. Only allowed in the top-level scenario |
| `call` | `{ scenario: relative/path, params: {...} }` runs a sub-scenario inline; `save` reads its variables |
| `log` / `fail` | Write a message to the receipt, or fail the run with one |

### Modifiers

| Modifier | Meaning |
|---|---|
| `id` | Names the step for `steps.<id>.output` |
| `save` | `{ var: $.path }`. Paths: `$`, `$.a.b`, `$.list[0]`, `$.list[-1]`. **Quote paths that contain `[` inside flow mappings**: `save: { first: '$.items[0]' }` |
| `expect` | Expression checked against `result` (the step output), e.g. `"result.text == 'ok'"` |
| `retry` | `{ attempts, backoffMs: n \| [n, ...] }`. Guard violations are never retried |
| `when` | Expression; the step is skipped when false |
| `guard` | Foreground check before acting: `{ process?, class?, titleLike? (regex), handle?, waitMs? }`. Fails with `GuardFailed`, and nothing is sent |
| `continueOnError` | `true` records a failure and keeps going |

### Expressions and interpolation

Inside `${...}`, `when`, `assert`, `expect` and `until`, you can use:

- **Literals:** numbers, `'text'` or `"text"`, `true`, `false`, `null`.
- **Paths:** `name`, `a.b`, `a[0]`, `params.x`, `steps.id.output.y`, `steps['my-id']`.
- **Operators:** `|| && ! == != < <= > >= + - * /` and parentheses.
- **Functions:** `abs`, `round(x[, n])`, `min`, `max`, `len`, `contains`, `startsWith`, `number`, `string`.

There is no other code execution. A string that is exactly one `${expr}` keeps the value's type (number, object), while text with embedded placeholders becomes a string.

## Safety

- **Writes need `allowWrites`.** Any step whose tool isn't read-only stops the run with `WritesNotAllowed` (exit 2) unless the run was started with `allowWrites` / `--allow-writes`.
- **Dry run never actuates.** Write steps run with the provider's dry-run path, assertions and checkpoints are skipped, and saved variables become `<dry-run:name>` placeholders so later steps still get schema-checked. The catalog dry run checks required arguments only, not enum values.
- **Target guard.** An actuating step resolves its window from `handle`, from `ref`, or by hit-testing `x`/`y`, otherwise from the foreground window. That window's process must be in `target.processes`. A scenario without `target` can't actuate. Restricted system apps (`control.exe`, `SystemSettings.exe`, `mmc.exe`, `regedit.exe`, `ms-settings:` and others; see [.agent/SAFETY.md](../.agent/SAFETY.md)) are always refused. Stale handles are refused.
- **Foreground guard.** `guard:` blocks typing unless the expected window is in front. The Lightroom value popup case is `guard: { process: lightroom.exe, class: NonActivateWindow, waitMs: 2000 }`.
- **Resume.** A resume token is single-use and expires after 12 hours. Resuming refuses if the scenario file changed, and secret params must be passed again. Recursion is blocked: `run_scenario` can't be called from inside a step; use `call:` instead.

## Lessons from the first live runs (2026-11-07)

- `wait_for` `window-active` / `window-closed` used to fail immediately instead of waiting (fixed; `WaitForRegressionTests`).
- Windows 11 Notepad restores previous tabs, so a bare `launch_app notepad.exe` may show one of your files. Open a named scratch file and guard on its title before typing (see the sample).
