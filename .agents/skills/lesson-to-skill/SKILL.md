---
name: lesson-to-skill
description: Use at the end of a session, after a correction, or when the user says bake this in, turn this into a skill, update the skill based on this conversation, capture the lesson, or don't make that mistake again. Analyzes the conversation for corrections, mistakes, repeated actions, and workarounds, then enriches an existing skill/command/rule or creates a new one. For mining an external document or repo instead, use /mine.
capability: "Convert conversation lessons into durable agent assets or memory"
side_effect_level: local_write
approval_required: false
requires_tools: "Read/Grep over .claude assets; python scripts/sync_assistant_trees.py; python scripts/ci/check_agent_frontmatter.py; optional python scripts/agent-memory/log_session.py"
output_schema: "Lesson table (evidence → durability → route) plus the asset patch and sync/frontmatter output"
risk_class: medium
---

# Lesson to skill

Turn what *this* conversation taught into something the next agent inherits. The input is the session
itself — the corrections, the dead ends, the command you ran four times — not an external document
(that is [`/mine`](references/shared/.claude/commands/mine.md)).

## When to use

- The user corrected you, and the correction would apply again.
- You repeated the same manual sequence several times.
- You hit an environment quirk and found a workaround.
- The session ends and the user says "bake this in" / "update the skills".

## Step 1 — Harvest with evidence

Work from the **conversation in context**. Do not go looking for a transcript file on disk: those
paths are assistant- and machine-specific, and this framework is seeded into other repos where they
do not exist. If the user hands you an exported transcript path, read that as an additional source.

Every lesson needs a concrete citation — a message, a command, an error:

| Signal | Look for | Usually becomes |
|--------|----------|-----------------|
| **Correction** | The user redirected you, or said you were wrong | The highest-value edit; a check in the relevant asset |
| **Mistake** | Wrong tool, wrong path, failed command, invented API | A guardrail or routing line |
| **Repetition** | Same command sequence three or more times | A harness candidate → [`/compile-skill`](references/shared/.claude/commands/compile-skill.md) |
| **Workaround** | Environment discovery that cost you a detour | A concrete command in the relevant skill |
| **Boundary** | A scope decision you would make the same way again | A "do not" line, or a routing rule |

## Step 2 — Filter for durability

Keep a lesson only if all three hold:

1. It would change behavior on a **different, future** task — not just this file or this bug.
2. It is stable, not a one-time glitch (a flaky network call is not a lesson).
3. It is not already written down. Check the asset before claiming the gap.

**If nothing survives, say "no durable lesson from this session" and stop.** Manufacturing lessons to
justify running this skill is its main failure mode, and it is how a skill tree fills with noise.

## Step 3 — Route each lesson

| Lesson shape | Route |
|--------------|-------|
| Refines a procedure that already has an asset | **Enrich that asset** — the default |
| Fact about this repo, this user's preference, or a one-off outcome | [`eval`](references/shared/.claude/skills/eval/SKILL.md) / `/log-session` memory candidate — *not* a skill |
| Distinct workflow with a trigger no existing skill covers | New skill via [`skill-authoring`](references/shared/.claude/skills/skill-authoring/SKILL.md) |
| Deterministic steps repeated every run | Harness via [`/compile-skill`](references/shared/.claude/commands/compile-skill.md) |
| Constraint that must hold on **every** task | Rule in `.claude/rules/` — ask the user first; rules load everywhere |

Default to enriching. A new skill must name the trigger surface that no current skill has; if you
cannot name it, the lesson belongs in an existing asset.

## Step 4 — Write the rule, not the war story

Bake in the reasoning, not the anecdote. For each lesson write:

1. **Trigger** — the condition where this applies.
2. **Check or step** — what would have prevented the mistake, with the concrete command or path when
   the fix is mechanical.
3. **Why**, in one line — so a later agent does not delete it as noise.
4. **Verification** — what proves it worked, when the lesson is about correctness.

Keep it compact. Long material goes to `references/` under the skill, per progressive disclosure —
never a growing "lessons learned" log inside `SKILL.md`.

## Step 5 — Propose before writing

Show the table, then wait:

| Lesson | Evidence | Route | Proposed change |
|--------|----------|-------|-----------------|

## Step 6 — Verify and register

```bash
python scripts/sync_assistant_trees.py
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
python scripts/generate_agent_asset_inventory.py
```

Update `.agent/SKILL_INVENTORY.md` (row and **Last reviewed**) for any new or materially changed
skill and apply [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](references/shared/.agent/SKILL_CHANGE_AST10_REVIEW.md).
Ship generated mirrors with the canonical source.

## Anti-patterns

- A skill per incident. Incidents cluster — enrich the asset that owns the cluster.
- A permanent rule from a single environment failure.
- Restating something a rule already enforces; that is duplication, not reinforcement.
- Recording *what happened* instead of *what to do next time*.

## Related

- [`skill-authoring`](references/shared/.claude/skills/skill-authoring/SKILL.md) — how to write the asset well
- [`/mine`](references/shared/.claude/commands/mine.md) — same output, external source
- [`agent-memory`](references/shared/.claude/skills/agent-memory/SKILL.md) — where non-durable session signals go
- [`karpathy-guidelines`](references/shared/.claude/skills/karpathy-guidelines/SKILL.md) — keep the resulting diff surgical

Resolve helper paths from this skill directory. Run project commands from the target workspace.
