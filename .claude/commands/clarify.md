---
capability: "clarify agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/clarify`. When customizing, keep in sync with `.cursor/commands/clarify.md`.

# /clarify — Resolve material ambiguities before planning

Use after `/spec` and before `/plan` when the spec has open questions or ambiguous acceptance criteria. This adopts Spec Kit's dedicated clarification gate: ask only questions whose answers would change scope, test expectations, architecture, data contracts, rollout, or risk posture.

## Inputs

- Draft or approved spec with `AC-n` acceptance criteria.
- User request, linked issue, or backlog item.
- Project governance from `AGENTS.md`, `.agent/SAFETY.md`, and package-specific docs.

## Output

1. **Clarification summary** — What was underspecified and why it matters.
2. **Decision questions** — Up to five prioritized questions, each with:
   - impacted `AC-n` / user story IDs,
   - why the answer matters,
   - recommended default when safe,
   - consequence of choosing the default.
3. **Assumptions to proceed** — Non-blocking assumptions recorded as explicit spec text.
4. **Spec patch notes** — Exact acceptance criteria, non-goals, assumptions, or dependencies to update.
5. **Planning readiness** — `Ready`, `Ready with assumptions`, or `Blocked`, with reason.

## Question quality rules

- Do not ask about preferences that can be safely decided by project convention.
- Do not ask more than five questions in one pass; batch by decision area.
- Prefer multiple-choice options when a bounded answer is possible.
- Convert vague concerns into observable spec changes, not implementation advice.
- If a question has a safe default, state the default and continue unless the user rejects it.

## Done when

- No `AC-n` remains marked **AMBIGUOUS**.
- Material open questions are answered, defaulted, or explicitly carried into `/plan` as risks.
- The spec has updated assumptions/dependencies instead of hidden decisions.
