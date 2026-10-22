---
trigger: glob
description: "Spec-first planning, acceptance criteria, and design notes"
globs: "**/docs/**"
---

# Spec and planning

## Spec-first

- Capture **problem**, **users / consumers**, **non-goals**, and **acceptance criteria** before large implementation.
- Prefer checkable criteria ("when X, then Y") over vague goals ("make it better").
- For MCP tools, acceptance criteria must map to [docs/tools.md](../../docs/tools.md) schemas and [docs/phases.md](../../docs/phases.md) gates.

## Plans

- Plans should name **files or areas** to touch, **risks**, and **test plan** (what to run or add).
- If the user is in plan-only mode, **do not** edit files or run mutating tools until they approve.

## Traceability

- Link specs to issues or tickets when the project uses them.
- Tool or schema changes require updating normative docs before code.
