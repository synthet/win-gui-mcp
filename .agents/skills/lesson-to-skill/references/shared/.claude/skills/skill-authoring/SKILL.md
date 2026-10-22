---
name: skill-authoring
description: Use when creating, reviewing, or improving first-party agent skills in this repo. Apply whenever a user asks to add a skill, edit SKILL.md, optimize skill triggering, benchmark skill behavior, or adapt patterns from Anthropic's public skills into the canonical .claude skill tree.
capability: "Create and improve repository agent skills with progressive disclosure, trigger evals, and generated-tree sync."
side_effect_level: local_write
approval_required: false
requires_tools: "git, python, scripts/sync_assistant_trees.py, scripts/ci/check_agent_frontmatter.py; optional pytest for sync/frontmatter tests."
output_schema: "Skill patch plus sync/frontmatter/test summary and AST10 review notes when applicable."
risk_class: medium
---

# Skill authoring

## Purpose

Create and improve durable first-party agent skills for this repository. A good skill is a compact
routing layer: its frontmatter makes the agent load it at the right time, its body gives the next
few decisions, and any large or deterministic material lives in bundled resources.

This skill adapts the public Anthropic skills patterns to this repo's stricter governance:
canonical source lives in `.claude/skills/`, generated mirrors live in `.cursor/skills/` and
`.agents/skills/`, and every material skill change needs sync + frontmatter validation.

## When to Use

Use this skill for tasks that mention any of these:

- create, add, update, improve, optimize, benchmark, or test a skill
- edit `SKILL.md`, skill frontmatter, bundled skill resources, or trigger descriptions
- port patterns from external skill examples into this repository
- diagnose why a skill under-triggers, over-triggers, or causes noisy behavior
- run AST10-style review for first-party skill changes

## Required Tools

- `git status --short` and bounded `git diff` commands
- `python scripts/sync_assistant_trees.py` and `python scripts/sync_assistant_trees.py --check`
- `python scripts/ci/check_agent_frontmatter.py`
- Optional: `pytest tests/test_sync_assistant_trees.py tests/test_agent_frontmatter.py`

## Authoring Workflow

1. **Capture intent before editing.** Identify what the skill should enable, when it should trigger,
   expected outputs, dependencies, and edge cases. Reuse evidence already in the conversation before
   asking for more detail.
2. **Choose the smallest change.** Prefer improving an existing skill when the trigger and workflow
   are closely related; create a new skill only when it has a distinct trigger surface and workflow.
3. **Author only canonical files first.** Edit `.claude/skills/<name>/SKILL.md` and any bundled
   `.claude/skills/<name>/{references,scripts,assets}/...` resources. Do not hand-edit generated
   `.cursor/skills/` or `.agents/skills/` copies.
4. **Run sync.** Execute `python scripts/sync_assistant_trees.py` so Cursor and Codex mirrors match
   the canonical Claude tree.
5. **Validate.** Run frontmatter and sync checks. Add focused tests when changing sync behavior,
   frontmatter contracts, or deterministic helper scripts.
6. **Review for safety.** Apply `.agent/SKILL_CHANGE_AST10_REVIEW.md`: honest metadata, no hidden
   risky behavior, no secrets, no surprising external export, and generated mirrors committed with
   the canonical source.

## Skill Structure

```text
.claude/skills/<skill-name>/
├── SKILL.md              # required: frontmatter + compact instructions
├── references/           # optional: loaded only when relevant
├── scripts/              # optional: deterministic helpers agents can run
└── assets/               # optional: templates, examples, static inputs
```

Use `references/` for long decision tables, provider-specific docs, schemas, examples, or checklists.
Use `scripts/` when future agents would otherwise rewrite the same parsing, packaging, grading, or
validation code repeatedly.

### Compiling a skill into a harness

After a procedural skill has crystallized (same sources, filters, and state every run),
**lower the stable steps into `scripts/harness.py`** and shrink `SKILL.md` to a thin bootloader
that lists only LLM judgment slots and human authority gates. Prefer `--json` output for agents.
Reuse shared parsers in `scripts/skill_harness/` when two or more harnesses need the same logic.
See [`.agent/SKILL_COMPILATION.md`](../../../.agent/SKILL_COMPILATION.md).

## Frontmatter Guidelines

The `description` is the primary trigger. Make it specific and slightly proactive:

- Include both **what the skill does** and **when to use it**.
- Name realistic trigger contexts and synonyms, not only the skill's formal name.
- Include near-boundary cases when the skill should win over adjacent skills.
- Avoid understating risk: metadata must match side effects and required approvals.

Required repo metadata:

```yaml
---
name: example-skill
description: Use when ...
capability: "..."
side_effect_level: read_only | local_write | remote_write | external_export
approval_required: false
requires_tools: "..."
output_schema: "..."
risk_class: low | medium | high
---
```

## Progressive Disclosure

Keep `SKILL.md` short enough to load quickly and put detailed material one level deeper.

| Layer | Loaded when | Guidance |
|-------|-------------|----------|
| Metadata | Always available | Optimize `name` + `description` for trigger accuracy. |
| `SKILL.md` body | Skill is selected | Provide decision workflow, safety gates, and pointers. |
| Bundled resources | Explicitly needed | Store lengthy references, scripts, templates, and examples. |

If a skill grows large, add a `references/README.md` or table of contents that tells agents which
single file to open for each variant. Do not make every invocation read every reference.

## Writing Patterns

- Prefer imperative steps with concise rationale. Explain why a step matters rather than piling on
  brittle all-caps rules.
- Match this repo's safety model: bounded commands, minimal diffs, no secrets, and explicit approval
  for remote writes or external exports.
- Include concrete command templates, output formats, and examples when they reduce ambiguity.
- Avoid overfitting to one prompt. Generalize from examples into reusable heuristics.
- Remove instructions that cause repeated unproductive exploration.

## Evaluation and Iteration

For non-trivial new skills or major rewrites, create 2-3 realistic test prompts before declaring the
skill done. Include both obvious triggers and near-misses.

Suggested lightweight eval file:

```json
{
  "skill_name": "example-skill",
  "evals": [
    {
      "id": "trigger-realistic-request",
      "prompt": "Concrete user request with paths/context like a real task",
      "should_trigger": true,
      "expected_behavior": "What the skill should cause the agent to do"
    }
  ]
}
```

Use qualitative review for subjective outputs. Use scripts or assertions for objective behavior such
as generated files, command selection, frontmatter validity, sync status, or schema conformance.
When improving an existing skill, compare against the previous version and look for:

- trigger precision: useful prompts load the skill, adjacent tasks do not
- token/work reduction: agents stop repeating boilerplate setup or helper code
- safety: side effects are described, gated, and auditable
- maintainability: long details moved to targeted references or scripts

## Verification Checklist

- [ ] Canonical source changed under `.claude/skills/`
- [ ] Generated mirrors updated with `python scripts/sync_assistant_trees.py`
- [ ] `python scripts/sync_assistant_trees.py --check` passes
- [ ] `python scripts/ci/check_agent_frontmatter.py` passes
- [ ] `.agent/SKILL_INVENTORY.md` updated for new or materially changed skills
- [ ] AST10 review considerations documented in the PR summary for material changes
