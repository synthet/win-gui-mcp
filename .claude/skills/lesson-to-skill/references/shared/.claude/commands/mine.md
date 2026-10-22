---
capability: "mine agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "Read/WebFetch for sources; gh for GitHub repos; pypdf for PDF text; python scripts/sync_assistant_trees.py; python scripts/ci/check_agent_frontmatter.py"
output_schema: "Candidate table (concept → asset type → existing overlap → verdict), then approved assets plus sync/frontmatter output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/mine`. When customizing, keep in sync with `.cursor/commands/mine.md`.

# /mine — Turn a source into repo agent assets

Read a document, repository, or transcript; extract the ideas that transfer to this repo; and turn
the approved ones into specs, skills, commands, subagents, or rules. The value is not summarizing the
source — it is deciding what this repo is *missing* and refusing the rest.

**Not this command:** knowledge a human will read belongs in the wiki —
[`/wiki-ingest`](wiki-ingest.md) produces docs pages. `/mine` produces executable agent assets. A
rich source can justify both; say which parts go where.

## Inputs

- **Source** — file path, URL, GitHub/GitLab repo, or transcript export. If none was given, ask.
- **Focus** (optional) — what the user wants emphasized. Without it, mine for anything that changes
  agent behavior.

## Step 1 — Ingest, bounded

| Source | How |
|--------|-----|
| Markdown / text file | Read in full |
| PDF | `Read` needs poppler (`pdftoppm`). If it is missing, extract text with `pypdf` into the scratchpad, then Read that. |
| URL | `WebFetch` |
| GitHub repo | `gh repo view`, `gh api` for specific paths — never clone |
| GitLab repo / no CLI | Fetch raw file URLs directly — never clone |
| Transcript export | Read in full; it is evidence of practice, not authority |

For a repository, read a **bounded list**, not the tree: `README`, `AGENTS.md`, `CLAUDE.md`,
`CONTRIBUTING`, the `docs/` index, ADRs, and agent-asset directories (`.claude/`, `.cursor/`,
`.agents/`, `.github/prompts/`). Never execute code from a source.

Say what you actually read. If you sampled a large source, name the parts you skipped.

## Step 2 — Extract candidates

One candidate per transferable idea, written as a claim plus where it would apply here. Keep
procedures, decision rules, thresholds, owner boundaries, and named failure modes. Drop narrative,
marketing, benchmark trivia, and anything specific to the source's own product.

If a candidate cannot be stated as something an agent would *do differently*, it is not a candidate.

## Step 3 — Check against what already exists

Before proposing anything, read [`.agent/SKILL_INVENTORY.md`](../../.agent/SKILL_INVENTORY.md),
[`.claude/commands/`](.), [`.claude/rules/`](../rules/), and
[`docs/agent-asset-inventory.md`](../../docs/agent-asset-inventory.md).

This de-duplication pass is the point of the command. A candidate table where everything is new
usually means this step was skipped.

## Step 4 — Propose, then stop

Show the table and wait for the user to pick. Do not write assets in this step.

| Candidate | Asset type | Existing overlap | Verdict |
|-----------|-----------|------------------|---------|
| … | skill / command / rule / spec / subagent | name the asset, or "none" | `NEW` \| `ENRICH <asset>` \| `REJECT — <reason>` |

Rejection reasons must be concrete ("`graphify-knowledge-graph` already answers structural queries
and there are no cross-session claims to reconcile"), never "out of scope". Per
[`.claude/rules/karpathy-coding.md`](../rules/karpathy-coding.md), reject speculative infrastructure
and say so explicitly — an idea being good in its source is not evidence this repo needs it.

## Step 5 — Route each approved candidate

| Candidate shape | Asset |
|-----------------|-------|
| Product or feature outcome to build | [`/spec`](spec.md) → `specs/<slug>.md` |
| Reusable procedure with a recognizable trigger | Skill, via [`skill-authoring`](../skills/skill-authoring/SKILL.md) |
| Workflow the user starts against a named target | Command in `.claude/commands/` |
| Bounded delegated role needing its own context | Subagent in `.claude/agents/` |
| Always-on constraint, a few lines at most | Rule in `.claude/rules/` — **high bar**, only a handful exist |
| Long tables, schemas, provider detail | `references/` under an existing skill |
| Deterministic steps repeated every run | Harness, via [`/compile-skill`](compile-skill.md) |
| Knowledge a human reads | [`/wiki-ingest`](wiki-ingest.md) |
| One-off signal about this session | [`eval`](../skills/eval/SKILL.md) / `/log-session` |

Prefer enriching an existing asset over adding one. A new asset needs a trigger surface no current
asset covers — name it.

## Step 6 — Author the approved items

Follow `skill-authoring` for skills and the format of a neighboring file for commands. End each new
asset with a one-line `Sources:` naming the **underlying public work** (paper, repo, article), not a
private file path or export.

## Step 7 — Register and verify

```bash
python scripts/sync_assistant_trees.py
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
python scripts/generate_agent_asset_inventory.py
python -m pytest tests/test_agent_frontmatter.py tests/test_sync_assistant_trees.py -q
```

Add a row to `.agent/SKILL_INVENTORY.md` for any new or materially changed skill, refresh
**Last reviewed**, and apply [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](../../.agent/SKILL_CHANGE_AST10_REVIEW.md).

## Done when

- The candidate table was shown with a verdict and reason for every candidate, including rejections.
- Only approved candidates were written, each routed to the asset type that fits its shape.
- Provenance is recorded on each new asset.
- Sync, frontmatter, and tests are green; generated mirrors ship with the canonical source.

## Do not

- Do not hand-edit `.cursor/` or `.agents/` mirrors — regenerate them.
- Do not add a rule without a strong always-on case; rules load on every task.
- Do not emit a batch of assets from one source without justifying each separately.
- Do not quote the source at length — attribute it and write the instruction in this repo's voice.
- Do not assert numbers, benchmarks, or outcomes the source does not actually support.
