# Security and operational constraints

WinGuiMcp grants AI agents direct control of the Windows desktop. Treat deployment as **high risk** unless sandboxed and supervised.

Related: [errors.md](errors.md) (`UIPIBlocked`, `SecureDesktop`).

## Threat model

| Risk | Mitigation |
|------|------------|
| Agent deletes files, sends email, exfiltrates data | Human-in-the-loop for destructive ops (Phase 4); run in VM/Windows Sandbox |
| Prompt injection via malicious on-screen text | Agent policy; screenshot classification (future); least-privilege user account |
| Network exposure of MCP server | MVP: stdio only (local). Remote transport requires auth (Phase 4 design) |
| Elevated agent compromises system | Default non-elevated; **separate privileged helper** for elevation-bound ops; never elevate whole service by default |
| Focus races (wrong window receives input) | Re-check foreground before SendInput; prefer UIA patterns ([architecture.md](architecture.md)) |
| Capability sprawl | Narrow core API; defer driver-level and hook-based paths |

Existing Windows MCP servers mark GUI control as **experimental and potentially dangerous**. WinGuiMcp follows the same posture.

## Integrity levels and UIPI

Windows **User Interface Privilege Isolation (UIPI)** blocks a lower-integrity process from sending input to a higher-integrity process.

| Server integrity | Target app | Result |
|------------------|------------|--------|
| Standard user | Standard user | Allowed |
| Standard user | Elevated (Run as admin) | **Blocked** → `UIPIBlocked` |
| Elevated admin | Standard user | Allowed |
| Elevated admin | Elevated app | Allowed (same level) |

**Default:** WinGuiMcp runs at the user's normal integrity level.

**To automate elevated apps:** Launch WinGuiMcp as Administrator, or use a **separate privileged helper process** (Phase 4) rather than running the entire MCP server elevated. The helper handles only elevation-bound operations; the main daemon stays user-level. Document this in deployment README (Phase 4).

### uiAccess manifest (optional advanced path)

Some secure UI scenarios require `uiAccess="true"` in the application manifest plus installation under `Program Files` and code signing. Spec-only for MVP; document if enterprise customers need secure-desktop edge cases beyond admin elevation.

## Hard failure scenarios

These cannot be automated from a standard session:

| Scenario | Error code | User action required |
|----------|------------|----------------------|
| UAC / secure desktop prompt | `SecureDesktop` | User clicks Yes/No |
| Workstation locked | `SecureDesktop` | User unlocks |
| RDP minimized or disconnected | `FocusFailed` / `CaptureFailed` | Restore session |
| Screensaver active | `FocusFailed` | User input |

Do not hang waiting for secure desktop—fail fast with structured error.

## DPI and coordinate safety

Mixed-DPI multi-monitor setups can report inconsistent UIA `BoundingRectangle` values (known issues: Teams, older Notepad).

Before SendInput fallback:

1. Validate bbox within captured frame bounds.
2. Log reconciliation offset if UIA vs capture disagree > 2 px.

Agents should prefer UIA patterns over coordinate clicks to avoid DPI bugs.

## Computer vision licensing (Phase 3)

| Component | License | Product impact |
|-----------|---------|----------------|
| Florence-2 captioner (OmniParser) | MIT | OK for permissive distribution |
| YOLO icon_detect (OmniParser) | **AGPL** | Blocks default commercial bundling |
| RapidOCR | Apache-2.0 | OK |
| Windows.Media.Ocr | OS component | OK |

**Options before Phase 3 ship:**

1. Ship CV as optional plugin with separate AGPL notice
2. Retrain/replace detector with permissive weights
3. OCR-only fallback (no icon detector) for permissive builds

Document chosen path in release notes.

## Deployment recommendations

### Development

- Dedicated test user account
- Windows Sandbox or Hyper-V VM for exploratory agent runs
- Do not connect to production email or cloud admin consoles

### Production (if ever)

- Stdio MCP only on local machine
- Separate Windows user with minimal privileges
- Audit trajectory logs (Phase 2)
- Block MCP HTTP transport without Bearer token + IP allowlist

### Future remote transport (spec only)

When implementing Streamable HTTP/SSE:

| Control | Setting |
|---------|---------|
| Authentication | `WINGUIMCP_AUTH_KEY` Bearer token |
| Network | `WINGUIMCP_IP_ALLOWLIST` CIDR blocks |
| TLS | HTTPS required |
| CORS | No wildcard origins |
| DNS rebinding | TrustedHost middleware |

Pattern reference: CursorTouch/Windows-MCP security model.

## Prompt injection and screenshots

Screenshots returned by `screenshot` may contain adversarial text designed to manipulate the agent. Mitigations:

- Prefer UIA snapshots over screenshots for action planning
- Agent system prompts should treat on-screen text as untrusted data
- Future: optional screenshot sanitization or classification pass

## Agent-restricted automation targets

Coding agents and automated gate scripts **must not** use WinGuiMcp to open or actuate Control Panel, Windows Settings (`ms-settings:`), Registry Editor, Group Policy, Disk Management, uninstallers, or similar system-configuration surfaces. Rationale: mis-clicks can change network, privacy, accounts, or uninstall software.

Product specs may still describe Settings flows for **manual operator** verification on a VM. See [.agent/SAFETY.md](../.agent/SAFETY.md).

## Data handling

- Snapshots contain UI text (names, values)—may include PII or secrets visible on screen
- Trajectory logs (Phase 2) persist tool calls and snapshots—secure log directory
- No telemetry in MVP; if added later, opt-in only

## Confirmation gate (Phase 4)

Configurable patterns requiring explicit user approval before execution:

- File delete / recycle bypass
- `shutdown`, `format`, disk partition tools
- Send/submit in email clients (pattern match on window title + button name)

Implementation: tool middleware returns `{ ok: false, code: "ConfirmationRequired", confirmationId }` until client ack.

## Summary

Run unelevated by default. Surface UIPI and secure-desktop failures clearly. Sandbox agents. Resolve AGPL before shipping CV. Treat GUI MCP as privileged automation—not a toy.
