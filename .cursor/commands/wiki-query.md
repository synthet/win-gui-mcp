---
capability: "wiki-query agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Cursor:** Same intent as Claude `/wiki-query`. When customizing, keep in sync with `.cursor/commands/wiki-query.md`.

# /wiki-query — Query the wiki and optionally file the answer

Answer a question by searching the docs wiki. Good answers can be filed back as new wiki pages so explorations compound in the knowledge base.

## Inputs

- A question or topic from the user message.
- Optional: `--file` flag to save the answer as a wiki page.

## Schema

Read `docs/OKF_ADOPTION.md` and `docs/WIKI_SCHEMA.md` for OKF metadata and conventions.

## Steps

1. **Read `docs/INDEX.md`** to identify relevant pages. Scan folder-level INDEX.md files if needed.
2. **Read the relevant pages** (typically 3-10 pages depending on scope).
3. **Synthesize an answer** with inline citations linking to source pages: `[ARCHITECTURE.md](architecture/system-overview.md)`.
4. **Present the answer** to the user in the chat.
5. **Ask if the answer should be filed** as a wiki page (or file automatically if `--file` was specified).
6. If filing:
   - Write the answer as a page in `docs/reports/` (synthesis/report type) with OKF frontmatter.
   - Update `docs/reports/INDEX.md`.
   - Add cross-references from cited pages back to the new synthesis page.
   - Append to `docs/log.md`: `## [YYYY-MM-DD] query | Question Summary`

## Answer Format

Structure answers as:

```markdown
## Answer

[Synthesized response with citations]

### Sources

- [PAGE_NAME.md](path) — what this page contributed to the answer
- [PAGE_NAME.md](path) — what this page contributed to the answer
```

## Done when

- Question is answered with citations to specific wiki pages.
- If filed: page exists, indexes updated, log entry appended.

## When to file

File the answer when it represents **durable, reusable knowledge** — comparisons, cross-cutting analysis, architectural decisions, research synthesis. Don't file simple lookups or ephemeral answers.
