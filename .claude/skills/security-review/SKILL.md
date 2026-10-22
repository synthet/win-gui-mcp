---
name: security-review
description: Lightweight security sanity check before merge or when touching auth, elevation, UI automation, file paths, or dependencies.
---

# Security review (lightweight)

## When to use

- User asks for a **security review**, **secret check**, or **pre-merge safety pass**.
- After touching MCP transport, UI automation, elevation, or dependencies.
- Before enabling new actuation capabilities.

## Checklist (automate what you can)

1. **Secrets** — Scan changed files for API keys, tokens, private keys, connection strings.
2. **Git** — Ensure `.cursor/mcp.json`, `.env`, `*.pem`, credential files are not staged; verify `.gitignore` coverage.
3. **Dependencies** — If `WinGuiMcp.csproj` changed, note major version jumps; review FlaUI and MCP SDK updates.
4. **UI automation** — Confirm no elevation bypass; UIPI errors are actionable; destructive actions documented.
5. **MCP surface** — No escape hatches (shell exec, arbitrary file read/write, network) beyond spec.
6. **Transport** — MVP stays stdio; no exposed network listener without auth.

## WinGuiMcp-specific

- Read [docs/security.md](../../../docs/security.md) and [.agent/SAFETY.md](../../../.agent/SAFETY.md).
- Elevated app automation requires admin server — document, do not hide.
- Secure desktop / UAC: fail fast, never hang.
- Prefer UIA patterns over SendInput (DPI safety).

## Output

- **Findings** ordered by severity (critical / high / medium / low).
- **False positives** called out briefly if uncertain.
- **Fixes** as concrete edits or steps.

## Limits

This is **not** a substitute for professional pentesting. Recommend VM/Sandbox for unattended automation.

## Related

- Command: `/mcp-tool-review`
- Rule: `.cursor/rules/20-security-and-safety.mdc`
