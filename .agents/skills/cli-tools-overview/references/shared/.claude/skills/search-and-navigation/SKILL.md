---
name: search-and-navigation
description: Use for fast repo navigation and discovery with rg, fd/find, bat/sed, tree, eza, zoxide, or delta. Apply whenever locating files, finding text, mapping repo layout, or bounding command output for code exploration.
capability: "search-and-navigation agent asset workflow"
side_effect_level: read_only
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: low
---

# Search and navigation

> **Tool choice first:** [`search-tool-selection`](../search-tool-selection/SKILL.md) — fd → rg → read slice.

## Purpose

Find files and text quickly with gitignore-aware tools and bounded reads.

## When to Use

- Locate files by name (`fd`)
- Search literals/regex in contents (`rg`)
- Orient in a new directory (`tree`, `eza`)
- Read a code slice (`bat`, `sed -n`)

## Required Tools

`rg`, `fd`, `bat`; optional `tree`, `eza`, `zoxide`, `delta`, `fzf` (humans only).

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
fd -e py -e md .
rg "pattern" src/ --glob '!node_modules' -n --max-count 40
tree -L 3 -I 'node_modules|dist|build|.git'
bat --line-range 1:120 path/to/file.py
git diff -- path/to/file | delta
```

## Agent-Safe Patterns

- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).
- Do not use `fzf` in agent loops (interactive TTY).
- Prefer IDE Grep/SemanticSearch/Glob when already bound in Cursor.

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- `rg` slow: narrow path, add `-g`, increase excludes for `node_modules`, `dist`, `build`.
- `fd` empty: check cwd is repo root.

## Windows Notes

- `fd` from winget; on Ubuntu WSL use `fdfind` alias.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Default location for heavy search in monorepos.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `rg --version` and `fd --version`
- [ ] Sample search returns bounded lines
