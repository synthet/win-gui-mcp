---
name: llm-wiki
description: Use when searching, asking, ingesting, compiling, reviewing, refreshing, validating, or answering from the shared local LLM Wiki (CLI `llmwiki` or MCP server `llmwiki-ro-core`).
capability: "evidence-bound LLM Wiki workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "llmwiki CLI or llmwiki-ro-core MCP server"
output_schema: "Cited answer or structured operation result"
risk_class: medium
---

# LLM Wiki (shared)

Use the installed `llmwiki` CLI or the `llmwiki-ro-core` MCP server. Do **not** edit
`.llmwiki/wiki.db`, object files, or generated `wiki/pages/*.md` directly.

Select the existing wiki root from the connected MCP configuration, an explicit user path,
or `LLMWIKI_ROOT`. Do not initialize or relocate it implicitly. In examples below,
`<wiki-root>` denotes that resolved path.

## Find and answer

1. Prefer MCP tools from `llmwiki-ro-core` when connected.
2. Start with reviewed-only search/ask. Non-reviewed states need an explicit filter and visible labels.
3. Return exact source-revision/evidence citations. If support is inadequate, say so; do not invent facts.

CLI fallback:

```bash
llmwiki --root "<wiki-root>" search "query" --json
llmwiki --root "<wiki-root>" ask "question" --json
llmwiki --root "<wiki-root>" sources --json
```

## Ingest / compile / review

Writes and review require elevated MCP flags (`--allow-writes` / `--allow-review`) or explicit CLI
commands with the user's reviewer identity. Generated facts stay `candidate` until human review.
After canonical changes: `llmwiki --root "<wiki-root>" validate` then `render`.

Consult the target wiki's `AGENTS.md`, `docs/schema.md` and current CLI help for its schema and modes.

Resolve helper paths from this skill directory. Run project commands from the target workspace.
