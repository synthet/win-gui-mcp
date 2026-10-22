# Memory candidate categories

| Category | Use for |
|----------|---------|
| `stable_fact` | Stack, architecture, env |
| `user_preference` | Style, workflow, review prefs |
| `working_rule` | How to run tests, what not to touch |
| `recurring_issue` | Flakes, traps, env pain |
| `successful_pattern` | Approaches that worked |
| `open_question` | Unverified assumptions |
| `deprecated` | Superseded guidance |

Confidence: `low`, `medium`, `high`.

## Dream review checklist

1. Open `dreams/*-changelog.md` — scan **Uncertain / needs review**.
2. Check **Stale / needs re-verification** against the current codebase.
3. Diff proposed `dreams/*.md` vs `memory.md`.
4. Redact anything sensitive; reject promotion if secrets might be present.
5. Promote only when the proposal is accurate and concise.

## Relationship to Claude Code native memory

- **`.agent-memory/memory.md`** — team-shared, committed, repo-scoped, review-gated.
- **Claude Code native `~/.claude/.../memory/MEMORY.md`** — private, auto-loaded, personal recall.

When they conflict, committed `.agent-memory/memory.md` and live repo evidence win.
