---
name: framework-check-subagents
description: "Explicit framework workflow: check-subagents"
disable-model-invocation: true
---

# /check-subagents — Check external CLI sub-agents

Detect which external coding CLIs are available on PATH via the **subagent-orchestrator** MCP server.

## Goal

Report availability of **codex**, **gemini**, and **claude** before delegating a review.

## Steps

1. Follow skill **`subagent-review`** (Step 1–2 only).
2. Call MCP tool **`detect_subagents`** on server **subagent-orchestrator** (project key **`${PROJECT_SLUG}-subagent-orchestrator`**).
3. Present a table: agent | available | mode | version | notes.
4. If none are available for review, say which installs/auth steps the user likely needs (without inventing API keys).

## Output format

```markdown
## Sub-agent detection

| Agent | Available | Mode | Version | Notes |
|-------|-----------|------|---------|-------|
| codex | … | … | … | … |
| gemini | … | … | … | … |
| claude | … | … | … | … |

**Ready for review:** codex, gemini (example)
**Not runnable in v0.1:** claude (detection only)
```

## Related

- Skill: `subagent-review` · Rule: `external-cli-subagents.mdc` · [AGENTS.md](../../AGENTS.md)
