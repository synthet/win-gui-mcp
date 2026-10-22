# Graphify MCP tools

Server example key: `proj-ro-graphify` (Cursor may show a project-prefixed name). Read schemas with `GetMcpTools` before calling.

| Tool | Required args | Use when |
|------|---------------|----------|
| `graph_stats` | — | Sanity check: node/edge/community counts |
| `god_nodes` | optional `top_n` (default 10) | Architectural hubs |
| `query_graph` | `question`; optional `mode` (`bfs`/`dfs`), `depth` (1–6), `token_budget`, `context_filter` | NL or keyword search over the graph |
| `shortest_path` | `source`, `target`; optional `max_hops` | How two labels connect |
| `get_node` | `label` | One node’s details |
| `get_neighbors` | `label`; optional `relation_filter` | Direct edges from a node |
| `get_community` | `community_id` | All nodes in a community |
| `list_prs` | optional `base`, `repo` | Open PRs + graph impact (GitHub) |
| `get_pr_impact` | `pr_number`; optional `repo` | One PR’s graph blast radius |
| `triage_prs` | optional `base`, `repo` | Review/merge priority from graph impact |

Optional on every tool: `project_path` — absolute directory containing `graphify-out/graph.json` (defaults to the graph the server was started with).

## Setup reminder

1. `uv tool install "graphifyy[mcp]"`
2. `graphify update .` (or full `/graphify .`) so `graphify-out/graph.json` exists
3. Copy `_examples.proj-ro-graphify` from `.cursor/mcp.example.json` into `.cursor/mcp.json`
4. Reload MCP in the IDE

Do **not** use bare `graphify cursor install` in this repository scaffold.
