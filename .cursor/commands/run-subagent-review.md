---
capability: "run-subagent-review agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# /run-subagent-review — Run external sub-agent review (MCP)

Delegate a **review-only** task to one or more external CLIs via **subagent-orchestrator** MCP.

## Inputs

Text after the command is the **task**. Optional leading keywords:

| Prefix | Behavior |
|--------|----------|
| `codex …` | Run Codex only |
| `gemini …` | Run Gemini only |
| `both …` / `panel …` | Run Codex and Gemini **sequentially** with the same task (v0.1 — no `run_subagent_panel` MCP tool yet) |
| (none) | Call `detect_subagents`, then use the best available agent (prefer codex, then gemini) |

Use `@` file paths for context.

Examples:

- `/run-subagent-review Review the autodrive phase planner changes in modules/`
- `/run-subagent-review both Review README for clarity and security wording`
- `/run-subagent-review codex dry run Review the changed API handler`

## Steps

1. Apply skill **`subagent-review`** end-to-end.
2. Parse agent choice from user text (see table above).
3. For **panel/both**: run `run_subagent` twice (codex then gemini, or reverse if one is unavailable); compare agreements and disagreements in the final summary.
4. For a single agent: one `run_subagent` call with `mode: "review"`, `allowWrites: false`.
5. Use `dryRun: true` when user says "dry run".
6. Summarize all `.agent-runs/` outputs; do not auto-apply patches.

## Panel summary template

When two agents ran:

```markdown
## Panel review

### Agreement
### Disagreements
### Strongest finding
### Likely false positives
### Recommended next action
```

## Related

- Skill: `subagent-review` · Subagent: `external-cli-reviewer` · [AGENTS.md](../../AGENTS.md)
