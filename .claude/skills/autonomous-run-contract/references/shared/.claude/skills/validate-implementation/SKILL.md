---
name: validate-implementation
description: Use after /implement or /test-and-fix, before /pr-ready, or whenever the user asks whether a spec is actually satisfied. Verifies implementation against acceptance criteria and marks each AC Verified, Failed, or Unknown with concrete evidence.
capability: "validate-implementation agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "python .claude/skills/validate-implementation/scripts/harness.py; project test commands"
output_schema: "Validation report markdown with AC verdicts and evidence"
risk_class: medium
---

# Validate implementation (compiled harness)

Answers: **does the implementation satisfy the spec's acceptance criteria?**
Not merge readiness — that is `/pr-ready`.

For generic “are we done?” without an AC matrix, use `verification-before-completion`.

## Invoke

```bash
# Parse ACs and emit Unknown skeleton
python .claude/skills/validate-implementation/scripts/harness.py \
  --spec path/to/spec.md --skeleton

# After checks, fill verdicts (evidence required for Verified)
python .claude/skills/validate-implementation/scripts/harness.py \
  --spec path/to/spec.md \
  --verdict 'AC-1=Verified|pytest tests/test_x.py::test_y passed' \
  --verdict 'AC-2=Unknown|needs staging credentials'
```

## LLM judgment slots

For each AC, assign **Verified**, **Failed**, or **Unknown** with evidence. **Evidence or it didn't happen.**

- Reading code alone is not enough — prefer the narrowest relevant test/command.
- **Unknown is never an implicit pass.** Do not round Unknown up to Verified.
- Do not weaken criteria; propose a rewrite if untestable.

## Rules enforced by harness

- Verdict enum only: Verified | Failed | Unknown
- **Verified requires non-empty evidence** (fails otherwise)
- Only claim the spec is satisfied when **every** AC is Verified. (`all_verified` in `--json`)
