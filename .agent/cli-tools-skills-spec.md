# CLI tool skills — maintenance contract

Vendored from the agent CLI tools enablement prompt. **Canonical skill paths** in this repo:

```text
.claude/skills/<name>/SKILL.md
```

Mirror to `.cursor/skills/` via `python scripts/sync_assistant_trees.py`. Do not use repo-root `skills/`.

## Goal

Maintain practical agent skills for lightweight CLI tools on Windows, WSL2, macOS, and Linux: fast search, safe edits, bounded output, and reliable verification.

## Required deliverables (12 topic skills)

| Skill directory | Role |
|-----------------|------|
| `cli-tools-overview` | Router + links to shared references |
| `safe-command-patterns` | Bounded output, inspect-before-edit, confirmation gates |
| `install-checklist` | Human workstation provisioning (not agent-automated) |
| `search-and-navigation` | rg, fd, fzf, tree, eza, zoxide, bat, delta |
| `structural-code-search` | ast-grep, semgrep, tree-sitter, ctags |
| `git-and-diff-workflows` | git, gh, git-filter-repo, gitleaks |
| `data-config-tools` | jq, yq, dasel, sqlite3, curl, httpie |
| `task-env-package-tools` | just, mise, uv, docker, project runners |
| `lint-format-security` | ruff, prettier, eslint, trivy, shellcheck |
| `mcp-code-intelligence` | MCP tiers; CLI vs structural vs embeddings |
| `windows-agent-tooling` | Native Windows host guidance |
| `wsl2-agent-tooling` | WSL2 build/test/MCP guidance |

**Net-new router (13th CLI skill):** `search-tool-selection` — when to pick fd → rg → ast-grep (and fff MCP when connected).

Shared references live under `cli-tools-overview/references/`:

- `install-blocks.md` — winget / apt / Homebrew install blocks
- `install-tiers.md` — Tier 0 → Block A → Block B → deferred; install scopes
- `agent-environment.md` — PATH contract, Cursor restart, smoke tests
- `bounded-output-patterns.md` — bounded search/read/git patterns
- `commands-requiring-confirmation.md` — destructive / auto-fix gates
- `windows-wsl-split.md` — Windows host vs WSL2 workloads

## Skill file requirements

Each `SKILL.md` must include these headings (in order):

1. Purpose
2. When to Use
3. Required Tools
4. Install (### Windows PowerShell, ### WSL2 Ubuntu, ### macOS)
5. Common Commands
6. Agent-Safe Patterns
7. Commands Requiring Confirmation
8. Troubleshooting
9. Windows Notes
10. WSL2 Notes
11. Verification Checklist

YAML frontmatter: `name` (first key, matches directory) + non-empty `description`.

## Agent-safe command rules

- Prefer bounded output; avoid dumping whole files.
- Prefer `rg`, `fd`, `sed -n`, `bat --line-range`, `git diff --stat`.
- Never recommend destructive commands without explicit confirmation.
- Always `git status --short` before editing; show `git diff --stat` before finalizing.
- Prefer project task runners (`just`, `npm run`, Makefile) over ad hoc commands.
- Do not assume repo language before inspecting files.

## Windows/WSL split (recommended)

```text
Windows host:
  VS Code / Cursor / Claude Desktop / Docker Desktop / local LLM apps

WSL2 Ubuntu:
  repositories, git, rg/fd/jq/yq, ast-grep, build tools, test commands, MCP servers
```

Native Windows is enough for: simple search, editing, GitHub CLI, Python with `uv`, Node with `pnpm`.

Prefer WSL2 for: Bash-heavy repos, Docker Compose, monorepos, MCP servers expecting Unix paths, CI-like test reproduction.

## Install tier model

Human provisioning follows a fixed order (see `cli-tools-overview/references/install-tiers.md`):

| Tier | Contents |
|------|----------|
| **Tier 0** | `git`, `rg`, `fd`, `jq`, `node` — router core |
| **Block A** | Canonical tools in `install-blocks.md` |
| **Block B** | Child-skill extensions (`yq`, `just`, `mise`, `direnv`, `eza`, `shellcheck`, `trivy`, `hadolint`) |
| **Deferred** | Optional per skill (`fzf`, `semgrep`, `hyperfine`, `gitleaks`, `fff-mcp`, …) |

**Install scopes:** Core only (Tier 0 + missing Block A) / Recommended (Block A + B) / Everything missing (+ deferred).

After winget/uv/npm installs, operators must **restart Cursor** and smoke-test PATH — see `agent-environment.md`.

## Agent environment

Shell CLIs are discovered via PATH in the agent shell (no separate Cursor registry). MCP tools require project or user `.cursor/mcp.json`. IDE built-ins (Grep, SemanticSearch, Glob) need no install.

## MCP tiers (mcp-code-intelligence)

```text
Minimal:  rg + fd + read_file + git diff + patch_file
Better:   rg + fd + ast-grep + git tools + task runner + optional fff MCP
Advanced: Serena or codebase-memory-mcp + Zoekt + optional embeddings
```

Embedding-first indexing is heavier — secondary to text/structural search.

## Validation

Run after any CLI skill change:

```bash
python scripts/validate_cli_skills.py
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
```

Validator checks: all 13 CLI dirs exist, required headings, platform install subsections, all 6 `cli-tools-overview/references/` files present, no repo-root `skills/` directory.

## Quality bar

Usable by another coding agent without extra explanation. Practical commands, safe workflows, clear platform distinctions.
