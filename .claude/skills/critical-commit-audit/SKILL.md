---
name: critical-commit-audit
description: Use for a deep, high-severity bug audit of recent commits after /critical-commit-audit or when the user asks for post-commit critical review. Focus only on concrete correctness, security, data loss, crash, and major breakage triggers with minimal fixes and tests.
capability: "critical-commit-audit agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# critical-commit-audit

You are a **deep bug-finding** automation focused on **high-severity** issues.

## Goal

Inspect **recent commits** and identify **critical** correctness bugs that escaped review. Only surface issues that would cause **data loss**, **crashes**, **security holes**, or **significant user-facing breakage**.

## Investigation strategy

- Focus on **behavioral changes** with meaningful blast radius.
- Look for: **data corruption**; **race conditions** that lose writes; **null dereferences** in critical paths; **auth/permission** bypasses; **infinite loops**; **resource leaks**; **silent data truncation**.
- **Trace through the full code path** — do not only pattern-match on the diff. Understand the **caller chain** and **downstream** effects.
- **Ignore:** style issues, minor edge cases, theoretical concerns without a concrete trigger, and low-severity issues that would merely degrade UX.

## Operational workflow

1. **Scope commits** — Default: `git log -n 20 --oneline` (or a user-provided range, e.g. `main..HEAD`, `abc123..def456`). Prioritize **merge commits** and **large diffs**.
2. **Review** — For each changed area, read the full diff, then follow symbols to **callers** (search references) and **callees**. Pay attention to boundaries: public API/contract surfaces, the data/persistence layer and migrations, and any cross-process/IPC paths touched by the change.
3. **Persistence / jobs** — If commits touch persistence or task/job state, consider ordering, transactions, and idempotency; trace what happens on failure or retry.
4. **Tests** — After any fix, run lint/tests per **AGENTS.md** on the narrowest scope that covers the change.

## Confidence bar

- You must be able to describe a **concrete scenario** that triggers the bug (sequence of user or system actions).
- If you **cannot** construct a plausible trigger scenario, **do not** open a PR.
- When in doubt, **report** findings in the current chat or as a short summary / ticket (do not block on a specific tool).
- If uncertain whether severity is “critical,” **treat it as not PR-worthy** and report qualitatively only.

## Fix strategy

- If you find a **critical** bug, implement a **minimal, high-confidence** fix.
- **Add or update tests** when possible to lock in the behavior.
- **Avoid** broad refactors in the same PR.
- **Do not** expand scope (see project SDLC: small, focused changes).

## Safety rules

- **Do not open a PR** unless you are **highly confident** the bug is real and the fix is correct.
- If **no** critical bug is found, post a short **“no critical bugs found”** summary. This is the **expected** outcome most days.

## Output when nothing critical is found

Use a **short** paragraph, for example:

- Commits reviewed: (range or count).
- Focus areas: (modules or themes).
- Result: **No critical issues** (data loss, security, crash-class, or major breakage) identified with a concrete trigger in the paths traced.

## Output when a critical bug is fixed (include in your reply)

- **Bug and impact** — What breaks and for whom.
- **Root cause** — Why the defect exists (one tight paragraph).
- **Fix and validation performed** — What changed, which tests or checks ran, key results.

## Related skills

- **pr-ready** — Merge-ready description after a fix.
- **subagent-review** — Optional external (Codex/Gemini) second opinion before opening a PR.

Resolve helper paths from this skill directory. Run project commands from the target workspace.
