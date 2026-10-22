# Agent safety and hygiene — ${PROJECT_NAME}

## Secrets and credentials

- Never commit `secrets.json`, real `.env`, API keys, tokens, or connection strings with passwords.
- Never paste live credentials into prompts, logs, or tool arguments when avoidable. Use `.env.example`
  for non-secret defaults.

## Generated and local artifacts

- Do not commit build outputs, caches, large binaries, personal scratch scripts, or machine-specific paths.
- Avoid bulk outputs under `output/`, `dist/`, `backups/`, or similar unless the repo expects them
  (prefer `.gitignore`).

## Contracts and terminology

- Do not invent public API paths, payload fields, DB column names, config keys, or status enums.
  Use [docs/CANONICAL_SOURCES.md](../docs/CANONICAL_SOURCES.md) as the authority map.


## Permission modes

- The active default Claude settings are intentionally **read-only**: `.claude/settings.json` allows `Bash(git status)`, `Bash(git diff:*)`, `Bash(git log:*)`, and `WebSearch` only.
- Local repository writes are opt-in. Use `.claude/settings.write.example.json` as a template when a maintainer explicitly wants local staging/committing (`git add`, `git commit`), and prefer enabling the smallest needed subset for the current task.
- Remote writes are not the same as local writes. `gh pr`, `gh issue`, and `gh project` mutate GitHub state, may trigger notifications/automation, and require explicit task intent plus verification of the target owner/repo/project.
- External export requires separate explicit approval before sending source files, prompts, logs, artifacts, review bundles, or other project data to third-party services/providers. Local-write approval does not imply export approval.
- Seeded projects inherit the safer read-only default because the scaffold ships `.claude/settings.json` without write-capable commands.

## Tooling / MCP

- Treat write-capable tools (run jobs, mutate config, execute arbitrary code, destructive SQL) as
  **high risk**. Prefer read-only diagnostics unless the user explicitly requests writes — see
  [.agent/workflows/safe_mcp_diagnostics.md](workflows/safe_mcp_diagnostics.md).
- On shared machines, a live MCP endpoint may attach to a running process; do not assume isolation.

## External CLI reviews (subagent-orchestrator)

- Review-only: never set `allowWrites: true` on `run_subagent`.
- Selected source files are sent to external providers (e.g. Codex / Gemini) per their policies;
  do not use for proprietary code you cannot export.
- Never include `secrets.json`, `.env`, credentials, or a full `config.json` in `task`, `files`, or
  `extraContext`.
- Outputs land in `.agent-runs/` (gitignored); treat as sensitive until reviewed.

## Git

- Never modify `.git/config` or add non-standard extensions (do not set `extensions.worktreeConfig`
  or change `core.repositoryformatversion`). Embedded git libraries in third-party tooling choke on
  non-standard extensions. If a worktree is needed, use a temporary one and clean it up immediately.

## Docs

- Prefer small linked pages over monolithic dumps.
- After wiki-affecting edits, update indexes per [docs/WIKI_SCHEMA.md](../docs/WIKI_SCHEMA.md) and
  append [docs/log.md](../docs/log.md).
