# ADR-0001: WinGuiMcp.Core library and declarative scenarios

**Status:** Accepted, 2026-11-07
**Supersedes:** the "record-and-replay" non-goal wording in [SPEC.md](../SPEC.md) and [architecture.md](../architecture.md) (narrowed, not removed)
**Accepts with changes:** [proposals/scenario-runner.md](../proposals/scenario-runner.md)

## Context

Agents driving apps with sparse UIA trees (Lightroom Desktop, DxO PhotoLab) repeat long chains of calls, and every skipped check costs real work. The proposal's evidence: about 380 calls for one Lightroom edit, 70 minutes of edits lost because persistence was never verified, and digits typed into a missed field firing rating and label shortcuts.

A sibling domain server, `lightroom-mcp`, needs the same UIA, input, capture and session layer. Until now that layer lived inside the WinGuiMcp exe, and the DxO server reimplemented it.

## Decision

1. **Core split.** `src/WinGuiMcp.Core` (class library) holds `Common` (except `McpResults`), `Platform`, `Session`, `Perception`, `Action`, `Capture`, `Tools/ToolService` and `Tools/ToolCatalogRegistry`. Namespaces are unchanged. The exe keeps `Program.cs`, `app.manifest`, `Phase1Tools`, `CompactDiscoveryTools` and `McpResults`, and wires Core with `AddWinGuiCore()`. Sibling servers consume Core by `ProjectReference` from a sibling checkout and never vendor it ([cross-project/LIGHTROOM_BOUNDARIES_CONSTITUTION.md](../cross-project/LIGHTROOM_BOUNDARIES_CONSTITUTION.md)).
2. **No tool-surface change from the split.** `tests/WinGuiMcp.Tests/ToolSurfaceSnapshotTests.cs` locks every `[McpServerTool]` attribute and catalog schema against `golden/tool-surface.json`, generated before the move.
3. **Declarative scenarios are in scope.** `src/WinGuiMcp.Core/Scenarios` runs `*.scenario.yaml` / `*.scenario.json` files ([scenarios.md](../scenarios.md)). It is exposed as four MCP tools (`run_scenario`, `resume_scenario`, `validate_scenario`, `list_scenarios`) and as a `scenario` CLI subcommand of the same exe, both through `ScenarioCommands`.
4. **Record-and-replay export stays deferred** (proposal stage S3, `draft --from-trajectory`).
5. **Steps come from providers.** Every catalog tool is a step under its own name. Sibling servers register namespaced providers (`lr.*`) inside their own process. Cross-server steps (calling another MCP server) are deferred; the proposal's MCP-client runner is not built yet.

## Changes from the proposal

| Proposal | Decision | Why |
|---|---|---|
| Separate MCP-client CLI first; `run_scenario` tool later | Engine in Core; tools and CLI subcommand now | One engine for both surfaces; sibling servers embed it in-process |
| JSON, YAML open | YAML and JSON, both | The user chose YAML for hand editing; JSON stays for generated files |
| Lightroom profile in win-gui-mcp | Lightroom steps and scenarios live in `lightroom-mcp` | Lightroom now has a domain server (Adobe's Controller API) |
| Step kinds `call/type/wait/capture/expect/exec/use/loop` | Any tool as a step; `set/assert/wait_until/checkpoint/call/log/fail`; modifiers `save/expect/retry/when/guard` | `wait_for` and `capture` already name tools/aliases; `exec` and `loop` deferred |
| Guards `foreground`, `expectFocus`, `region-unchanged`, `window-rect` | `guard:` (foreground process, class, title, handle, with wait) and `target:` (process allowlist plus a built-in denylist) | `region-unchanged` and `window-rect` deferred |
| `screenshot.outputPath` (S0) | The scenario provider honours `outputPath`; `WindowCapture.CaptureToFile` is in Core | The tool contract stays unchanged until tools.md adopts it |
| `wait_for` `region-stable`, `region-changed`, `foreground-class` | Deferred; `guard.waitMs` covers the Lightroom popup case | |
| Exit codes 0–4 | Adopted as is | |
| `writes:` plus `--allow-writes` | Adopted. Every non-read-only step needs `allowWrites`; `writes:` is documentation | |

## Known differences between the catalog path and direct tools

Scenario steps go through `ToolCatalogRegistry.ExecuteToolAsync`, like `execute_tool`. Both differences below are left unchanged:

- Errors don't carry `foregroundHwnd` / `foregroundTitle` details; `Phase1Tools` adds them.
- A partial `screenshot` region is ignored instead of raising `InvalidArgument`.

The catalog `dryRun` only checks that required arguments are present. Enum values, such as an unknown `wait_for` condition, surface only in a real run.

## Consequences

- The tool catalog and `UiaDispatcher.Shared` are process-wide static state. One host per process is assumed.
- Consumers must ship a PerMonitorV2 `app.manifest`; `AddWinGuiCore` warns on stderr otherwise.
- The first live scenario run found a real bug: `wait_for` `window-active` and `window-closed` failed immediately with "Unknown condition" instead of polling. It is fixed, with `WaitForRegressionTests`.
