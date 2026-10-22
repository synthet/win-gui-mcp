---
capability: "wiki-ingest agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Cursor:** Same intent as Claude `/wiki-ingest`. When customizing, keep in sync with `.cursor/commands/wiki-ingest.md`.

# /wiki-ingest — Process a source into the wiki

Ingest a new source document (article, paper, report, code analysis, or user-provided file) into the docs wiki. The wiki is a persistent, compounding knowledge base — each ingest updates multiple pages.

## Inputs

- Source document: file path, URL, or pasted content from the user message.
- User guidance on what to emphasize (optional).

## Schema

Read `docs/OKF_ADOPTION.md` and `docs/WIKI_SCHEMA.md` for OKF metadata and conventions, page types, and linking rules before proceeding.

## Steps

1. **Read the source** in full. If it's a URL, fetch it. If it's a file, read it.
2. **Summarize key takeaways** to the user (3-5 bullets). Ask what to emphasize or de-emphasize before writing.
3. **Determine page type and location** per the schema (technical, plan, report, guide, reference, architecture).
4. **Write the summary page** in the appropriate `docs/` subfolder following naming conventions, with OKF frontmatter containing at least `type` and preferably `title`, `description`, `resource`, `tags`, `timestamp`, and `okf_version: 0.1`.
5. **Update the folder's INDEX.md** — add the new page with a one-line description.
6. **Update `docs/INDEX.md`** if the page belongs to a category not yet represented there.
7. **Cross-reference existing pages:**
   - Read `docs/INDEX.md` to identify related pages.
   - Read those pages and add bidirectional links where relevant.
   - If new content contradicts existing pages, update them and note the discrepancy.
8. **If the source is a file**, copy or move it to `docs/raw/` (immutable archive).
9. **Append to `docs/log.md`** using the format: `## [YYYY-MM-DD] ingest | Source Title`

## Done when

- Summary page exists in the correct location.
- All relevant INDEX.md files are updated.
- Cross-references added to/from at least 2 existing pages (if related pages exist).
- Log entry appended.
- User has reviewed the summary.

## Example log entry

```markdown
## [2026-04-13] ingest | IAA Models Survey 2025

Ingested survey paper on image aesthetic assessment models. Created [IAA_MODELS_SURVEY_2025.md](reports/IAA_MODELS_SURVEY_2025.md). Updated: [MODELS_SUMMARY.md](technical/MODELS_SUMMARY.md) (added survey refs), [INDEX.md](INDEX.md). Raw source: [docs/raw/iaa_survey_2025.pdf](raw/iaa_survey_2025.pdf).
```
