---
name: graphify-knowledge-graph
description: Use when querying architecture, concept paths, god nodes, or communities via Graphify MCP or CLI; when the user mentions graphify, knowledge graph, GRAPH_REPORT, graphify-out, query_graph, or asks how two modules connect. Prefer this over broad rg/fff for structural “how does A relate to B” questions when the graph or MCP is available. Do not use for setup-tier comparisons (see mcp-code-intelligence) or ordinary text/filename search.
capability: "Query and refresh the local Graphify knowledge graph via MCP (preferred) or CLI."
side_effect_level: local_write
approval_required: false
requires_tools: "Graphify MCP (proj-ro-graphify) and/or graphify CLI; graphify-out/graph.json; GetMcpTools + CallMcpTool"
output_schema: "Scoped graph answer: nodes/edges/path, or setup/refresh status with next step."
risk_class: medium
---

# Graphify knowledge graph

Query the local knowledge graph before grepping the whole tree for architecture questions.

## When to use

- How do two concepts/modules/files connect?
- What are the hub (god) nodes or communities?
- User says `/graphify`, `graphify query`, or points at `graphify-out/`
- PR blast-radius against the graph (`list_prs` / `get_pr_impact` / `triage_prs`) when those tools are needed

**Not this skill:** choosing search backends → [mcp-code-intelligence](../mcp-code-intelligence/SKILL.md). Filename/text search → [search-tool-selection](../search-tool-selection/SKILL.md).

## Prerequisites

1. CLI: `uv tool install graphifyy` (MCP: `uv tool install "graphifyy[mcp]"`).
2. Graph built: `graphify update .` (AST-only) or assistant `/graphify .`.
3. MCP (preferred): `_examples.proj-ro-graphify` copied into gitignored `.cursor/mcp.json`; reload MCP.

**Do not** run bare `graphify cursor install` / `graphify install --project` in this scaffold (fights `.claude/` → sync SoT). See [AGENTS.md](../../../AGENTS.md).

## Procedure

### 1. Confirm graph or MCP

- Prefer MCP server key matching `proj-ro-graphify` / `graphify` (Cursor may prefix the project name).
- If MCP missing but `graphify-out/graph.json` exists, use CLI: `graphify query` / `path` / `explain` / `god-nodes`.
- If neither exists: tell the operator to install + `graphify update .`, then optionally enable MCP. Do not invent graph answers.

### 2. Discover schemas then call

1. `GetMcpTools` for the Graphify server (or one tool) before the first `CallMcpTool`.
2. Pick the tool from [references/mcp-tools.md](references/mcp-tools.md).
3. Keep `token_budget` / depth bounded; raise budget only if truncated.

### 3. Route by question

| Ask | Prefer |
|-----|--------|
| Broad “what’s in the graph?” | `graph_stats` then `god_nodes` |
| Natural-language / keyword | `query_graph` (`bfs` default; `dfs` to trace) |
| How A connects to B | `shortest_path` then `get_neighbors` / `get_node` |
| One symbol / file node | `get_node` |
| Community membership | `get_community` |
| Open PR impact | `list_prs` / `get_pr_impact` / `triage_prs` (needs GitHub access) |

### 4. After code edits (optional refresh)

AST-only refresh (no LLM): `graphify update .`. Doc/paper semantic refresh needs a model backend — only when the user asks.

## Agent-safe patterns

- Read-only graph queries are default; do not push Neo4j/HTTP export unless explicitly requested.
- No secrets in MCP args. PR tools may call GitHub — use only when the user asked about PRs.
- If MCP fails: fall back to CLI against `graphify-out/graph.json`, then rg/fff.

## Fallback CLI

```bash
graphify god-nodes --top 10
graphify query "your question" --budget 2000
graphify path "Source" "Target"
graphify explain "Concept"
graphify update .
```

Prefer MCP `graph_stats` / `god_nodes` / `query_graph` / `shortest_path` when connected.
## Evaluation prompts (trigger checks)

1. “How does `sync_assistant_trees` relate to Cursor skills?” → this skill + `query_graph` / path tools.
2. “What are the god nodes in this repo?” → this skill + `god_nodes`.
3. Near miss: “find the string `WORKSPACE_ROOT` in mcp configs” → search-tool-selection / rg/fff, not Graphify.

## Verification checklist

- [ ] Schemas read before first Graphify MCP call
- [ ] Answer cites graph nodes/edges (or honest “no path / graph missing”)
- [ ] Did not recommend bare `graphify cursor install` in this scaffold
- [ ] After substantial code edits, offered `graphify update .` when relevant
