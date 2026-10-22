---
description: Update CHANGELOG.md with recent changes, commit, and push
---

# Update changelog

## Steps

1. **Check status & recent history** — review `git status` and the last ~10 commits to understand
   what changed.

2. **Review unstaged changes** — read the full diff. Open `CHANGELOG.md` to find the current version.

3. **Update `CHANGELOG.md`**:
   - Bump the version (major/minor/patch as appropriate; follow Conventional Commits / SemVer).
   - Use today's date.
   - Categorize under **Added / Changed / Fixed / Removed**.
   - Bold feature names; reference files/modules where helpful.

4. **Stage, commit, push** — only when the user has asked you to commit/push:
   ```sh
   git add -A
   git commit -m "<conventional commit summary>"
   git push
   ```

5. **Verify** — confirm the commit landed (`git log -1`) and the tree is clean (`git status`).

## Do not

- Do not commit or push unless explicitly asked.
- Do not bypass hooks (`--no-verify`) or signing unless the user requested it.
