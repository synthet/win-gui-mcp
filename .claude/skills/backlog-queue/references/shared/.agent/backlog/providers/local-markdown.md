# Local Markdown backlog provider

Use this provider for generic projects, prototypes, local-only work, or repos without a hosted issue
tracker.

## Suggested files

- `.agent/backlog/items.md` — append-only table of backlog items.
- `.agent/backlog/archive.md` — completed or obsolete items, if the active list grows too large.

## Suggested item format

```markdown
| ID | Status | Priority | Owner | Title | Notes |
|----|--------|----------|-------|-------|-------|
| L-001 | Ready | p1 | - | Add smoke test | Acceptance: ... |
```

## Statuses

Use `Backlog`, `Ready`, `Claimed`, `In Progress`, `Blocked`, `Review`, and `Done` unless the project
defines a smaller vocabulary.

## Claim flow

1. Find the highest-priority `Ready` item.
2. Set `Owner` to yourself and `Status` to `Claimed`.
3. Set `Status` to `In Progress` on the first commit.
4. If blocked, set `Status` to `Blocked` and add the unblock condition in `Notes`.
5. Reference the local ID in the PR body, for example `Refs L-001`.
