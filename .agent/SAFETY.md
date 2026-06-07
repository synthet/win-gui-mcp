# Safety — Windows UI automation

Distilled from [docs/security.md](../docs/security.md). Agents and operators must follow these guardrails.

## Threat posture

WinGuiMcp grants AI agents **direct control of the Windows desktop**. Treat deployment as **high risk** unless sandboxed and supervised.

| Risk | Mitigation |
|------|------------|
| Agent deletes files, sends email, exfiltrates data | Human-in-the-loop for destructive ops; run in VM/Windows Sandbox |
| Prompt injection via on-screen text | Agent policy; least-privilege user account |
| Network exposure | MVP: stdio only (local). No HTTP without auth (Phase 4) |
| Elevated agent compromises system | Default non-elevated; elevate only when needed |

## Integrity levels (UIPI)

- **Default:** server runs at user's normal integrity level.
- Standard-user server **cannot** automate elevated apps → return `UIPIBlocked`.
- To automate elevated apps: launch WinGuiMcp as Administrator (Phase 4 deployment docs).
- **Do not** implement elevation bypasses.

## Hard failures — fail fast

Never hang waiting for:

- UAC / secure desktop prompts → `SecureDesktop`
- Locked workstation → `SecureDesktop`
- RDP minimized/disconnected → `FocusFailed` / `CaptureFailed`
- Screensaver active → `FocusFailed`

## Actuation

- **Prefer UIA control patterns** over coordinate clicks.
- Validate bbox within captured frame before SendInput fallback.
- Log offset if UIA vs capture disagree > 2 px.

## Agent behavior

- **Warn** before destructive UI actions (delete, uninstall, send, format).
- Recommend VM or Windows Sandbox for unattended runs.
- Never commit `.cursor/mcp.json` or secrets.
- **Never automate restricted system apps** (see below).

## Restricted targets — agents must not open or actuate

Coding agents, gate runners, and `/phase-gate` **must not** launch, focus, snapshot for action planning, click, toggle, or type in:

| Category | Examples | Why |
|----------|----------|-----|
| **Control Panel / legacy CPL** | `control.exe`, `appwiz.cpl`, `sysdm.cpl`, `ncpa.cpl` | Uninstall, network, system config |
| **Windows Settings** | `ms-settings:`, Settings (`SystemSettings`) | Toggles can change privacy, network, accounts, updates |
| **Admin / security surfaces** | Registry Editor, Local Group Policy, Computer Management, Disk Management, Windows Security (except read-only smoke if ever required) | Destructive or hard-to-reverse changes |
| **Account & recovery** | User Accounts, Reset this PC, BitLocker, Windows Update restart flows | System lockout or data loss |

**Allowed gate / dev apps:** Calculator, Notepad, File Explorer (user folders only — no Format or Properties→Disk Tools), and repo WinForms/WPF samples.

Operators may run restricted flows **manually** on a VM; agents do not. If a task requires Settings or Control Panel, stop and ask the user to perform it by hand.

## MCP surface

- No arbitrary shell, file, or network escape hatches beyond [docs/tools.md](../docs/tools.md).
- Tool names and schemas are normative — update spec before expanding capabilities.

## References

- [docs/errors.md](../docs/errors.md) — `UIPIBlocked`, `SecureDesktop`, recovery hints
- [docs/security.md](../docs/security.md) — full threat model and deployment recommendations
