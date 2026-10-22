---
name: lint-format-security
description: Use when running or choosing bounded linters, formatters, type checks, or security scanners such as ruff, prettier, eslint, clippy, rustfmt, shellcheck, trivy, hadolint, or gitleaks. Apply before merge when the user asks for lint, format, static analysis, or security checks.
capability: "Route lint/format/security stacks to check-mode commands via compiled harness"
side_effect_level: local_write
approval_required: false
requires_tools: "python .claude/skills/lint-format-security/scripts/harness.py; optional ruff, eslint, prettier, cargo, clippy, rustfmt, shellcheck, trivy, hadolint, gitleaks"
output_schema: "Stack recommendation: check_commands, avoid, confirmation_gates"
risk_class: medium
---

# Lint, format, and security (compiled harness)

## Purpose

Static analysis and container/config scanning without silent auto-fix at scale.
The stack → command table is compiled into `scripts/harness.py`.

## When to Use

- Python: `ruff`, `pyright`
- JS/TS: `eslint`, `prettier`
- Rust: `cargo fmt`, `cargo clippy`
- Shell: `shellcheck`
- Containers/IaC: `hadolint`, `trivy`
- Secrets: `gitleaks`

## Required Tools

Harness: `python .claude/skills/lint-format-security/scripts/harness.py`.
Optional: `ruff`, `eslint`, `prettier`, `cargo`/`clippy`/`rustfmt`, `shellcheck`, `trivy`, `hadolint`, `gitleaks`.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.

## Common Commands

**Compiled router (preferred):**

```bash
python .claude/skills/lint-format-security/scripts/harness.py --list --json
python .claude/skills/lint-format-security/scripts/harness.py --stack python --paths scripts/skill_harness --json
python .claude/skills/lint-format-security/scripts/harness.py --stack js_ts --paths src --json
python .claude/skills/lint-format-security/scripts/harness.py --stack rust --paths crates/foo --json
```

Stacks: `python`, `js_ts`, `rust`, `shell`, `container`, `secrets`.

## LLM judgment slots

- Map changed files / user ask to `--stack`.
- Decide whether the user approved auto-fix before any write mode.

## Agent-Safe Patterns

- Check mode before write/fix mode.
- Scope to changed paths.
- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

`--fix`, `--write`, `--auto-fix` on broad trees. See
[commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Missing plugins: use project devDependencies / uv tools.
- trivy DB download: may need network once.
- Unknown stack: run harness `--list`.

## Windows Notes

- Prefer project-local `npx eslint` over global installs.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Match CI linter versions when possible.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] Ran harness with a concrete `--stack`
- [ ] Linters run on narrowest scope
- [ ] User approved any auto-fix
