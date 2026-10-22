---
name: git-and-diff-workflows
description: Use for safe Git and GitHub workflows: status, diffs, logs, branches, staging, commits, PR/issue inspection, and secret checks. Always apply before committing or when the user asks about git state, PRs, issues, or reviewing repository changes.
capability: "git-and-diff-workflows agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Git and diff workflows

## Purpose

Repository operations with bounded diffs and explicit confirmation for history rewrites.

## When to Use

- Before/after edits (`status`, `diff`)
- PR/issue work (`gh`)
- Secret scanning (`gitleaks` read-only)

## Required Tools

`git`, `gh`; optional `delta`, `gitleaks`, `git-filter-repo` (human-only).

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
git log --oneline -n 20
gh pr view 123 --json title,state,url
gh issue list --limit 20
gitleaks detect --source . --no-git -v 2>/dev/null | head -50
```

## Agent-Safe Patterns

- Always `git status --short` before staging.
- Show `git diff --stat` before commit messages.
- See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md). — especially reset, clean, force-push, filter-repo.

## Troubleshooting

- Detached HEAD: ask user before checkout.
- `gh` auth: user runs `gh auth login`.

## Windows Notes

- Line endings: watch for CRLF noise in diffs.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Commit from same environment as tests ran.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `git status` clean or changes understood
- [ ] Diff reviewed before commit

## Integrating a pull-request stack or upstream fork

Verify the repository, default branch, clean/understood working state and task authority.
Determine dependency order from actual diffs; identify already-landed changes before merging.
Preserve unrelated work without automatically stashing or committing it. Never weaken required
checks, use admin bypass, or make failed deployment checks optional merely to unblock a merge.

For an upstream fork, record the fork's behavioral boundaries before integration. Resolve
signature/rename/module-move conflicts using upstream's new structure while preserving intentional
fork behavior. Inspect clean auto-merges for duplicate methods, removed types, stale variants and
silently restored upstream features. Verify those boundaries and the affected tests after merging.
Fetch a verified upstream URL when needed; never change `.git/config` to add remotes.
