# MCP tool reference - synthet-agent-swarm

Server display name: **synthet-agent-swarm**. The field contract is `docs/api.md` in the synthet-agent-swarm checkout.

Tools are read-only. `swarm_search` lists harnesses. `swarm_run` runs one task. `run_judge_panel` runs a frozen manifest. The server does not call Jev or render images.

## swarm_search

Optional `query` matches id, title, command, transport, and notes.

```json
{ "query": "cursor" }
```

Each row has `id`, `title`, `command`, `available`, `transports`, `modelFlag`, and `notes`.

## swarm_run

| Field | Required | Notes |
|---|---|---|
| `task` | yes | Max 8000 characters |
| `files` | no | Workspace-relative. Max 20 |
| `harnesses` | no | Omit for every available harness. `antigravity` is `agy`. `agent` is `cursor` |
| `model` | no | Fails the whole call with `model_unsupported` when a selected harness has no `--model` flag |
| `transport` | no | `auto` (default), `cli`, or `acp` |
| `mode` | no | `review` by default. `judge` is one blind vote |
| `timeoutMs` | no | Clamped to the configured maximum |
| `dryRun` | no | Does not spawn. Omits `agreement` |
| `extraContext` | no | Max 4000 characters |

One harness omits `agreement`. Two or more include strict-majority agreement. `ok` is true when at least one harness succeeds.

```json
{
  "task": "Review the sandbox flags",
  "files": ["agent-orchestrator/src/runners/codex.ts"],
  "harnesses": ["codex", "agy"],
  "dryRun": true
}
```

Failure envelope:

```json
{
  "ok": false,
  "error": {
    "code": "model_unsupported",
    "message": "",
    "details": {},
    "retryable": false,
    "next_steps": ["codex"]
  },
  "harnesses": []
}
```

## run_judge_panel

Same manifest as before, plus optional `harnesses`, `model`, and `transport`. Blind is the default and rejects `extraContext`. `stages.jev.called` and `stages.rules.applied` stay false.

```json
{
  "task": "Choose A or B for each item.",
  "items": [{ "id": "a", "files": ["blind/a.jpg"] }],
  "harnesses": ["codex", "agy"],
  "dryRun": true
}
```

## Aliases

`detect_subagents`, `run_subagent`, and `run_judge_swarm` call the same functions and return the previous JSON shape. `antigravity` in those responses is the `agy` harness.

## On-disk artifacts

```text
.agent-runs/YYYYMMDD-HHMMSS-<harness>-<slug>/
.agent-runs/YYYYMMDD-HHMMSS-swarm-<slug>/swarm.json
.agent-runs/YYYYMMDD-HHMMSS-panel-<slug>/panel.json
```
