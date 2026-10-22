# Project guide — how agents navigate ${PROJECT_NAME}

A fast orientation for AI agents. Read this, then the canonical sources it points to.

## 1. Before picking work

- The queue is the configured **backlog provider**, not ad hoc `TODO.md` notes. Follow the five-step contract in
  [`../docs/project/00-backlog-workflow.md`](../docs/project/00-backlog-workflow.md) and provider details in
  [`backlog/README.md`](backlog/README.md). Claim with `/task-claim <item-ref>`.

## 2. Authority (don't invent contracts)

- [`../docs/CANONICAL_SOURCES.md`](../docs/CANONICAL_SOURCES.md) — where each API/schema/config/vocabulary is defined.
- [`SAFETY.md`](SAFETY.md) — secrets, git, MCP, external-review safety.

## 3. Core workflows

| Goal | Use |
|------|-----|
| Spec a change | `/spec` → [`workflows/spec.md`](workflows/spec.md) |
| Plan it | `/plan` → [`workflows/plan.md`](workflows/plan.md) |
| Implement | `/implement` → [`workflows/implement.md`](workflows/implement.md) |
| Fix failing tests | `/test-and-fix` → [`workflows/test-and-fix.md`](workflows/test-and-fix.md) |
| Prepare a PR | `/pr-ready` → [`workflows/pr-ready.md`](workflows/pr-ready.md) |
| Critical bug hunt | `/critical-commit-audit` |
| External review | `/check-subagents`, `/run-codex-review`, `/run-gemini-review` |
| Docs | `/wiki-ingest`, `/wiki-lint`, `/wiki-query` |
| Memory | `/log-session` → `/dream-memory` → `/promote-memory` → `/memory-context` |

## 4. Environment & commands

- Build/test/lint commands and the test vocabulary live in [`../AGENTS.md`](../AGENTS.md).
- Verified one-liners: [`COMMANDS.md`](COMMANDS.md).

## 5. Asset map

- Full inventory: [`AGENT_INFRA_INVENTORY.md`](AGENT_INFRA_INVENTORY.md).
- Where every asset lives + the SDLC loop: [`../docs/ai-workflow/README.md`](../docs/ai-workflow/README.md).
