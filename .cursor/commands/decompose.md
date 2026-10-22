---
capability: "decompose agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/decompose`. When customizing, keep in sync with `.cursor/commands/decompose.md`.

# /decompose — Break a large task into parallelizable subtasks

Use when a task or epic is too large for one `/plan`, or when multiple independent work streams
exist. The goal is a pick-list where each subtask can be handed to a separate `/plan` session —
or run in parallel branches by separate agents.

## Inputs

- Epic description or linked issue number.
- Current architecture context (see **AGENTS.md**); note any known constraints or dependencies.

## Before splitting

Some work is worse when divided: architecture design, tightly coupled refactors, and changes whose
correctness depends on holding the whole picture at once. If the epic is one of those, say so and
recommend a single `/plan` instead of producing a subtask list.

## Output

1. **Subtask list** — For each subtask:
   - **Title** — Short action phrase ("Add X", "Migrate Y", "Expose Z endpoint")
   - **Done means** — One-line completion criterion
   - **Size** — S (< 2h), M (half-day), L (full day)
   - **Depends on** — Other subtask titles that must complete first (empty = independent)

2. **Dependency graph** — Visual or bulleted map of which subtasks block which.
   Call out explicitly which subtasks are **independent** (can run in parallel).

3. **Parallel execution note** — For each independent cluster:
   > "Subtasks A, B, C are independent. Run as separate branches simultaneously using
   > git worktrees or separate Claude Code sessions."

4. **Test boundaries** — For each subtask, the minimal test or assertion that confirms
   it is done without depending on other subtasks being complete.

5. **Reducer** — How the finished subtasks come back together: integration order, who merges,
   the check that proves the reassembled whole works, and what to do when two subtasks conflict.
   Write this *before* work fans out — otherwise you get N finished branches and no integration.

## Done when

- Each subtask can be passed to `/plan` without the planner needing to know about any other subtask.
- The dependency graph has no hidden chains (all blockers are explicit).
- Every subtask has a test boundary that validates it independently.
- The reducer is written down and names an integration check.

## Note

File a backlog issue for each subtask via the `backlog-queue` skill before starting work.
Use the subtask title as the issue title and "Done means" as the acceptance criterion.
