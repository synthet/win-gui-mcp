---
name: framework-pr-ready
description: "Explicit framework workflow: pr-ready"
disable-model-invocation: true
---

# /pr-ready — Prepare for pull request

Use when implementation is complete and you want a merge-ready PR.

## Inputs

- Diff or branch state; **AGENTS.md**; optional issue link.

## Output

1. **Summary** — User-facing description of the change (not the commit list).
2. **Risk / rollout** — Breaking changes, migrations, config.
3. **Testing** — `dotnet build` (and `dotnet test` if applicable) commands run and results.
4. **Suggested commit message** — Prefer [Conventional Commits](https://www.conventionalcommits.org/); use skill `commit-conventions`.
5. **PR description** — Paste-ready Markdown.

## Self-review

- Scan diff for **debug code**, **TODOs** that should be issues, and **accidental files** (`bin/`, `obj/`).
- Confirm no secrets or large binaries.
- MCP tool changes align with [docs/tools.md](../../docs/tools.md).

## Done when

- Maintainer can open a PR without rewriting the description.

## Related

- Skill: `commit-conventions`
- Subagent: `pr-ready-hygiene` (`.cursor/agents/pr-ready-hygiene.md`)
