---
name: eval
description: Use at the end of an implemented task, merged PR, or multi-iteration agent workflow to capture quality signals in project memory. Apply when the user mentions eval, feedback loop, task quality, regression learnings, or session scoring.
capability: "Validate eval signals and emit log_session.py command via compiled harness"
side_effect_level: local_write
approval_required: false
requires_tools: "python .claude/skills/eval/scripts/harness.py; python scripts/agent-memory/log_session.py"
output_schema: "Signal schema or emit-log-cmd payload with argv/command"
risk_class: medium
---

# eval (compiled harness)

Thin bootloader. Signal schema validation and `log_session.py` command construction
are compiled; you score the task and phrase memory candidates.

## Invoke

```bash
# Schema + outcome→category hints
python .claude/skills/eval/scripts/harness.py --json

# After scoring signals, emit the log command (does not write memory itself)
python .claude/skills/eval/scripts/harness.py --emit-log-cmd --json \
  --summary "Implemented <feature> (Refs L-001)" \
  --test-pass-rate yes \
  --first-try-success yes \
  --iteration-count 1 \
  --candidate "Tests-first on auth module|successful_pattern|high"
```

Then run the emitted `command` (or argv) via `scripts/agent-memory/log_session.py`.

## When to use

- After `/implement` + `/pr-ready` (success or not)
- Multi-round agent tasks, missing-then-added tests, regressions caught or missed

## LLM judgment slots

1. Score `test_pass_rate` (`yes`/`partial`/`no`), `first_try_success` (`yes`/`no`), `iteration_count`.
2. Phrase durable candidate text (`text|category|confidence`) using harness outcome hints.
3. Do not invent brittle rules for “interesting” learnings — judgment stays here.

## Human / safety

- Never log secrets, `.env`, tokens, or personal machine paths.
- Promote/dream remains under `agent-memory` (`/dream-memory`, `/promote-memory`).
- Patterns appearing ≥ 3 times in recurring issues → file a backlog follow-up.

## Cross-references

- `agent-memory` skill — log → dream → promote → context
- `backlog-queue` — file systemic follow-ups
