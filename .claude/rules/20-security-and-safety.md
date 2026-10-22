# Security and safety

WinGuiMcp grants agents direct control of the Windows desktop. Full detail: [docs/security.md](../../docs/security.md). Operator guardrails: [.agent/SAFETY.md](../../.agent/SAFETY.md).

## Integrity and UIPI

- **Default:** server runs at the user's normal integrity level.
- Standard-user server **cannot** automate elevated (Run as admin) apps → return `UIPIBlocked` with actionable hint.
- Do not implement elevation bypasses; document admin launch only for Phase 4 deployment.

## Hard failures (fail fast)

Do not hang on secure desktop, UAC prompts, locked workstation, or disconnected RDP. Return structured errors: `SecureDesktop`, `FocusFailed`, `CaptureFailed` per [docs/errors.md](../../docs/errors.md).

## Actuation safety

- **Prefer UIA patterns** over coordinate clicks (DPI bugs on mixed-monitor setups).
- Validate bbox within captured frame bounds before SendInput fallback.
- Log reconciliation offset if UIA vs capture disagree > 2 px.

## Agent behavior

- Warn before destructive UI actions (delete, send, format, uninstall).
- Recommend VM / Windows Sandbox for unattended automation.
- MVP transport is **stdio only** — no network exposure without auth (Phase 4).
- **Never automate Control Panel, Windows Settings (`ms-settings:`), or other restricted system apps** — see [.agent/SAFETY.md](../../.agent/SAFETY.md#restricted-targets--agents-must-not-open-or-actuate). Gate/dev flows use Calculator, Notepad, Explorer (user folders), and repo samples only.

## Secrets

- Never commit `.cursor/mcp.json` or credential files.
- MCP config uses local `dotnet run`; no API keys in repo.
