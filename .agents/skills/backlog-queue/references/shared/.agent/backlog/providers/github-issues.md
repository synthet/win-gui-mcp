# GitHub Issues backlog provider

Use this provider when GitHub Issues is the queue without a GitHub Project board.

## Suggested labels

| Family | Example values |
|--------|----------------|
| `area:*` | one per major module/domain of your repo |
| `priority:*` | `p0`, `p1`, `p2`, `p3` |
| `type:*` | `bug`, `feature`, `refactor`, `test`, `chore`, `epic` |
| `status:*` | `ready`, `claimed`, `in-progress`, `blocked`, `review`, `obsolete` |

## Claim flow

```bash
OWNER="<owner>"
REPO="<repo>"
N="<issue-number>"

gh issue view "$N" --repo "$OWNER/$REPO" --json number,state,assignees,title,labels
gh issue edit "$N" --repo "$OWNER/$REPO" --add-assignee @me --add-label status:claimed
```

If the issue is closed, abort. If it is assigned to someone else, abort unless the maintainer gives
an explicit handoff.

On first commit, replace `status:claimed` with `status:in-progress`. If blocked, add
`status:blocked` and comment with the reason and unblock condition. PR descriptions should contain
`Closes #<N>` when the PR fully resolves the issue, or `Refs #<N>` for partial work.
