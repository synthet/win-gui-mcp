---
description: Always-on safety and secret-handling rules.
alwaysApply: true
---

# Safety & secrets (always on)

- Secrets (API keys, tokens, passwords) live in `secrets.json` / `.env` (git-ignored). Never commit
  them, never paste them into prompts/logs/tool args, never return them in tool output.
- Never modify `.git/config` or add non-standard git extensions (no `extensions.worktreeConfig`).
- Treat write-capable / destructive tools (run jobs, mutate config, execute code, `DELETE`/DDL SQL) as
  high-risk: prefer read-only diagnostics unless the user explicitly asks for the write.
- External CLI reviews are review-only (`allowWrites: false`); never send secrets or full config.
- Validate external inputs; side-effecting actions need confirmation/approval.

Full detail: [`.agent/SAFETY.md`](../../.agent/SAFETY.md), [`docs/security.md`](../../docs/security.md).
