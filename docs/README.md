# WinGuiMcp Documentation

WinGuiMcp is a Model Context Protocol (MCP) server that gives AI agents a Playwright-style interface for controlling the Windows desktop. Agents work from structured accessibility snapshots with stable element refs—not pixel coordinates.

## Reading order (implementers)

1. [SPEC.md](SPEC.md) — Executive summary and scope (~5 min)
2. [stack.md](stack.md) — Language, runtime, and layer technology choices
3. [architecture.md](architecture.md) — Layers, session model, data flows
4. [data-model.md](data-model.md) — Unified Element Model (UEM) JSON schema
5. [tools.md](tools.md) — MCP tool reference with JSON schemas
6. [phases.md](phases.md) — Implementation roadmap and acceptance gates
7. [requirements/README.md](requirements/README.md) — Technical requirements (acceptance criteria, traceability)

Read [errors.md](errors.md) and [security.md](security.md) alongside `tools.md` when implementing error handling and deployment constraints.

**Authority map:** [CANONICAL_SOURCES.md](CANONICAL_SOURCES.md) — which file wins when docs disagree.

## Document map

| Document | Purpose |
|----------|---------|
| [CANONICAL_SOURCES.md](CANONICAL_SOURCES.md) | Authority map for agents and implementers |
| [SPEC.md](SPEC.md) | Executive specification; links to all sub-specs |
| [stack.md](stack.md) | Language, runtime, NuGet baseline, alternatives considered |
| [architecture.md](architecture.md) | System design, modules, session invalidation |
| [data-model.md](data-model.md) | UEM node schema, ref format, tree pruning |
| [tools.md](tools.md) | Normative MCP tool definitions (Phase 1 MVP + Phase 2+ catalog) |
| [errors.md](errors.md) | Typed error codes and agent recovery hints |
| [phases.md](phases.md) | Phased delivery plan with benchmark gates |
| [security.md](security.md) | UIPI, elevation, deployment warnings |
| [requirements/README.md](requirements/README.md) | Technical requirements index (traceability; does not override schemas) |

## Research (background only)

The [research/](research/) folder holds exploratory design notes and literature reviews. **Do not implement from these files directly**—use the normative specs above.

| File | Notes |
|------|-------|
| [research/design-research.md](research/design-research.md) | Comprehensive research-backed design (alias: `windows-gui`) |
| [research/deep-research-report.md](research/deep-research-report.md) | Open-source landscape survey (tracked markdown extract) |
| [research/raw-design-report.txt](research/raw-design-report.txt) | Original long-form report (alias: WinDOM-MCP) |
| [research/windows_gui_mcp_design_html.html](research/windows_gui_mcp_design_html.html) | Interactive research dashboard; tool names superseded by [tools.md](tools.md) |

Optional PDF sources (not in git): see [research/README.md](research/README.md#optional-pdfs-not-in-git). Redirect stubs at [deep-research-report.md](deep-research-report.md) for the old docs-root path.

## Canonical decisions

- **Product name:** WinGuiMcp
- **API style:** Playwright MCP snapshot + ref
- **Language / runtime:** C# / .NET 9 — rationale and alternatives in [stack.md](stack.md)
- **Stack:** FlaUI UIA3 (UIA2 fallback), `ModelContextProtocol` SDK, stdio MCP
- **Actuation:** UIA control patterns first; SendInput at bbox center as fallback

## Prior art

Closest existing implementation: [shanselman/FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) (Playwright-style refs on Windows via FlaUI).

