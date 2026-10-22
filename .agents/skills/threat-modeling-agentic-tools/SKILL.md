---
name: threat-modeling-agentic-tools
description: Use when threat-modeling agentic tooling, MCP/tools, hooks, prompts, remote control surfaces, review bundles, external exports, or secret exposure. Apply during security reviews or when adding features that let AI agents call tools or move data.
capability: "threat-modeling-agentic-tools agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# Threat modeling (agentic tools)

## Use this skill when

- Security reviews, or new features touching MCP servers, tools, hooks, or any remote-control surface
- Writing or updating `docs/security.md`
- Running `/security-review`

## Procedure

1. Read your security docs and the safety rules ([.agent/SAFETY.md](references/shared/.agent/SAFETY.md)).
2. **Enumerate assets:** secrets/tokens, credentials, workspace files, agent sessions, audit logs,
   any data the tools can read or mutate.
3. **Enumerate threats** (adapt to your surfaces):
   - Compromised caller / session hijack → access bypass? (mitigate: pairing/allowlist, auth, rate limits)
   - Local network or co-tenant attacker → exposed RPC/endpoint? (mitigate: localhost bind, bearer token)
   - Malicious tool caller → arbitrary operations? (mitigate: tool allowlist, approval gate, no raw shell tools)
   - Hook / prompt / payload **injection** → RCE or unintended action? (mitigate: validate input, no `eval`, minimal hook scripts)
   - Log / output **exfiltration** → secrets in logs or tool output? (mitigate: redaction, never return secrets)
   - Callback / request **replay** → duplicate side effects? (mitigate: nonce, expiry, idempotency)
4. Document **fail-closed** behavior for each path (deny on doubt).
5. List **known limitations** explicitly.

## Safety checks

- Every new external input has a validation schema and an abuse note.
- Explicit approval required for high-risk / side-effecting action types.

## Done criteria

- Review output lists threats, mitigations, gaps, and recommended tests.
- No undocumented secret-storage locations; secrets never appear in logs or tool output.

Resolve helper paths from this skill directory. Run project commands from the target workspace.
