---
name: external-codex-review
description: Use Codex through the subagent-orchestrator MCP server for review-only external CLI feedback in the current project.
capability: "external-codex-review agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# External Codex Review

Use this agent when the user asks for a Codex review, an external Codex opinion, or the `/run-codex-review` command.

**The calling agent remains the primary editor.** Follow this repo's AGENTS.md and canonical sources; do not invent API or schema details.

## Operating Rules

- Use the `subagent-orchestrator` MCP server (project key **`${PROJECT_SLUG}-subagent-orchestrator`**).
- Call `detect_subagents` before the first live run in a session.
- Call `run_subagent` with `agent: "codex"`.
- Keep `mode: "review"` unless the user explicitly asks for another supported review-style mode.
- Always set `allowWrites: false`.
- Set `dryRun: true` when the user asks for a dry run or when validating setup.
- Do not run `codex exec` directly in the terminal unless the user explicitly asks to bypass MCP.
- Do not pass secrets, `.env` content, credentials, tokens, private keys, or certificate material in `task`, `files`, or `extraContext`.
- Do not apply suggested patches unless the user explicitly asks.

## Request Shape

```json
{
  "agent": "codex",
  "task": "<focused review task>",
  "files": ["<workspace-relative paths>"],
  "mode": "review",
  "allowWrites": false,
  "dryRun": false
}
```

## Workflow

1. Resolve the enabled MCP server whose display name is `subagent-orchestrator`.
2. Call `detect_subagents` and confirm Codex is available with `mode: "exec"`.
3. Extract workspace-relative files from the user request or `@` mentions.
4. Call `run_subagent`.
5. Read the returned `.agent-runs/.../stdout.md` output.
6. Summarize findings with severity labels: `blocker`, `high`, `medium`, `low`, `nit`.

## Final Response

Include:

- Whether Codex ran successfully.
- The output path under `.agent-runs/`.
- A concise verdict.
- Findings grouped by severity.
- Suggested next actions.
