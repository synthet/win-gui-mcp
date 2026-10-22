---
name: karpathy-guidelines
description: Apply Karpathy's anti-pattern checklist before/during non-trivial coding — surface assumptions, resist overcomplication, keep diffs surgical, frame work as verifiable goals. Use on ambiguous or multi-step tasks, or when the user asks for a "Karpathy pass".
capability: "Deliberate coding checklist against LLM failure modes"
side_effect_level: local_write
approval_required: false
requires_tools: "git status/diff for surgical scope checks; project test commands for success criteria"
output_schema: "Assumptions, chosen interpretation, success criteria, and scoped change summary"
risk_class: medium
---

# Karpathy guidelines (deliberate coding pass)

The four guardrails in [`.claude/rules/karpathy-coding.md`](../../rules/karpathy-coding.md) are
always on. This skill is the *active checklist* to run when a task is ambiguous, multi-step, or
high-stakes — turn the guardrails into explicit steps before and during implementation.

## When to use

- The request has more than one plausible interpretation.
- The change touches existing code you'd be tempted to "clean up" along the way.
- The task is stated imperatively ("make it work", "fix it") without success criteria.
- The user explicitly asks for a Karpathy pass / deliberate review.

## Before coding

1. **Assumptions** — Write down what you're assuming about intent, scope, and environment. If any
   assumption is load-bearing and uncertain, ask before proceeding.
2. **Interpretations** — If the request is ambiguous, list the readings and pick one *explicitly*
   (or ask). Never resolve ambiguity silently.
3. **Simpler path** — Name the simplest solution that satisfies the request. If you're about to add
   abstraction, config, or error handling beyond what's asked, justify it or drop it.
4. **Success criteria** — Restate the task as a verifiable goal: the test or observable check that
   proves "done". This feeds the `/spec → /plan → /implement` loop.

## While coding

5. **Surgical scope** — Every changed line traces to the request. No drive-by edits to adjacent
   code, comments, or formatting. Match existing style.
6. **Orphan cleanup** — Remove only imports/symbols *your* change made unused. Flag pre-existing
   dead code in your summary; don't delete it unasked.

## Before finishing

7. **Diff review** — Re-read the diff as a reviewer: would a senior engineer call any of it
   overcomplicated or out of scope? If yes, cut it.
8. **Report** — Summarize what changed, what you assumed, and anything you flagged but didn't touch.

## Relationship to the SDLC loop

This is the coding-behavior layer; it doesn't replace the board workflow or the
`/spec → /plan → /implement → /test-and-fix → /pr-ready` loop — it sharpens each step. Use `/spec`
and `/plan` for the goal-driven framing (step 4); use this checklist to keep the diff honest.
