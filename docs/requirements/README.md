# WinGuiMcp technical requirements

Traceable acceptance criteria derived from normative specs. **Does not override** tool JSON schemas — see [CANONICAL_SOURCES.md](../CANONICAL_SOURCES.md).

## Reading order

1. Normative specs: [tools.md](../tools.md), [architecture.md](../architecture.md), [phases.md](../phases.md)
2. Phase 1 requirements: [modules/](modules/) then [tools/](tools/) then [gates/](gates/)
3. Cross-cutting: [cross-cutting/](cross-cutting/)
4. Phase 2–4: files tagged P2/P3/P4 in same folders

## TR ID scheme

```text
TR-{Phase}-{Category}-{Feature}-{Seq}
```

| Category | Meaning |
|----------|---------|
| `MOD` | Module |
| `TOOL` | MCP tool |
| `GATE` | Acceptance gate step |
| `X` | Cross-cutting |
| `SEC` | Security / deployment |

Template: [_template.md](_template.md)

## Implementation order (Phase 1)

```text
mcp-host → dpi-manager, window-enumerator
  → snapshot-session, ref-table
  → uia-snapshot-builder, window-capture
  → pattern-executor, input-simulator
  → tools (list_windows → snapshot → click → …)
  → phase-1 gates
```

Slash commands: `/bootstrap-phase1`, `/implement-tool`, `/phase-gate`

## Module → tool dependencies

| Module | Dependent tools |
|--------|-----------------|
| [mcp-host](modules/mcp-host.md) | All |
| [window-enumerator](modules/window-enumerator.md) | list_windows, focus_window, launch_app |
| [uia-snapshot-builder](modules/uia-snapshot-builder.md) | snapshot, find_element |
| [ref-table](modules/ref-table.md) | find_element, get_properties, click, type_text, set_value |
| [pattern-executor](modules/pattern-executor.md) | click, type_text, set_value |
| [input-simulator](modules/input-simulator.md) | click, type_text, hotkey |
| [window-capture](modules/window-capture.md) | screenshot |

## Traceability matrix

**Total TRs:** 142 (excluding template example)

### Phase 1 — MVP

| Feature file | TRs | Canonical spec | Command | Gate |
|--------------|-----|----------------|---------|------|
| [modules/mcp-host.md](modules/mcp-host.md) | 4 | architecture, stack | /bootstrap-phase1 | — |
| [modules/snapshot-session.md](modules/snapshot-session.md) | 3 | architecture, data-model | /bootstrap-phase1 | — |
| [modules/ref-table.md](modules/ref-table.md) | 3 | data-model | /bootstrap-phase1 | — |
| [modules/uia-snapshot-builder.md](modules/uia-snapshot-builder.md) | 4 | architecture, data-model | /bootstrap-phase1 | phase-1-* |
| [modules/pattern-executor.md](modules/pattern-executor.md) | 4 | tools, architecture | /bootstrap-phase1 | phase-1-* |
| [modules/input-simulator.md](modules/input-simulator.md) | 4 | architecture, tools | /bootstrap-phase1 | — |
| [modules/window-capture.md](modules/window-capture.md) | 3 | tools, architecture | /bootstrap-phase1 | — |
| [modules/dpi-manager.md](modules/dpi-manager.md) | 2 | stack, data-model | /bootstrap-phase1 | — |
| [modules/window-enumerator.md](modules/window-enumerator.md) | 3 | tools | /bootstrap-phase1 | — |
| [cross-cutting/error-envelope.md](cross-cutting/error-envelope.md) | 2 | errors | — | — |
| [cross-cutting/session-invalidation.md](cross-cutting/session-invalidation.md) | 3 | data-model, errors | — | — |
| [cross-cutting/performance-budgets.md](cross-cutting/performance-budgets.md) | 4 | phases | /phase-gate | phase-1-* |
| [cross-cutting/dpi-and-coordinates.md](cross-cutting/dpi-and-coordinates.md) | 3 | security, data-model | — | — |
| [tools/list_windows.md](tools/list_windows.md) | 2 | tools | /implement-tool | — |
| [tools/launch_app.md](tools/launch_app.md) | 2 | tools | /implement-tool | phase-1-* |
| [tools/focus_window.md](tools/focus_window.md) | 2 | tools | /implement-tool | phase-1-* |
| [tools/snapshot.md](tools/snapshot.md) | 3 | tools, data-model | /implement-tool | phase-1-* |
| [tools/find_element.md](tools/find_element.md) | 3 | tools | /implement-tool | phase-1-* |
| [tools/get_properties.md](tools/get_properties.md) | 2 | tools, data-model | /implement-tool | — |
| [tools/click.md](tools/click.md) | 3 | tools | /implement-tool | phase-1-* |
| [tools/type_text.md](tools/type_text.md) | 3 | tools | /implement-tool | phase-1-* |
| [tools/set_value.md](tools/set_value.md) | 2 | tools | /implement-tool | phase-1-* |
| [tools/hotkey.md](tools/hotkey.md) | 2 | tools | /implement-tool | phase-1-* |
| [tools/screenshot.md](tools/screenshot.md) | 3 | tools | /implement-tool | — |
| [tools/wait_for.md](tools/wait_for.md) | 3 | tools | /implement-tool | phase-1-* |
| [gates/phase-1-calculator.md](gates/phase-1-calculator.md) | 4 | phases | /phase-gate | phase-1-calculator |
| [gates/phase-1-notepad.md](gates/phase-1-notepad.md) | 6 | phases | /phase-gate | phase-1-notepad |
| [gates/phase-1-file-explorer.md](gates/phase-1-file-explorer.md) | 5 | phases | /phase-gate | phase-1-file-explorer |
| [gates/phase-1-settings.md](gates/phase-1-settings.md) | 4 | phases | /phase-gate | phase-1-settings |
| [gates/phase-1-winforms-sample.md](gates/phase-1-winforms-sample.md) | 4 | phases | /phase-gate | phase-1-winforms |
| [gates/phase-1-wpf-sample.md](gates/phase-1-wpf-sample.md) | 4 | phases | /phase-gate | phase-1-wpf |

### Phase 2 — Robustness

| Feature file | TRs | Canonical spec | Command | Gate |
|--------------|-----|----------------|---------|------|
| [modules/events.md](modules/events.md) | 2 | architecture | /implement | phase-2-office |
| [modules/msaa-ia2-bridge.md](modules/msaa-ia2-bridge.md) | 2 | phases, architecture | /implement | — |
| [modules/observability.md](modules/observability.md) | 2 | architecture, errors | /implement | — |
| [modules/pattern-executor.md](modules/pattern-executor.md) | +1 | phases | /implement-tool | phase-2-office |
| [modules/window-capture.md](modules/window-capture.md) | +1 | architecture | /implement | — |
| [tools/toggle.md](tools/toggle.md) | 1 | tools | /implement-tool | — |
| [tools/expand.md](tools/expand.md) | 1 | tools | /implement-tool | — |
| [tools/select.md](tools/select.md) | 1 | tools | /implement-tool | — |
| [tools/scroll.md](tools/scroll.md) | 1 | tools | /implement-tool | — |
| [tools/drag.md](tools/drag.md) | 1 | tools | /implement-tool | — |
| [tools/get_text.md](tools/get_text.md) | 1 | tools | /implement-tool | — |
| [tools/inspect_at.md](tools/inspect_at.md) | 1 | tools | /implement-tool | — |
| [tools/attach.md](tools/attach.md) | 1 | tools | /implement-tool | — |
| [cross-cutting/error-envelope.md](cross-cutting/error-envelope.md) | +1 | errors | — | — |
| [gates/phase-2-office-workflow.md](gates/phase-2-office-workflow.md) | 2 | phases | /phase-gate | phase-2-office |

### Phase 3 — Vision

| Feature file | TRs | Canonical spec | Command | Gate |
|--------------|-----|----------------|---------|------|
| [modules/vision-fusion.md](modules/vision-fusion.md) | 4 | architecture, data-model | /implement | phase-3-uia-blind |
| [modules/uia-snapshot-builder.md](modules/uia-snapshot-builder.md) | +1 | tools, phases | /implement-tool | phase-3-uia-blind |
| [tools/snapshot.md](tools/snapshot.md) | +1 | tools | /implement-tool | phase-3-uia-blind |
| [tools/find_element.md](tools/find_element.md) | +1 | tools | /implement-tool | phase-3-uia-blind |
| [tools/get_text.md](tools/get_text.md) | +1 | tools, phases | /implement-tool | phase-3-uia-blind |
| [security/cv-licensing.md](security/cv-licensing.md) | 2 | security, phases | — | — |
| [gates/phase-3-uia-blind-demo.md](gates/phase-3-uia-blind-demo.md) | 2 | phases | /phase-gate | phase-3-uia-blind |

### Phase 4 — Security

| Feature file | TRs | Canonical spec | Command | Gate |
|--------------|-----|----------------|---------|------|
| [security/elevation-and-uipi.md](security/elevation-and-uipi.md) | 3 | security, errors | — | phase-4-security |
| [security/confirmation-gates.md](security/confirmation-gates.md) | 1 | errors, security | — | — |
| [security/deployment.md](security/deployment.md) | 4 | security, phases | — | — |
| [gates/phase-4-uipi-and-secure-desktop.md](gates/phase-4-uipi-and-secure-desktop.md) | 3 | phases | /phase-gate | phase-4-security |

## Phase 1 explicit non-requirements

- Vision, OCR, `useVision` (Phase 3)
- `scroll`, `drag`, Phase 2 pattern tools
- UIA event subscriptions (Phase 2)
- Required post-action `_warning` emission (Phase 2)
- `click.point` for gate pass criteria
- HTTP MCP transport

## Open questions (spec review before implementation)

| TR | Question |
|----|----------|
| TR-P3-TOOL-FindElement-004 | Confirm exact vision selector syntax in tools.md before coding |
| TR-P2-MOD-MsaaIa2-002 | Whether MSAA nodes need distinct `source` enum in data-model |
