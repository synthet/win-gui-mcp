# /tasks — Generate implementation tasks from a plan

Use after `/spec` and `/plan` are approved, before `/implement`. This Spec Kit-inspired gate turns a product spec and technical plan into a traceable, ordered task list that another developer or coding agent can execute without reinterpreting requirements.

## Inputs

- Approved spec with stable `AC-n` acceptance criteria.
- Approved implementation plan.
- Relevant backlog item, issue, or project-tracking reference, when available.

## Steps

1. Build a **traceability matrix** that maps every `AC-n` to one or more task IDs and planned verification commands.
2. Draft a numbered **task table** (`T-n`) with owner role or agent role, files / areas to touch, dependency IDs, covered acceptance criteria, and verification command or evidence.
3. Group tasks into setup, test-first scaffolding, implementation, integration, validation, and documentation.
4. Identify tasks that can run concurrently and tasks that must stay serialized.
5. List the definition-of-done checks required before `/pr-ready`.

## Done when

- Another developer can execute the task list in order without guessing.
- No acceptance criterion is orphaned.
- Verification commands or evidence are listed for every acceptance criterion.
- Out-of-scope follow-ups are separated from implementation tasks.
