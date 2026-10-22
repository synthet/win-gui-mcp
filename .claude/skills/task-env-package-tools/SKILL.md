---
name: task-env-package-tools
description: Use for synthet-code-framework task runners, uv, Docker, sync checks, frontmatter validation, OKF lint, pytest, and project verification gates. Apply when running repo-specific tests/checks or diagnosing environment/package tooling.
capability: "Emit and optionally run framework verify gates via compiled harness"
side_effect_level: local_write
approval_required: false
requires_tools: "python scripts/harness.py; python; optional docker, uv, just, mise"
output_schema: "Gate catalog / selected commands / optional run results (JSON)"
risk_class: medium
---

# Task, environment, and package tools (compiled harness)

## Purpose

Run builds/tests/lint via project conventions. The framework verify catalog is
compiled into `scripts/harness.py` — do not re-derive gate lists from prose.

## When to Use

- Running framework or bootstrapped project checks
- After editing `.claude/` assets (sync + frontmatter + cli_skills)
- Choosing project scripts over ad hoc commands

## Required Tools

Harness: `python scripts/harness.py`.
Optional: `uv`, `docker`, `just`, `mise`, `pnpm`.

## Install

Install blocks are shared — See [install-blocks.md](references/shared/.claude/skills/cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.

## Common Commands

**Compiled gates (preferred):**

```bash
python scripts/harness.py --list --json
python scripts/harness.py --profile agent-assets --json
python scripts/harness.py --profile agent-assets --run --json
python scripts/harness.py --gates pytest,okf_lint --run --json
```

Profiles: `framework`, `agent-assets` (sync + frontmatter + cli_skills), `tests`, `lint`.

Bootstrapped projects: use `<documented-build-command>` / `<documented-test-command>` / `<documented-lint-command>` from AGENTS.md.

## LLM judgment slots

- Choose `--profile` / `--gates` for the change type.
- Interpret failures; narrow pytest to the failing file first.

## Agent-Safe Patterns

- Read AGENTS.md / Makefile / package.json scripts before inventing commands.
- Default is dry-run; pass `--run` only for selected gates.
- See [bounded-output-patterns.md](references/shared/.claude/skills/cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](references/shared/.claude/skills/cli-tools-overview/references/commands-requiring-confirmation.md).
`docker system prune`, broad `pip install`, production deploy scripts need confirmation.

## Troubleshooting

- Sync drift: harness `--profile agent-assets --run`, or `python scripts/sync_assistant_trees.py`.
- pytest failures: narrow to failing test file first.

## Windows Notes

- Run Python from project venv when documented.
- See [windows-wsl-split.md](references/shared/.claude/skills/cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Prefer WSL for Linux CI parity.
- See [windows-wsl-split.md](references/shared/.claude/skills/cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] Harness profile/gates match the change
- [ ] `<documented-test-command>` or pytest green for touched area
- [ ] sync + frontmatter checks when agent assets changed

## Reusing this workflow outside the framework

Choose gates from the current repository's contract and declared task runner. Framework-only
profiles are examples for this scaffold, not universal acceptance checks. Use the existing
uv, Poetry, Pipenv, Conda or virtualenv environment; do not introduce another manager or install
dependencies globally. Preserve the declared dependency/lock workflow and do not replace it
with an unsolicited `pip freeze`. Read current CLI help when a helper or gate is unavailable.

Resolve helper paths from this skill directory. Run project commands from the target workspace.
