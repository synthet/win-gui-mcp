---
type: Lessons Learned
title: Agent lessons — win-gui-mcp
description: Transcript-mined agent lessons and conventions for win-gui-mcp.
resource: docs/LESSONS_LEARNED.md
tags: [lessons, agent, okf]
timestamp: 2026-06-16T00:00:00Z
okf_version: 0.1
---

# Agent lessons — win-gui-mcp

Staging: `.agent/scratch/transcript-mining/REVIEW.md`.

## MCP development

- Windows GUI MCP servers need clear stdio vs SSE transport docs in `AGENTS.md`.
- Test tools with Cursor `mcp.json` locally; do not commit user paths or tokens.

## Windows GUI Automation & UIA Architecture (Oct 2026 Landscape)

- **UIA CacheRequest is essential for performance**: Walking un-cached element trees incurs N*M out-of-process COM calls. Batching properties (`BoundingRectangle`, `Name`, `AutomationId`, `ControlType`, `IsEnabled`, `HasKeyboardFocus`, `IsOffscreen`) and patterns (`Invoke`, `Value`, `Toggle`, `SelectionItem`, `ExpandCollapse`) reduces snapshot latency by 5x-10x.
- **Never rebuild full snapshots in wait loops**: Polling loops in `wait_for` must evaluate targeted UIA conditions (`FindFirstDescendant` or property match) rather than regenerating the full window snapshot, which resets snapshot IDs and causes ref churn.
- **Semantic patterns before coordinate clicks**: Always attempt `InvokePattern`, `TogglePattern`, `SelectionItemPattern` (tabs/lists), and `ExpandCollapsePattern` (combos/trees) before falling back to physical `SendInput` mouse clicks.
- **UIPI and Elevation boundaries**: Windows silently blocks `SendInput` from medium integrity to elevated processes without notifying the caller. Servers should detect elevated windows or mismatched integrity and fail fast with actionable hints (`UIPIBlocked`) rather than hanging.
- **Diagnostic failure telemetry**: Always capture foreground HWND, active window title, and modal child windows on action failure so AI agents can diagnose focus loss or unexpected blocking dialogs.
