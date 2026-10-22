---
name: verification-before-completion
description: Use before claiming work is complete, fixed, passing, ready to commit, or ready for PR. Apply to ensure fresh command output supports every success claim and to report warnings or failures honestly.
capability: "Completion verification evidence workflow"
side_effect_level: read_only
approval_required: false
requires_tools: "python .claude/skills/verification-before-completion/scripts/harness.py; project test/build/lint; git status"
output_schema: "Verification summary listing each claim, exact command, result, and any limitations"
risk_class: low
---

# Verification before completion (compiled harness)

Do not claim done/fixed/passing/ready until fresh command output supports the claim.

## Invoke

```bash
# List catalog
python .claude/skills/verification-before-completion/scripts/harness.py --json

# Run selected proofs
python .claude/skills/verification-before-completion/scripts/harness.py \
  --claim tests_pass --claim assets_synced --run --json

# Framework self-verify suite
python .claude/skills/verification-before-completion/scripts/harness.py --framework --run
```

## LLM judgment slots

1. **Name the claim** and pick the falsifying proof (catalog ids or custom command).
2. **Interpret output** after the final edit — exit code, warnings, skipped tests, scope.
3. Report ✅ / ⚠️ / ❌ with the exact command; never upgrade incomplete verification to pass.

## Use with

- `validate-implementation` when an AC matrix is required
- `commit-and-push` before an actual commit
