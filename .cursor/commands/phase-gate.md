# /phase-gate — Run Phase 1 acceptance gate

Use when Phase 1 tools are implemented and you need to verify acceptance criteria.

## Inputs

- [docs/phases.md](../../docs/phases.md) — Phase 1 gate table.
- Running WinGuiMcp MCP server (local `dotnet run` or Cursor MCP).
- Windows test apps: Calculator, Notepad, File Explorer, Settings, WinForms/WPF samples.
- Sample apps in repo:
  - WinForms: `samples/WinGuiMcp.WinFormsSample/WinGuiMcp.WinFormsSample.csproj`
  - WPF: `samples/WinGuiMcp.WpfSample/WinGuiMcp.WpfSample.csproj`

## Gate flows

An agent (or manual script invoking tools) must complete **multi-step flows without coordinate clicks**:

| App | Sample flow |
|-----|-------------|
| Calculator | Launch → click digits → equals → read result via snapshot |
| Notepad | Launch → type text → save dialog navigate (Ctrl+S) |
| File Explorer | Open folder → select file → rename |
| Settings | Navigate to a settings page and toggle a switch |
| WinForms sample | Button click + text box fill |
| WPF sample | Menu navigation + dialog |

## Steps

1. For each app, run the flow using MCP tools only (refs from snapshots, UIA patterns).
2. Record pass/fail per app with error codes if failed.
3. Measure informational metrics: snapshot latency on Settings (< 2 s target), token size on Calculator (< 8k tokens target).
4. Summarize blockers and whether phase gate passes.

## Output

| App | Pass | Notes / error codes |
|-----|------|---------------------|
| … | yes/no | … |

**Gate result:** pass / fail with remediation list.

## Done when

- All six app flows documented with pass/fail.
- Failures cite [docs/errors.md](../../docs/errors.md) codes and recovery hints.

## Related

- Skill: `win-gui-mcp-tools`
- Docs: [docs/phases.md](../../docs/phases.md)
