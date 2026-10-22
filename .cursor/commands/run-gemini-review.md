---
capability: "run-gemini-review agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# /run-gemini-review — Run Gemini review (MCP)

Delegate a **review-only** task to **Gemini CLI** through the **subagent-orchestrator** MCP server. Do **not** run `gemini -p` directly in the terminal unless the user explicitly asks to bypass the orchestrator.

## Inputs

Everything after `/run-gemini-review` is the **task** description. Reference files with `@` when helpful.

Examples:

- `/run-gemini-review Review the changed API handler for contract drift`
- `/run-gemini-review dry run Summarize risks in the new migration`

## Steps

1. Apply skill **`subagent-review`**.
2. Optionally call **`detect_subagents`**; confirm gemini `mode: prompt`.
3. Collect workspace-relative **files** from `@` paths.
4. Call **`run_subagent`**:

```json
{
  "agent": "gemini",
  "task": "<user task>",
  "files": ["..."],
  "mode": "review",
  "allowWrites": false,
  "dryRun": false
}
```

Set `"dryRun": true` when the user said "dry run".

5. Read **`outputFile`**; summarize with severity labels.
6. Do **not** apply file changes automatically.

## Guardrails

- Never `allowWrites: true` (v0.1).
- No secrets in prompts.
- If gemini unavailable, report detection notes and stop.

## Related

- Skill: `subagent-review` · Rule: `external-cli-subagents.mdc` · Alternative: `/run-codex-review`
