# Install tiers

Tiered install order for **synthet-code-framework** agent CLI skills. Block A commands live in [install-blocks.md](install-blocks.md) — do not duplicate full winget blocks here.

## Tier overview

| Tier | What | When to install |
|------|------|-----------------|
| **Tier 0 (router core)** | `git`, `rg`, `fd`, `jq`, `node` | First — router and most skills assume these |
| **Block A (canonical)** | Full block in [install-blocks.md](install-blocks.md) | Core agent workflow on your platform |
| **Block B (extensions)** | Child-skill tools not in Block A | Recommended full agent workflow |
| **Deferred** | Optional per skill | Only when that skill's task needs them |

## Install scopes (operator choice)

| Scope | Includes |
|-------|----------|
| **Core only** | Tier 0 + any missing Block A tools from install-blocks |
| **Recommended** | Block A + Block B |
| **Everything missing** | Recommended + deferred tools you expect to use |

After any winget/uv/npm install, see [agent-environment.md](agent-environment.md) — restart Cursor and smoke-test PATH.

## Tier 0 — router core

Must respond to `--version` before other skills:

```powershell
git --version; rg --version; fd --version; jq --version; node --version
```

Install missing tools via Block A in [install-blocks.md](install-blocks.md).

## Block A — canonical (install-blocks.md)

Tools in the Windows winget / WSL apt / Homebrew blocks:

- `git`, `gh`, `rg`, `fd`, `jq`, `delta`, `bat`, `zoxide`, `node` (+ npm/corepack)
- `ast-grep` (`sg` via npm global)
- `uv`, `ruff`, `pyright`

**Note:** Prefer standalone winget `rg` on Windows even if Cursor bundles its own ripgrep for IDE search.

## Block B — child-skill extensions

Not listed in Block A install-blocks; install when pursuing **Recommended** scope.

| Tool | Child skill | Windows (winget) |
|------|-------------|------------------|
| `yq` | [data-config-tools](shared/.claude/skills/data-config-tools/SKILL.md) | `MikeFarah.yq` |
| `just` | [task-env-package-tools](shared/.claude/skills/task-env-package-tools/SKILL.md) | `casey.just` |
| `mise` | task-env-package-tools | `jdx.mise` |
| `direnv` | task-env-package-tools | `direnv.direnv` |
| `eza` | [search-and-navigation](shared/.claude/skills/search-and-navigation/SKILL.md) | `eza-community.eza` |
| `shellcheck` | [lint-format-security](shared/.claude/skills/lint-format-security/SKILL.md) | `koalaman.shellcheck` |
| `trivy` | lint-format-security | `AquaSecurity.Trivy` |
| `hadolint` | lint-format-security | `hadolint.hadolint` |

WSL/macOS: many Block B tools are in the Homebrew one-liner in install-blocks; on WSL apt, install individually or via brew/linuxbrew when needed.

### Block B — one-shot Windows example

```powershell
winget install MikeFarah.yq casey.just jdx.mise direnv.direnv eza-community.eza koalaman.shellcheck AquaSecurity.Trivy hadolint.hadolint
```

Confirm IDs with `winget search` on locked-down machines.

## Deferred — optional

Install only when the linked skill's task requires it:

| Tool | Skill | When |
|------|-------|------|
| `fzf` | [search-and-navigation](shared/.claude/skills/search-and-navigation/SKILL.md) | Human interactive pick from long lists |
| `semgrep` | [structural-code-search](shared/.claude/skills/structural-code-search/SKILL.md) | Rule/security scans (`--dryrun` default) |
| `hyperfine` | task-env-package-tools | Benchmarking commands |
| `gitleaks` | [git-and-diff-workflows](shared/.claude/skills/git-and-diff-workflows/SKILL.md) | Secret scan before sharing diffs |
| `ctags` / `tree-sitter` | structural-code-search | Repeated def/ref across sessions |
| `fff-mcp` | [mcp-code-intelligence](shared/.claude/skills/mcp-code-intelligence/SKILL.md), [search-tool-selection](shared/.claude/skills/search-tool-selection/SKILL.md) | Opt-in project MCP — see [AGENTS.md](shared/AGENTS.md) |
| `graphify` (`graphifyy`) | [mcp-code-intelligence](shared/.claude/skills/mcp-code-intelligence/SKILL.md) | Opt-in knowledge graph CLI/MCP — `uv tool install graphifyy` (MCP: `graphifyy[mcp]`); see [AGENTS.md](shared/AGENTS.md) |

## Framework verification (after install)

```powershell
# Tier 0 + Block A sample
fd --version; bat --version; sg --version; ruff --version

# Block B sample
yq --version; just --version
```

Repo skill validators:

```bash
python scripts/validate_cli_skills.py
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
```

Full environment checklist: [agent-environment.md](agent-environment.md).
