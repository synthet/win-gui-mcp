# Agent infrastructure inventory — win-gui-mcp

Source of truth for paths: [AGENTS.md](../AGENTS.md). Canonical specs: [docs/CANONICAL_SOURCES.md](../docs/CANONICAL_SOURCES.md).

## Rules (`.cursor/rules/`)

| File | alwaysApply | Purpose |
|------|-------------|---------|
| `agent-canonical-sources.mdc` | true | Spec authority; no invented APIs |
| `sdlc-core.mdc` | true | Small diffs, dotnet build, no secrets |
| `00-win-gui-mcp-overview.mdc` | true | Product overview, Phase 1 scope |
| `20-security-and-safety.mdc` | true | UIPI, elevation, actuation safety |
| `spec-and-planning.mdc` | false | Spec-first planning |
| `implementation.mdc` | false | C# style, architecture modules |
| `review-and-ci.mdc` | false | PR hygiene, CI |

## Commands (`.cursor/commands/`)

| Command | File |
|---------|------|
| `/spec` | `spec.md` |
| `/plan` | `plan.md` |
| `/implement` | `implement.md` |
| `/test-and-fix` | `test-and-fix.md` |
| `/pr-ready` | `pr-ready.md` |
| `/release-notes` | `release-notes.md` |
| `/implement-tool` | `implement-tool.md` |
| `/bootstrap-phase1` | `bootstrap-phase1.md` |
| `/phase-gate` | `phase-gate.md` |
| `/mcp-tool-review` | `mcp-tool-review.md` |

## Skills (`.cursor/skills/`)

| Skill | Path |
|-------|------|
| `win-gui-mcp-tools` | `win-gui-mcp-tools/SKILL.md` |
| `win-gui-mcp-implementation` | `win-gui-mcp-implementation/SKILL.md` |
| `mcp-server-design` | `mcp-server-design/SKILL.md` |
| `security-review` | `security-review/SKILL.md` |
| `commit-conventions` | `commit-conventions/SKILL.md` |

Details: [SKILL_INVENTORY.md](SKILL_INVENTORY.md)

## Subagents (`.cursor/agents/`)

| Agent | Path |
|-------|------|
| `pr-ready-hygiene` | `pr-ready-hygiene.md` |

## MCP config

| File | Purpose |
|------|---------|
| `.cursor/mcp.example.json` | Committed template |
| `.cursor/mcp.json` | Gitignored operator copy |

## Hub docs (`.agent/`)

| File | Purpose |
|------|---------|
| `AGENT_INFRA_INVENTORY.md` | This file |
| `SKILL_INVENTORY.md` | Skill triggers and command mapping |
| `COMMANDS.md` | Operator build/run quick reference |
| `SAFETY.md` | Windows UI automation guardrails |
