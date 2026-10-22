# Skill compilation (specialized harnesses)

Pattern adapted from [Compiling an AI agent skill](https://vivekhaldar.com/articles/compiling-an-ai-agent-skill/)
(Vivek Haldar / Token Shrinker method).

## Why

Natural-language `SKILL.md` files are excellent for **discovering** a workflow.
Once the same procedure runs repeatedly, paying a frontier model to re-plan,
rebuild state, and re-interpret fixed rules is wasteful (the “reasoning tax”).

**Compile** the crystallized steps into deterministic code. Keep the model only
for semantic judgment. Keep humans for consequential actions.

## Owner boundary

| Owner | Owns |
|-------|------|
| **Code** | Paths, parsers, fixed rules, state, schema validation, report skeletons, dry-run gates |
| **LLM** | Semver judgment, AC verdicts from evidence, claim interpretation, commit-message wording, ambiguous task classification |
| **Human** | Commit/push/tag, external export approval, production publish |

## Layout

```text
.claude/skills/<name>/
├── SKILL.md           # thin bootloader: when-to-use, harness invoke, LLM slots
└── scripts/
    └── harness.py     # deterministic CLI; prefer --json for agents
```

Shared parsers live in [`scripts/skill_harness/`](../scripts/skill_harness/).

Author under `.claude/` only; run `python scripts/sync_assistant_trees.py` so
`.cursor/skills/` and `.agents/skills/` mirrors include `scripts/`.

## Compiled in this repo

| Skill | Harness | LLM slots |
|-------|---------|-----------|
| `release-bump` | `.claude/skills/release-bump/scripts/harness.py` | Choose major\|minor\|patch |
| `validate-implementation` | `.claude/skills/validate-implementation/scripts/harness.py` | Verdict + evidence per AC |
| `verification-before-completion` | `.claude/skills/verification-before-completion/scripts/harness.py` | Name claims; interpret outputs |
| `search-tool-selection` | `.claude/skills/search-tool-selection/scripts/harness.py` | Map ask → task type |
| `commit-and-push` | `.claude/skills/commit-and-push/scripts/harness.py` | Commit message; human must request ship |
| `agent-memory` | `scripts/agent-memory/*.py` (pre-existing) | Phrase candidates; review dreams |
| `task-env-package-tools` | `.claude/skills/task-env-package-tools/scripts/harness.py` | Choose profile/gates; interpret failures |
| `lint-format-security` | `.claude/skills/lint-format-security/scripts/harness.py` | Map ask/paths → stack; approve auto-fix |
| `eval` | `.claude/skills/eval/scripts/harness.py` | Score signals; phrase memory candidates |

## How to compile another skill

Run [`/compile-skill <name>`](../.claude/commands/compile-skill.md), which drives this checklist:

1. Confirm the workflow is stable (repeated traces / inventory usage).
2. Partition steps into code / LLM / human — do not freeze judgment into brittle rules.
3. Implement `scripts/harness.py` with dry-run default and `--json`.
4. Shrink `SKILL.md` to a bootloader that points at the harness and lists judgment slots.
5. Add fixture tests under `tests/test_skill_harnesses.py`.
6. Sync trees, update [SKILL_INVENTORY.md](SKILL_INVENTORY.md), run AST10 review.

## Measurement note

This repository does not store per-session token telemetry, so we do not claim
numeric 94% token savings here. Savings are **structural**: agents load a short
bootloader and run code instead of re-deriving the procedure from long prose.
Replay harnesses against fixtures to prove behavioral parity for the code path.
