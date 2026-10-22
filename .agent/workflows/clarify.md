# /clarify — Resolve material ambiguities before planning

Use after `/spec` and before `/plan` when a spec has open questions or ambiguous acceptance criteria. This gate borrows Spec Kit's dedicated clarification phase: clarify decisions that materially change behavior and record safe defaults for everything else.

## Inputs

- Draft or approved spec with `AC-n` acceptance criteria.
- User request, linked issue, or backlog item.
- Project governance from [AGENTS.md](../../AGENTS.md), [SAFETY](../SAFETY.md), and package-specific docs.

## Steps

1. Identify open questions, ambiguous `AC-n` criteria, missing non-goals, and hidden assumptions.
2. Keep only questions whose answers affect scope, acceptance criteria, tests, architecture, data contracts, rollout, or risk posture.
3. Ask up to five prioritized questions; when safe, provide a recommended default and the consequence of accepting it.
4. Convert answers or defaults into explicit spec patch notes for acceptance criteria, non-goals, assumptions, dependencies, or risks.
5. Mark planning readiness as `Ready`, `Ready with assumptions`, or `Blocked`.

## Done when

- No `AC-n` remains **AMBIGUOUS**.
- Material open questions are answered, defaulted, or explicitly carried into `/plan` as risks.
- The spec captures assumptions/dependencies instead of relying on hidden decisions.
