# GitHub Projects backlog provider

Use this optional provider when a GitHub Project board is the queue. This file is the home for
GitHub Projects-specific IDs, `gh project` commands, Stage field IDs, option IDs, and label taxonomy.
Generic workflow docs should point here instead of embedding board details.

## Setup

Fill these values after the board exists. Missing board IDs are not required for projects using the
Local Markdown or GitHub Issues providers.

Read field and option IDs with:

```bash
gh project field-list <PROJECT_NUMBER> --owner <OWNER>
```

| Thing | Value |
|-------|-------|
| Board URL | `${PROJECT_BOARD_URL}` |
| Owner | `${PROJECT_OWNER}` |
| Project number | `${PROJECT_NUMBER}` |
| Project node id | `${PROJECT_NODE_ID}` |
| Stage field id | `${STAGE_FIELD_ID}` |
| `Backlog` option | `${STAGE_BACKLOG_ID}` |
| `Ready` option | `${STAGE_READY_ID}` |
| `Claimed` option | `${STAGE_CLAIMED_ID}` |
| `In Progress` option | `${STAGE_IN_PROGRESS_ID}` |
| `Blocked` option | `${STAGE_BLOCKED_ID}` |
| `Review` option | `${STAGE_REVIEW_ID}` |
| `Done` option | `${STAGE_DONE_ID}` |

## Stage contract

1. Pick from `Stage = Ready`, sorted by `priority:p0..p3`.
2. Claim the issue by assigning yourself and moving the project item to `Stage = Claimed`.
3. Move the card to `Stage = In Progress` on the first commit.
4. If blocked, move the card to `Stage = Blocked` and comment the unblock condition.
5. Include `Closes #<N>` in the PR and move the card to `Stage = Review`; on merge, move it to
   `Stage = Done` if automation does not do so.

## Claim commands

```bash
OWNER="${PROJECT_OWNER}"
REPO="${PROJECT_REPO}"
N="<issue-number>"

gh issue view "$N" --repo "$OWNER/$REPO" --json number,state,assignees,title
gh issue edit "$N" --repo "$OWNER/$REPO" --add-assignee @me

ITEM_ID=$(gh project item-list ${PROJECT_NUMBER} --owner "$OWNER" --format json --limit 200 \
  | jq -r --argjson n "$N" --arg repo "$REPO" '
      .items[]
      | select(.content.number == $n)
      | select((.content.repository // "") | endswith($repo))
      | .id')

gh project item-edit \
  --id "$ITEM_ID" \
  --project-id ${PROJECT_NODE_ID} \
  --field-id ${STAGE_FIELD_ID} \
  --single-select-option-id ${STAGE_CLAIMED_ID}
```

## Filing a new task

```bash
gh issue list --repo "$OWNER/$REPO" --search "<keywords>"
gh issue create --repo "$OWNER/$REPO" --title "<title>" --body "<body>" --label "type:chore,priority:p2"
gh project item-add ${PROJECT_NUMBER} --owner "$OWNER" --url "<issue-url>"
```

Set `Stage = Backlog` by default; promote to `Ready` only with maintainer signoff.

## Label taxonomy

| Family | Example values |
|--------|----------------|
| `area:*` | one per major module/domain of your repo |
| `priority:*` | `p0`, `p1`, `p2`, `p3` |
| `type:*` | `bug`, `feature`, `refactor`, `test`, `chore`, `epic` |
| (special) | `cross-repo` for multi-repo programs |
| (status) | `status:obsolete` for superseded/deferred work that remains visible |
