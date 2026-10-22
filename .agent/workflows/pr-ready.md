# /pr-ready — Prepare for pull request

Use when implementation is complete and you want a merge-ready PR.

This is the **definition-of-done** gate (merge readiness). Spec satisfaction is verified
separately by the `validate-implementation` skill — do not conflate the two.

## Inputs

- Diff or branch state; [AGENTS.md](../../AGENTS.md).
- Validation report from `validate-implementation` when the work has a spec with `AC-n` criteria.
- Optional sanitized JSONL trace artifact; see [Agent observability](../../docs/agent-observability.md).

## Output (PR Ready Report)

1. **Summary** — User-facing description of the change (not the commit list).
2. **Risk / rollout** — Breaking changes, migrations, config.
3. **Testing** — Commands run and results.
4. **Trace evidence (optional)** — Link to any sanitized JSONL trace artifact that supports the validation story.
5. **Suggested commit message** — Prefer Conventional Commits.
6. **PR description** — Paste-ready Markdown.

## Definition of done

- Lint/test commands ran and are green (actual results, not "probably green").
- Spec ACs Verified per `validate-implementation`, or open Unknowns/Failures listed explicitly.
- Optional trace artifacts are sanitized, referenced by path, and summarized rather than pasted wholesale.
- PR references its backlog item using the selected provider's convention (`Refs <ID>` or `Closes #<N>`); if the provider tracks status, the item is marked review-ready; providers without status fields only need the PR reference.
- No GitHub Projects board/card status is required unless GitHub Projects is the selected provider.
- Diff clean: no debug code, secrets, large binaries, or unrelated refactors.

## Done when

- Maintainer can open a PR without rewriting the description.
