---
name: structural-code-search
description: Use when syntax-aware search is needed with ast-grep, semgrep, or ctags, especially after rg has too many false positives. Apply for refactors, API usage patterns, AST shapes, policy rules, or symbol indexes.
capability: "structural-code-search agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Structural code search

> **Tool choice first:** [`search-tool-selection`](../search-tool-selection/SKILL.md).

## Purpose

Match code **shapes** (AST patterns), run security rule packs, or use symbol indexes.

## When to Use

- Refactor targets (function signatures, imports, class shapes)
- Policy/security scans (`semgrep`)
- Repeated def/ref jumps (`ctags`)

## Required Tools

`ast-grep` (`sg`), optional `semgrep`, `universal-ctags`.

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
sg run --pattern 'function $NAME($$$) { $$$ }' --lang javascript src/
semgrep scan --config auto --max-target-bytes 1000000
ctags -R --fields=+n src/
rg "SymbolName" --type-add 'lang:*.xyz' -t lang  # fallback text search
```

Preview ast-grep rewrites with diff before applying (`sg run -r`).

## Agent-Safe Patterns

- Try `rg` first for simple literals; escalate here when noisy.
- `semgrep --dryrun` unless user requests fixes.
- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

Mechanical rewrites and `semgrep --fix` require user approval. See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Language not supported: fall back to `rg` with file-type globs.
- semgrep timeout: narrow paths or configs.

## Windows Notes

- `@ast-grep/cli` via npm global is reliable on Windows.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Prefer WSL for `semgrep` rule caches and large trees.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `sg --version`
- [ ] Test pattern on small subdirectory first
