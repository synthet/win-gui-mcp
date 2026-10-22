---
capability: "audit-instructions agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/audit-instructions`. When customizing, keep in sync with `.cursor/commands/audit-instructions.md`.

# /audit-instructions — Sort instruction files into three piles and prune the dated one

Audit this repo's instruction files against the current model generation. The value is not
summarizing what they say — it is separating rules that are permanently true from rules that only
existed to compensate for a model weakness, and marking the second kind as **review-due**. Guardrails
written for an older model turn into a tax on a newer one: conflicting instructions collide inside a
single request and the model spends reasoning resolving the conflict before it does the work.

**Not this command:** `/mine` turns an *external* source into new assets; this command audits the
*existing* instruction files for staleness. `/analyze` checks spec/plan/task agreement for one task;
this runs across the always-loaded instruction surface.

## Inputs

- **Targets** (default) — every file that loads into agent context regardless of task:
  [`CLAUDE.md`](../../CLAUDE.md), [`AGENTS.md`](../../AGENTS.md), [`.claude/rules/*`](../rules/), and
  the `.agent/` governance files those two reference. Add a path to narrow or extend the set.
- **Model generation** (optional) — the generation you are auditing against. Without it, audit
  against the current one and say which that is.

## Step 1 — Sort every rule into three piles

Read each target and classify each rule (not each file — a single file mixes all three) into exactly
one pile. These age at completely different rates:

| Pile | What it is | Ages? | Examples in this repo |
|------|-----------|-------|-----------------------|
| **Repo facts** | Gotchas, invariants, layout that are true no matter which model reads them | No — permanent | "`.claude/` is canonical; mirrors are generated." "Never modify `.git/config`." |
| **You & your policies** | Identity, safety, ask-before-touching-shared-state, remote-write gates, PR conventions | No — you want these holding *regardless* of how good the model's judgment gets | "Secrets via env only." "Remote writes require explicit task intent + target verification." |
| **Model-weakness compensations** | Rules that exist only because models used to do something bad | **Yes — silently** | A blanket "default to no comments"; "don't add error handling for impossible cases" |

Spend most tokens on pile 1 — gotchas, not orientation. Pile 2 is where the ask-first and
secrets-hygiene rules live; **better model judgment is not a reason to relax them** — nothing about a
model having better taste makes it correct for it to decide on its own whether to force-push.

## Step 2 — Treat pile 3 as dated, not conditional

For each pile-3 rule, the test is **"this rule existed because models used to do X — do they still?"**
Not `if model == <name>`. This is a maintenance discipline, not a schema: do **not** propose
per-model instruction files (`CLAUDE.<model>.md`) — the fix for a rule that no longer earns its place
is to rewrite or delete it, the way Anthropic's own rewrite replaced a blanket comment ban with
"write code that reads like the surrounding code: match its comment density, naming, and idiom."

Newer does not reliably mean less-constrained, and the harness confounds behavior more than the model
version does — so verify the weakness still exists before acting, and flag rather than auto-delete.

## Output

1. **Classification table** — every rule → pile, with its source `file:line`.
2. **Pile-3 worklist** — each compensation rule with: the weakness it was compensating for, whether
   that weakness still reproduces (state how you'd check, or that it's unverified), and a proposed
   rewrite or a "keep — still needed" with the reason.
3. **Conflict findings** — pairs of rules that collide (e.g. an always-on rule contradicting a
   command's guidance), since those cost reasoning on every request.
4. **Recommended edits** — minimal diffs, one rule at a time. Stop here and let the maintainer pick;
   do not rewrite instruction files in this step.

## Done when

- Every rule in the target files lands in exactly one pile with a `file:line` anchor.
- Every pile-3 rule has a keep/rewrite/delete verdict with a stated reason, never "out of date"
  unqualified.
- No per-model instruction file was proposed.
- Recommended edits are minimal and were shown for approval, not applied.

## Do not

- Do not relax a pile-2 safety or ask-first rule on the grounds that the model is now better.
- Do not delete a pile-3 rule without checking the weakness still reproduces — read the flagged line
  before concluding it is dead.
- Do not touch generated mirrors (`.cursor/`, `.agents/`) — those regenerate from `.claude/`.

Sources: "The new rules of context engineering for Claude 5 generation models" — Thariq Shihipar,
Anthropic (2026-07-24).
