# WinGuiMcp

WinGuiMcp is a Model Context Protocol (MCP) server for Windows GUI automation. It provides a Playwright-inspired "snapshot + ref" API for agentic AI control of the Windows Desktop.

## Project Documentation

Start at **[docs/README.md](docs/README.md)** for reading order and the full document map. **Authority map:** [docs/CANONICAL_SOURCES.md](docs/CANONICAL_SOURCES.md).

| Document | Purpose |
|----------|---------|
| [SPEC.md](docs/SPEC.md) | Executive summary — principles, stack, phases |
| [stack.md](docs/stack.md) | C# / .NET 10 LTS, NuGet baseline, alternatives considered |
| [architecture.md](docs/architecture.md) | System layers, session model, data flows |
| [data-model.md](docs/data-model.md) | Unified Element Model (UEM) and ref format |
| [tools.md](docs/tools.md) | Normative MCP tool definitions and JSON schemas |
| [phases.md](docs/phases.md) | Phased delivery plan and acceptance gates |
| [errors.md](docs/errors.md) | Structured error codes and recovery hints |
| [security.md](docs/security.md) | UIPI, elevation, deployment constraints |

### Background Research

Exploratory design notes live in **[docs/research/](docs/research/)** (non-normative):

- [design-research.md](docs/research/design-research.md) — comprehensive design doc
- [deep-research-report.md](docs/research/deep-research-report.md) — open-source landscape survey
- [raw-design-report.txt](docs/research/raw-design-report.txt) — original long-form report
- [windows_gui_mcp_design_html.html](docs/research/windows_gui_mcp_design_html.html) — interactive dashboard

Coding agents: see [AGENTS.md](AGENTS.md).

## Project Links

- **Repository:** https://github.com/synthet/win-gui-mcp

## Getting Started

This project is currently in the **Phase 1: MVP** development stage.

### Prerequisites

- .NET 10 LTS.0 SDK
- Windows OS (Windows 10/11 recommended)

### Build

```bash
dotnet build WinGuiMcp.sln
dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj
```

## Architecture Overview

WinGuiMcp uses a hybrid perception model:

1. **Primary:** Microsoft UI Automation (UIA) via [FlaUI](https://github.com/FlaUI/FlaUI).
2. **Fallback:** Computer Vision (OCR + object detection) for custom-rendered apps (Phase 3).

The server exposes MCP tools so agents work from structured accessibility trees with stable element refs—not pixel coordinates.
