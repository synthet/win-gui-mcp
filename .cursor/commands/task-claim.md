---
capability: "task-claim agent asset workflow"
side_effect_level: remote_write
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/task-claim`. When customizing, keep in sync with `.cursor/commands/task-claim.md`.

# /task-claim — claim a backlog item

Use when starting work on a backlog item. `$ARGUMENTS` is a provider-specific item reference.

**Usage:**
```text
/task-claim <item-ref> [--provider local-markdown|github-issues|github-projects] [--repo <owner/repo>]
```

If `--provider` is omitted, prefer the project's configured provider. For generic projects, default
to **Local Markdown** or **GitHub Issues**. Use **GitHub Projects** only when the project explicitly
adopts the board provider and has filled in the IDs in
[`.agent/backlog/providers/github-projects.md`](../../.agent/backlog/providers/github-projects.md).

## Action

Run the provider-specific claim flow and stop on any failure.

### Local Markdown (default for generic/offline projects)

1. Open the active local backlog file (usually `.agent/backlog/items.md`).
2. Find the requested item, confirm it is `Ready` and unowned.
3. Set `Owner` to yourself and `Status` to `Claimed`.
4. Report the item ID/title and remind the user to move it to `In Progress` on the first commit.

See [`.agent/backlog/providers/local-markdown.md`](../../.agent/backlog/providers/local-markdown.md).

### GitHub Issues (default for GitHub-hosted repos without a board)

```bash
OWNER_REPO="<owner/repo>"  # current repo unless --repo is provided
N="<issue-number>"

gh issue view "$N" --repo "$OWNER_REPO" --json number,state,assignees,title,labels
gh issue edit "$N" --repo "$OWNER_REPO" --add-assignee @me --add-label status:claimed
```

If the issue is closed, abort. If it is assigned to someone else, abort unless the maintainer gives
an explicit handoff. See [`.agent/backlog/providers/github-issues.md`](../../.agent/backlog/providers/github-issues.md).

### GitHub Projects (optional board provider)

Use the GitHub Projects claim commands and ID table in
[`.agent/backlog/providers/github-projects.md`](../../.agent/backlog/providers/github-projects.md). Do
not treat missing project/field/option IDs as a blocker unless this provider is explicitly selected.

## Done when

Report back with:

- Provider and item reference.
- Title/summary.
- Confirmation that the item is claimed.
- The provider-specific PR reference to use (`Refs <ID>` or `Closes #<N>`).

Full contract: [`docs/project/00-backlog-workflow.md`](../../docs/project/00-backlog-workflow.md).
