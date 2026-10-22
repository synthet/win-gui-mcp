---
name: install-checklist
description: Use when provisioning a human workstation with agent CLI tools via winget, apt, or Homebrew. Apply when the user asks what to install on Windows, WSL2 Ubuntu, or macOS; do not use for automated dependency install loops inside a task.
capability: "install-checklist agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Install checklist

## Purpose

One-time setup of CLI tools on a developer machine.

## When to Use

- New laptop or VM setup
- Missing `rg`/`fd`/ `gh` errors in agent sessions

**Agents:** recommend this skill to the user; do not run bulk `winget install` without approval.

## Required Tools

Package managers: `winget`, `apt`, or `brew`.

## Install

Choose an install scope first — see [install-tiers.md](../cli-tools-overview/references/install-tiers.md) (Core only / Recommended / Everything missing).

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.


## Common Commands

Copy full blocks from [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

## Agent-Safe Patterns

- User executes install blocks interactively.
- After install, **restart Cursor** and run the smoke test in [agent-environment.md](../cli-tools-overview/references/agent-environment.md).
- Verify with `--version` commands only.

## Commands Requiring Confirmation

All install/uninstall commands require user initiation. See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- `fd` not found on Ubuntu: use `fdfind` or symlink per install reference.
- PATH not updated: restart shell after uv/node installs.

## Windows Notes

- winget may need elevation for some packages.
- fff-mcp: optional; install from [fff](https://github.com/dmtrKovalenko/fff) releases.
- graphify: optional; `uv tool install graphifyy` (MCP: `uv tool install "graphifyy[mcp]"`) — see [Graphify](https://github.com/Graphify-Labs/graphify).

## WSL2 Notes

- Install tools inside WSL, not only on Windows host.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `git --version`, `rg --version`, `fd --version`, `jq --version`, `gh --version`
- [ ] `node --version`, `pnpm --version` if JS repo
- [ ] `uv --version` if Python repo
- [ ] Block B (Recommended scope): `yq --version`, `just --version`, `shellcheck --version`
- [ ] Cursor restarted; smoke test passes per [agent-environment.md](../cli-tools-overview/references/agent-environment.md)
