---
name: wsl2-agent-tooling
description: Use for WSL2 Ubuntu agent workflows: repos under Linux filesystem, apt tooling, builds/tests, ast-grep, MCP servers, Docker/Windows interop, and CI-like commands. Apply when the environment or user request is WSL2-specific.
capability: "wsl2-agent-tooling agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# WSL2 agent tooling

## Purpose

Run agent shell work in WSL2 when Linux tooling, paths, or MCP servers require it.

## When to Use

- Bash-heavy repos and shell scripts
- Docker Compose with Linux paths
- ast-grep, semgrep, MCP stdio servers
- Reproducing GitHub Actions locally

## Required Tools

WSL2 Ubuntu, `git`, `rg`, `fd`, build chain for the project.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.


## Common Commands

```bash
cd ~/projects/my-repo
git status --short
rg "pattern" . --glob '!node_modules' -n --max-count 30
sg run --pattern 'import $M from $P' --lang ts src/ | head -40
docker compose up -d
python -m pytest tests/test_foo.py -q
```

## Host / WSL split

```text
Windows host:
  Cursor / VS Code / Claude Desktop / Docker Desktop

WSL2 Ubuntu:
  git checkout, rg/fd/jq/yq, ast-grep, build/test, MCP servers
```

Full diagram: [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Agent-Safe Patterns

- Open repo from WSL path (`~/...`), not `/mnt/c/...` when performance matters.
- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- `fd` command: use `fdfind` or symlink from install reference.
- Docker in WSL: enable WSL integration in Docker Desktop.

## Windows Notes

- IDE may edit via `\\wsl$\` — run tests in WSL terminal.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Primary environment for this skill.

## Verification Checklist

- [ ] `uname -a` shows WSL
- [ ] Project tests run in WSL checkout
