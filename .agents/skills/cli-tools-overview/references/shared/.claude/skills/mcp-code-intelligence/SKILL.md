---
name: mcp-code-intelligence
description: Use when choosing or comparing code-intelligence/search backends such as text search, ast-grep, Serena, Zoekt, fff MCP, Graphify, embeddings, or IDE semantic search. Apply when repo search is repeated, cross-reference heavy, or plain rg is insufficient.
capability: "mcp-code-intelligence agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# MCP code intelligence

## Purpose

Choose the right code-intelligence layer: shell tools, MCP servers, embeddings.

## When to Use

- Setting up agent MCP servers
- Deciding fff vs rg vs semantic/embedding search
- Evaluating Serena, Zoekt, Graphify, codebase-memory-mcp

## Required Tools

Optional MCP: fff (`ffgrep`, `fffind`, `fff-multi-grep`), Graphify (`query_graph`, `get_node`, …), ast-grep MCP, Serena, Zoekt, embedding servers.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.


## Common Commands

**Tier recommendation:**

```text
Minimal:  rg + fd + read_file + git diff + patch_file (IDE/shell)
Better:   above + ast-grep + gh + task runner + fff MCP (optional)
Advanced: Serena or codebase-memory-mcp + Zoekt + optional Graphify + optional embeddings
```

**fff MCP (optional):** Install [fff-mcp](https://github.com/dmtrKovalenko/fff); add to `.cursor/mcp.json` from `.cursor/mcp.example.json` `_examples.proj-ro-fff`. Reload MCP. Prefer fff for repeated repo file/content search when connected.

**Graphify (optional knowledge graph):** Package is `graphifyy`; CLI is `graphify`.

1. `uv tool install graphifyy` (MCP: `uv tool install "graphifyy[mcp]"`).
2. Build a local graph: `graphify .` (AST code map is free/local; docs/media may use a model backend).
3. Query via CLI (`graphify query` / `path` / `explain`) or copy `_examples.proj-ro-graphify` from `.cursor/mcp.example.json` / `.mcp.json` into gitignored `.cursor/mcp.json` (needs `graphify-out/graph.json`).
4. **Do not** run bare `graphify cursor install` or `graphify install --project` in this scaffold — those write under `.cursor/` / inject AGENTS.md and fight `.claude/` → sync SoT. Prefer this docs + MCP path, or a user-global install outside the repo.

**Using the graph:** once MCP or `graphify-out/graph.json` exists, follow [graphify-knowledge-graph](../graphify-knowledge-graph/SKILL.md) for query/path/god-node workflows.

**Warning:** Embedding-first indexing is memory-heavy — use after rg/ast-grep/fff.

Compare:

| Layer | Best for |
|-------|----------|
| CLI rg/fd | Fast text, gitignore-aware |
| fff MCP | Indexed repo search, multi-pattern grep |
| ast-grep MCP / sg | Structural patterns |
| Serena / ctags | Symbols, refs |
| Zoekt | Large org code search |
| Graphify | Architecture / path / community queries over `graphify-out/` |
| Embeddings | Fuzzy conceptual search (secondary) |

## Agent-Safe Patterns

- Read MCP tool schemas before CallMcpTool.
- No secrets in MCP args; See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

MCP tools that write files or run jobs need user intent. See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- fff not found: add `%LOCALAPPDATA%\fff-mcp\bin` to PATH or use full path in mcp.json.
- graphify-mcp won't start: confirm `graphify-out/graph.json` exists and `graphifyy[mcp]` is installed.
- MCP server won't start: check WORKSPACE_ROOT env.

## Windows Notes

- fff-mcp runs natively on Windows; many MCP servers prefer WSL.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Run Unix-first MCP servers here.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] MCP tier matches task (text before embeddings)
- [ ] fff optional and documented as opt-in
- [ ] Graphify optional and documented as opt-in (no bare cursor install)
- [ ] Tool schemas read before first call
