---
name: subagent-review
description: Use when the user requests external CLI review, codex/gemini review, a subagent panel, tie-breaker, or invokes /check-subagents, /run-codex-review, /run-gemini-review, or /run-subagent-review. Runs detect_subagents and review-mode subagents, then summarizes findings.
capability: "subagent-review agent asset workflow"
side_effect_level: external_export
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: high
---

# Subagent review (MCP)

Run external CLI coding agents **through the subagent-orchestrator MCP server** — not by spawning `codex` / `gemini` / `claude` directly in the terminal.

## Prerequisites

- Sibling checkout: `../subagent-orchestrator/agent-orchestrator` built (`npm install && npm run build` there once)
- MCP server enabled in [`.cursor/mcp.example.json`](../../../.cursor/mcp.example.json) as **`cli-review`** (or user-level **`subagent-orchestrator`**) and reloaded in Cursor

## Step 1 — Resolve MCP server

Before `CallMcpTool`, find the enabled server where `serverName` is `subagent-orchestrator` (project key **`cli-review`**; user-level keys may also appear). Read tool schemas from `mcps/*/tools/detect_subagents.json` and `run_subagent.json`. Prefer the **project** server so `WORKSPACE_ROOT` is this repo.

## Step 2 — Detect

Call **`detect_subagents`** (no arguments). Present a short table:

| Agent | Available | Mode | Notes |
|-------|-----------|------|-------|

Note: **Claude** is detection-only in v0.1 — do not rely on it for live `run_subagent`.

## Step 3 — Build request

- **task** — clear review goal (from user or slash command text)
- **files** — workspace-relative paths from `@` mentions (max 20; no `.env`, keys, binaries). Before calling `run_subagent`, validate the exact file list from the repo root with `python scripts/agent_policy/validate_export.py --external-export --approval-marker APPROVED_EXTERNAL_EXPORT <files...>` and stop if it fails.
- **mode** — `review` (default) or `tie-breaker` when comparing opinions
- **allowWrites** — always `false`
- **dryRun** — `true` when user says “dry run” or you are validating setup

## Step 4 — Run

Call **`run_subagent`** with JSON per [references/mcp-tools.md](references/mcp-tools.md).

For a “panel” (user wants both agents): run **sequentially** — codex then gemini (or vice versa) with the same `task` and `files`. There is no `run_subagent_panel` tool in v0.1.

## Step 5 — Read results

1. Parse MCP response (`outputFile`, `summary`, `ok`, `error`).
2. Read `.agent-runs/.../stdout.md` at the returned path.
3. Summarize for the user:

```markdown
## External review summary

### Verdict
### Findings (blocker | high | medium | low | nit)
### Suggested changes
### Agent agreement (if multiple runs)
### Next steps
```

Do **not** apply patches unless the user explicitly asks.

## File selection

- When the user did not `@` paths, suggest **files** from `git diff --name-only` (changed files in
  the working branch); cap at ~20 and exclude `.env`, secrets, and binaries. Validate the final list with `scripts/agent_policy/validate_export.py` before export/review.
- Cross-repo changes: review **this workspace only**; open a sibling repo separately for its diffs
  (v0.1 cannot span both roots in one MCP call).
- Safety: [.agent/SAFETY.md](../../../.agent/SAFETY.md)

## Errors

| Situation | Action |
|-----------|--------|
| Agent not on PATH | Report `detect_subagents` notes; suggest install/auth |
| `allowWrites` rejected | Explain v0.1 is review-only |
| Claude run requested | Use Codex or Gemini instead |
| MCP server missing | [docs/EXTERNAL_CLI_REVIEWS.md](../../../docs/EXTERNAL_CLI_REVIEWS.md); build sibling orchestrator |

## Related

- Rule: `external-cli-subagents.mdc`
- Commands: `/check-subagents`, `/run-codex-review`, `/run-gemini-review`, `/run-subagent-review`
- Orchestrator package: `../subagent-orchestrator/agent-orchestrator/README.md`
