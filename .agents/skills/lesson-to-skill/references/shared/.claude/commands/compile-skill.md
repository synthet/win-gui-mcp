---
capability: "compile-skill agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "python scripts/sync_assistant_trees.py; python scripts/ci/check_agent_frontmatter.py; pytest tests/test_skill_harnesses.py"
output_schema: "Partition table, harness script, bootloader SKILL.md, fixture tests, and sync/frontmatter/test output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/compile-skill`. When customizing, keep in sync with `.cursor/commands/compile-skill.md`.

# /compile-skill — Lower a stable skill into a deterministic harness

Use when a procedural skill has crystallized and the agent keeps paying the reasoning tax to
re-derive the same steps. Compiles the fixed parts into `scripts/harness.py` and shrinks `SKILL.md`
to a thin bootloader. Pattern and current inventory: [`.agent/SKILL_COMPILATION.md`](../../.agent/SKILL_COMPILATION.md).

## Inputs

- Target skill: `.claude/skills/<name>/SKILL.md`. If the user did not name one, list uncompiled
  candidates with the stability evidence you have and ask — do not pick silently.
- Evidence of stability: repeated transcripts running the same procedure, or a row in
  [`.agent/SKILL_INVENTORY.md`](../../.agent/SKILL_INVENTORY.md) with real usage.

## Readiness gate

Compile only if **all** hold. If any fails, say "not ready" and name the failing condition instead
of compiling an unstable procedure.

- Same sources, filters, and state every run — no per-run re-planning.
- At least one step is pure mechanics (path resolution, parsing, formatting, schema validation).
- The judgment left over is nameable in a sentence or two ("choose the semver level").
- The skill is not mostly judgment. Advisory skills (`systematic-debugging`, `karpathy-guidelines`,
  `security-review`) have nothing to lower — leave them as prose.

## Step 1 — Partition the steps

Produce this table before writing code, and show it to the user:

| Owner | Gets |
|-------|------|
| **Code** | Paths, parsers, fixed rules, state, schema validation, report skeletons, dry-run gates |
| **LLM** | Semantic judgment that would become brittle as a rule |
| **Human** | Commit / push / tag, external export approval, production publish |

If a step needs a heuristic to stay correct, it belongs to the LLM. Freezing judgment into rules is
the main way this pattern fails.

## Step 2 — Implement the harness

```text
.claude/skills/<name>/scripts/harness.py
```

- Stdlib only; resolve the repo root as `Path(__file__).resolve().parents[4]` — no hardcoded paths.
- Read-only by default; writes require an explicit `--apply` (mirror `release-bump`).
- Support `--json` for agents; print human summary otherwise. Errors to stderr, non-zero exit.
- Put logic two or more harnesses share in [`scripts/skill_harness/`](../../scripts/skill_harness/)
  and import it — do not copy parsers between harnesses.
- The harness never commits, pushes, tags, or exports outside the repo.

## Step 3 — Shrink `SKILL.md` to a bootloader

Keep the frontmatter contract (`name` first, then `description`, `capability`,
`side_effect_level`, `approval_required`, `requires_tools`, `output_schema`, `risk_class`), then
keep only: when to use, **Invoke** (copy-pasteable commands), **LLM judgment slots** (numbered),
**Human authority**, **Verify**. Delete prose the harness now enforces.

## Step 4 — Test

Add fixture tests to [`tests/test_skill_harnesses.py`](../../tests/test_skill_harnesses.py) covering
the shared helper directly and the harness end-to-end via `--json`. Assert behavioral parity with
the prose procedure on at least one realistic fixture — that is the evidence the compile was lossless.

## Step 5 — Sync and record

```bash
python scripts/sync_assistant_trees.py
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
python -m pytest tests/test_skill_harnesses.py -q
```

Then update `.agent/SKILL_COMPILATION.md` (new row in the compiled table),
`.agent/SKILL_INVENTORY.md` (mark **compiled harness**, refresh **Last reviewed**), and apply
[`.agent/SKILL_CHANGE_AST10_REVIEW.md`](../../.agent/SKILL_CHANGE_AST10_REVIEW.md).

## Done when

- Partition table was shown and the LLM slots are named in the bootloader.
- Harness runs read-only by default, emits `--json`, and reuses shared parsers where applicable.
- Fixture tests pass and demonstrate parity on a real case.
- Sync, frontmatter, and tests are green; both `.agent/` docs updated in the same change.

## Do not

- Do not hand-edit `.cursor/` or `.agents/` mirrors — regenerate them.
- Do not give the harness commit/push/tag or external-export authority.
- Do not claim numeric token savings; this repo has no per-session telemetry. Savings are structural.
