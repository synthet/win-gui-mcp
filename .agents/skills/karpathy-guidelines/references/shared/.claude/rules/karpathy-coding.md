---
description: Behavioral guardrails against common LLM coding failure modes — assumptions, overcomplication, collateral edits.
alwaysApply: true
---

# Karpathy coding guardrails (always on)

Complements [`sdlc-core.md`](./sdlc-core.md). Where `sdlc-core` covers *process* (board, loop,
canonical sources), this covers *coding behavior*: the failure modes LLMs fall into. Bias toward
caution over speed; for trivial fixes (typos, obvious one-liners) use judgment.

- **Think before coding.** Don't assume silently. State assumptions; if multiple interpretations
  exist, surface them instead of picking one. If a simpler approach exists, say so. If something is
  unclear, stop and ask — name what's confusing.
- **Simplicity first.** Write the minimum that solves the problem. No speculative features, no
  abstractions for single-use code, no configurability that wasn't requested, no error handling for
  impossible cases. Test: *would a senior engineer call this overcomplicated?* If yes, simplify.
- **Surgical changes.** Every changed line must trace to the request. Don't "improve" adjacent code,
  comments, or formatting; match existing style even if you'd do it differently. Remove only the
  imports/symbols *your* change orphaned — flag pre-existing dead code, don't delete it unasked.
- **Goal-driven execution.** Turn imperative tasks into verifiable goals before looping (this is what
  the `/spec → /plan → /implement` flow encodes): "fix the bug" → "write a test that reproduces it,
  then make it pass." Strong success criteria let you loop independently; weak ones ("make it work")
  force constant clarification.

Working if: diffs contain only requested changes, fewer rewrites from overcomplication, and
clarifying questions land *before* implementation rather than after mistakes.
