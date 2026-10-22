---
name: backlog-queue
description: Use when picking, claiming, triaging, updating, closing, or filing backlog work across Local Markdown, GitHub Issues, or GitHub Projects. Always use when the user mentions backlog, queue, claim, status transition, issue intake, project board, or provider-oriented task flow.
capability: "backlog-queue agent asset workflow"
side_effect_level: remote_write
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Backlog queue

The canonical task queue is the project's configured **backlog provider**, not ad hoc `TODO.md`
notes. Provider docs live in [`.agent/backlog/`](references/shared/.agent/backlog/README.md). Resolve the
provider in this order:

1. Use the provider explicitly named by the user or project docs.
2. Otherwise use GitHub Issues for GitHub-hosted repos with issue tracking enabled.
3. Otherwise use Local Markdown for generic/offline projects.
4. Use GitHub Projects only when the project explicitly adopts that optional board provider.

## When to use

- The user asks to pick the next task, start work, or "what's next".
- The user asks to file a new backlog item.
- A PR is being prepared and needs an item reference.
- A task has hit a blocker, is ready for review, or is finished.
- The user mentions a provider status such as Ready, Claimed, In Progress, Blocked, Review, or Done.
- An agent picks up a task without a corresponding backlog item — stop and file one first.

Treat Ready/Claimed/In Progress/Blocked/Review/Done as logical states. Map them to the selected
provider's fields, labels, table columns, or workflow statuses instead of assuming a GitHub Projects
`Stage` field exists.

## Generic five-step contract

Every contributor (human or AI) follows the same five steps. Do **all** of them; skipping a step
puts the queue out of sync.

### 1. Pick from the ready queue

Open the configured provider and pick the highest-priority unassigned ready item. If the ready queue
is empty, stop and ask the maintainer to promote or create work. **Do not invent new work.**

### 2. Claim the item

Use `/task-claim <item-ref>` (preferred) or the provider-specific claim flow. Claiming must record an
owner/assignee before work begins. Do not require GitHub Projects IDs unless GitHub Projects is the
selected provider.

### 3. Mark in progress on first commit

Move the item to the provider's in-progress state on the first commit.

### 4. If blocked, mark + comment

Move the item to the provider's blocked state and record the reason plus the unblock condition.

### 5. PR references the item

Your PR description must reference the backlog item. Use `Closes #<N>` for GitHub Issues when the PR
fully resolves the issue; use `Refs <ID>` for local or partial work. Move the item to the provider's
review/done state according to that provider's rules.

## Provider references

- Local Markdown: [`.agent/backlog/providers/local-markdown.md`](references/shared/.agent/backlog/providers/local-markdown.md)
- GitHub Issues: [`.agent/backlog/providers/github-issues.md`](references/shared/.agent/backlog/providers/github-issues.md)
- GitHub Projects: [`.agent/backlog/providers/github-projects.md`](references/shared/.agent/backlog/providers/github-projects.md)

## Don'ts

- **Don't** add tasks to random `TODO.md` files.
- **Don't** start work without claiming a provider-backed item.
- **Don't** silently abandon a claimed or in-progress item — mark it blocked with the unblock condition.
- **Don't** open a PR without a backlog reference.
- **Don't** embed provider-specific IDs or board commands in generic workflow docs; put them under `.agent/backlog/providers/`.
- **Don't** treat missing optional-provider IDs (for example GitHub Projects field IDs) as setup blockers for Local Markdown or GitHub Issues.

## Mirrors

This skill is mirrored to `.cursor/skills/backlog-queue/SKILL.md` and
`.agents/skills/backlog-queue/SKILL.md`. `.claude/` is canonical — regenerate both mirrors via
`python scripts/sync_assistant_trees.py`.

Resolve helper paths from this skill directory. Run project commands from the target workspace.
