---
name: search-tool-selection
description: Use before any code-finding task to choose fd/find vs rg vs ast-grep vs semgrep vs fff MCP vs IDE search. Apply when deciding filename, text, syntax, symbol, config, or repeated repo-wide search strategy.
capability: "search-tool-selection agent asset workflow"
side_effect_level: read_only
approval_required: false
requires_tools: "python .claude/skills/search-tool-selection/scripts/harness.py; optional fd, rg, ast-grep, fff MCP"
output_schema: "Recommendation: task, use, avoid, notes"
risk_class: low
---

# Search tool selection (compiled harness)

## Purpose

Route agents to the correct search tool **before** running commands. The decision table is
compiled into `scripts/harness.py` — do not re-derive it from prose.

## When to Use

- Any code-finding or exploration task
- `rg` returns too many false positives
- Unsure whether to use shell, MCP, or IDE tools

## Required Tools

Optional: `fd`, `rg`, `ast-grep`, fff MCP (`ffgrep`, `fffind`, `fff-multi-grep`), IDE Grep/SemanticSearch/Glob.
Harness: `python .claude/skills/search-tool-selection/scripts/harness.py`.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.

## Common Commands

**Compiled router (preferred):**

```bash
python .claude/skills/search-tool-selection/scripts/harness.py --list --json
python .claude/skills/search-tool-selection/scripts/harness.py --task-type content --json
```

Task types: `filename`, `content`, `syntax`, `security`, `symbols`, `layout`, `config`, `repeated`, `cursor`.

**LLM slot:** map a vague user ask to `--task-type`, then follow the harness `use` / `avoid` fields.

**Default escalation:** `fd` (filename) → `rg` (content) → `ast-grep` (syntax) → `bat`/`Read` (slice).

**grep vs rg:** Always prefer `rg`. Use `grep` only if `rg` is unavailable.

**fff MCP:** When connected, prefer fff for repo-wide search; keep one-off probes as `rg`/`fd`.

**Graphify (optional):** For architecture / “how does A connect to B” / community questions when `graphify-out/graph.json` exists (or Graphify MCP is connected), use [graphify-knowledge-graph](../graphify-knowledge-graph/SKILL.md) (`query_graph` / `path` / `explain`). Fall back to rg/fff when the graph is absent. Do not run bare `graphify cursor install` in this scaffold.

**fzf:** Humans only — agents use `--max-count`, `-l`, `-g`, explicit paths.

Exclude globs: `node_modules`, `dist`, `build`, `.git`.

## Agent-Safe Patterns

- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).
- Stop escalating when the task is satisfied — do not chain all tools by default.

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Too many rg hits: narrow path, add `-g '!tests'`, or switch to `--task-type syntax`.
- fff MCP unavailable: fall back to `rg`/`fd`; see mcp-code-intelligence for setup.
- Architecture questions with no graph: fall back to rg/fff; Graphify setup in mcp-code-intelligence.
- Unknown task type: run harness `--list` and pick the closest id.

## Windows Notes

- IDE tools often beat shell on Windows for first pass (`--task-type cursor`).
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- ast-grep and semgrep often smoother in WSL for large repos.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] Ran harness with a concrete `--task-type` (or listed types)
- [ ] Picked tool matches task type (name vs text vs syntax)
- [ ] Output bounded before reading files
- [ ] fff MCP used only when server is connected
- [ ] Graphify used only when graph/MCP is available
