# Commands requiring confirmation

Agents must **ask the user** before running any of these (or equivalent):

## Git history / destructive

- `git reset --hard`, `git clean -fd`, `git push --force`
- `git rebase` / `git cherry-pick` on shared branches
- `git filter-repo`, `git filter-branch`
- `git stash drop`, `git branch -D` on non-local experimental branches

## Filesystem

- `rm -rf`, `Remove-Item -Recurse -Force` outside `.agent/scratch/` or explicit temp dirs
- Truncating or overwriting config without showing diff first

## Package / environment

- `npm install` / `pip install` without user request (prefer read-only inspection first)
- `docker system prune`, volume deletes, database drops
- `DROP TABLE`, `DELETE` without WHERE, migration rollback in production-like DBs

## Security / secrets

- Reading or printing `.env`, `secrets.json`, credential files
- `gitleaks detect` is OK read-only; fixing leaks needs user review

## Auto-fix linters

- `eslint --fix`, `ruff check --fix`, `prettier --write` on broad paths — show scope and get approval unless user asked to fix lint

Always show `git diff --stat` after any write the user approved.
