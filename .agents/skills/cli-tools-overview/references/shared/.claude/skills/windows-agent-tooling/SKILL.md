---
name: windows-agent-tooling
description: Use for native Windows agent workflows involving PowerShell, winget, Cursor IDE, Docker Desktop, path conventions, and decisions about when WSL is optional. Apply when the environment or user request is Windows-specific.
capability: "windows-agent-tooling agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Windows agent tooling

## Purpose

Effective agent work on Windows host without unnecessary WSL hops.

## When to Use

- User workspace is native Windows
- Simple search, edit, gh, uv, pnpm tasks
- Docker Desktop on Windows

## Required Tools

PowerShell, `git`, `rg`, `fd`, `gh`, optional Docker Desktop.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.


## Common Commands

```powershell
git status --short
rg "pattern" . --glob '!node_modules' -n --max-count 30
fd -e md .
Get-Content .\README.md -TotalCount 80
gh pr list --limit 10
```

## Host / WSL split

```text
Windows host:
  Cursor / VS Code / Claude Desktop / Docker Desktop / local LLM apps
  Simple rg/fd, GitHub CLI, uv/pnpm on native paths

WSL2 Ubuntu:
  repositories (optional), ast-grep, MCP stdio servers, CI-like tests
```

When native Windows is enough: simple search, editing, GitHub CLI, Python with `uv`, Node with `pnpm`.

When WSL2 is better: Bash-heavy repos, Docker Compose Linux paths, MCP Unix servers, monorepo CI parity.

Full diagram: [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Agent-Safe Patterns

- Avoid mixing `D:\` and `/mnt/d/` in one workflow.
- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Execution policy blocking scripts: user adjusts policy for their profile.
- Docker not running: start Docker Desktop before compose commands.

## Windows Notes

- Primary environment for this skill.
- Optional fff-mcp at `%LOCALAPPDATA%\fff-mcp\bin\fff-mcp.exe`.

## WSL2 Notes

- Escalate to [wsl2-agent-tooling](../wsl2-agent-tooling/SKILL.md) when tools fail on native Windows.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] Core tools available in PowerShell
- [ ] Chosen Windows vs WSL for this task
