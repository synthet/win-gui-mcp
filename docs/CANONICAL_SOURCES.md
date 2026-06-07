# Canonical sources

Authority map for AI agents and implementers. When docs disagree, the files listed here win.

## Normative specs

| Topic | Authority file | Agent rule |
|-------|----------------|------------|
| Executive scope | [`SPEC.md`](SPEC.md) | Product name, API style, stack summary |
| Language, runtime, NuGet baseline | [`stack.md`](stack.md) | C# / .NET 9; do not change stack without updating this file |
| Tool names + JSON schemas | [`tools.md`](tools.md) | Do not rename or add tools without updating this file |
| UEM / ref format | [`data-model.md`](data-model.md) | Refs like `w1e42` only; bboxes stay server-side |
| Module layout | [`architecture.md`](architecture.md) | Match proposed `src/WinGuiMcp/` folders |
| Phase scope | [`phases.md`](phases.md) | Phase 1 gate apps pass before Phase 2 |
| Error codes | [`errors.md`](errors.md) | Typed codes + agent recovery hints |
| Security | [`security.md`](security.md) | UIPI, elevation, deployment constraints |
| Technical requirements | [`requirements/README.md`](requirements/README.md) | Acceptance criteria and traceability; does not override tool schemas |

## Agent configuration

| Topic | Authority file | Agent rule |
|-------|----------------|------------|
| Commands, build, MCP setup | [`../AGENTS.md`](../AGENTS.md) | Overrides generic guidance for this repo |
| Infra inventory | [`../.agent/AGENT_INFRA_INVENTORY.md`](../.agent/AGENT_INFRA_INVENTORY.md) | Paths to rules, commands, skills |
| Safety guardrails | [`../.agent/SAFETY.md`](../.agent/SAFETY.md) | Windows automation behavior |

## Non-normative (background only)

| Topic | Location | Agent rule |
|-------|----------|------------|
| Design research | [`research/design-research.md`](research/design-research.md) | **Do not implement from these files** |
| Landscape survey | [`research/deep-research-report.md`](research/deep-research-report.md) | Tracked markdown extract; **do not implement from this file** |
| Design research | [`research/design-research.md`](research/design-research.md) | Background only |
| Raw design report | [`research/raw-design-report.txt`](research/raw-design-report.txt) | Background only |
| Interactive dashboard | [`research/windows_gui_mcp_design_html.html`](research/windows_gui_mcp_design_html.html) | Stale tool names; see [`tools.md`](tools.md) |
| Optional PDFs | [`research/README.md`](research/README.md#optional-pdfs-not-in-git) | Not tracked in git; use markdown equivalents |
| Redirect stubs | [`deep-research-report.md`](deep-research-report.md), [`open-source-windows-gui-control-for-ai-agents.pdf.md`](open-source-windows-gui-control-for-ai-agents.pdf.md) | Old paths → [`research/`](research/) or normative specs |

## Prior art (reference, not authority)

- [shanselman/FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) — Playwright-style refs on Windows via FlaUI

When borrowing patterns from prior art, reconcile tool names and schemas with [`tools.md`](tools.md) before implementing.
