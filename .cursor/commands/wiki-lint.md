---
capability: "wiki-lint agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Cursor:** Same intent as Claude `/wiki-lint`. When customizing, keep in sync with `.cursor/commands/wiki-lint.md`.

# /wiki-lint — Health-check the docs wiki

Audit the wiki for structural problems, stale content, and missed connections. Keep the knowledge base healthy as it grows.

## Schema

Read `docs/OKF_ADOPTION.md` and `docs/WIKI_SCHEMA.md` for the OKF frontmatter profile, conventions, and the full lint checklist.

## Steps

0. **Run automated lint** (from repo root):
   - `python scripts/okf_lint.py --profile project --exclude-prefix archive/`
   - Optional combined structural scan: `python scripts/wiki_lint.py --exclude-prefix archive/`
   - Optional sibling bundle: `python scripts/okf_lint.py ../<sibling-repo>/docs --profile project`
   Use findings as the starting point for manual review below.

1. **Scan all INDEX.md files** — build a list of every page referenced in indexes.
2. **Scan all .md files in `docs/`** — build a list of every page that exists on disk and note which living docs lack OKF frontmatter or a non-empty `type`.
3. **Compare** to find:
   - **Orphan pages** — exist on disk but not in any INDEX.md
   - **Broken index entries** — listed in INDEX.md but file doesn't exist
   - **Broken links** — `[text](path)` references to non-existent files
4. **Check cross-references** — for each page, verify it has at least one inbound link.
5. **Check for stale content** (sample 10-15 pages):
   - Compare claims against current code (e.g., does the API endpoint still exist?)
   - Flag pages that reference removed features or outdated versions
6. **Check for missing pages** — scan for concepts frequently mentioned across pages that lack their own dedicated page.
7. **Check for contradictions** — look for pages that make conflicting claims about the same topic.
8. **Present findings** as a prioritized report:

```markdown
## Wiki Lint Report — YYYY-MM-DD

### Critical (broken references)
- ...

### Structural (orphans, missing indexes)
- ...

### Stale (outdated content)
- ...

### Suggestions (missing pages, new connections)
- ...
```

9. **Ask the user** which fixes to apply.
10. **Apply approved fixes** and update `docs/log.md`: `## [YYYY-MM-DD] lint | Wiki health check`

## Scope Control

- **Quick lint** (default): Steps 1-4 only (structural checks). Fast, no content reading.
- **Full lint**: All steps including content staleness and contradiction checks. Slower, reads many pages.
- User can specify: `/wiki-lint quick` or `/wiki-lint full`

## Done when

- Report presented to user.
- Approved fixes applied.
- Log entry appended.
