---
capability: "run-codex-review agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# /run-codex-review — Run Codex review (MCP)

Delegate a **review-only** task to **Codex** through the **subagent-orchestrator** MCP server. Do **not** run `codex exec` directly in the terminal unless the user explicitly asks to bypass the orchestrator.

## Inputs

Everything after `/run-codex-review` is the **task** description. Reference files with `@` when helpful.

Examples:

- `/run-codex-review Review modules/runs_autodrive.py for phase planner regressions`
- `/run-codex-review dry run Review README.md`

## Steps

1. Apply skill **`subagent-review`**.
2. Optionally call **`detect_subagents`** if not done recently; confirm codex `mode: exec`.
3. Collect workspace-relative **files** from `@` paths.
4. Call **`run_subagent`**:

```json
{
  "agent": "codex",
  "task": "<user task>",
  "files": ["..."],
  "mode": "review",
  "allowWrites": false,
  "dryRun": false
}
```

Set `"dryRun": true` when the user said "dry run".

5. Read **`outputFile`** under `.agent-runs/`; summarize findings with severity labels.
6. Do **not** apply file changes automatically.

## Guardrails

- Never `allowWrites: true` (v0.1).
- No secrets in `task` or `extraContext`.
- If codex unavailable, report detection notes and stop.

## Related

- Skill: `subagent-review` · Rule: `external-cli-subagents.mdc` · Alternative: `/run-gemini-review`
