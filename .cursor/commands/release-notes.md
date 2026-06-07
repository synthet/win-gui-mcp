# /release-notes — User-facing changelog slice

Use before tagging a release or publishing notes.

## Inputs

- Commit range, merged PRs, or closed issues since last release.
- Product name and version (or "next").

## Output

1. **Highlights** — 3–7 bullets for end users (behavior, not implementation).
2. **Breaking changes** — Clear migration notes (especially MCP tool schema changes).
3. **Deprecations** — What to stop using and by when.
4. **Contributors** — Optional thank-you / attribution if the project does that.

## Tone

- Present tense or "Now …"; avoid internal jargon.

## Done when

- A release manager could paste this into GitHub Releases or CHANGELOG.md with minimal edits.

## Related

- Skill: `commit-conventions`
- Docs: [docs/phases.md](../../docs/phases.md) for phase milestones
