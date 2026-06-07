# Skill inventory — win-gui-mcp

Project skills live under [`.cursor/skills/`](../.cursor/skills/). Each skill has YAML front matter (`name`, `description`).

| Skill | When to use | Related commands |
|-------|-------------|------------------|
| `win-gui-mcp-tools` | Implementing MCP tools, phase-gate testing | `/implement-tool`, `/phase-gate` |
| `win-gui-mcp-implementation` | Scaffolding modules, Phase 1 bootstrap | `/bootstrap-phase1`, `/implement` |
| `mcp-server-design` | MCP transport, tool registration, schema review | `/mcp-tool-review` |
| `security-review` | Security pass, elevation/UI automation changes | `/mcp-tool-review` |
| `commit-conventions` | Commit messages, PR titles | `/pr-ready`, `/release-notes` |

## Command ↔ skill map

| Command | Primary skill |
|---------|---------------|
| `/implement-tool` | `win-gui-mcp-tools` |
| `/bootstrap-phase1` | `win-gui-mcp-implementation` |
| `/implement` | `win-gui-mcp-implementation` |
| `/phase-gate` | `win-gui-mcp-tools` |
| `/mcp-tool-review` | `mcp-server-design`, `security-review` |
| `/pr-ready` | `commit-conventions` |

## Authority docs (all skills)

- [docs/CANONICAL_SOURCES.md](../docs/CANONICAL_SOURCES.md)
- [docs/tools.md](../docs/tools.md)
- [docs/architecture.md](../docs/architecture.md)
- [docs/errors.md](../docs/errors.md)
- [docs/security.md](../docs/security.md)
