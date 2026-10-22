---
name: safe-command-patterns
description: Use before running shell commands that explore, edit, delete, move, generate, or inspect many files. Apply when constructing safe bounded commands, avoiding destructive operations, or replacing risky patterns with agent-safe alternatives.
capability: "safe-command-patterns agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Safe command patterns

## Purpose

Default safety rails for agent shell usage: bounded output, inspect-before-edit, confirmation gates.

## When to Use

Throughout every coding task — before search, edit, test, or commit.

## Required Tools

`git`, `rg` or IDE search; optional `bat`, `delta`.

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
git status --short
git diff --stat
git diff -- path/to/file
rg "pattern" path/ --max-count 30
```

## Agent-Safe Patterns

- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).
- When **fff MCP** is connected, prefer `ffgrep`/`fffind` for repeated repo search over many shell roundtrips; one-off probes may still use `rg`/`fd`.
- Never paste secrets into commands or logs.
- Prefer project scripts from AGENTS.md over invented commands.

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Huge command output: add `--max-count`, path prefix, or `-g` globs.
- Wrong directory: `git rev-parse --show-toplevel`.

## Windows Notes

- Use `Get-Content -TotalCount N` instead of dumping full files.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Run repo commands from the WSL checkout, not `/mnt/c/...` unless intentional.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `git status --short` before edits
- [ ] `git diff --stat` before commit
- [ ] No destructive command without user OK
