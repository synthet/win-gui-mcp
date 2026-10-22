# Spec Kit concepts adopted by this framework

This project does not vendor GitHub Spec Kit, but it adopts the parts that fit this repository's existing agent workflow and generated asset model.

## Adopted ideas

- **Spec-driven development:** Start with product scenarios and observable outcomes before implementation details.
- **Constitution / principles gate:** Check plans against `AGENTS.md`, `.agent/SAFETY.md`, package boundaries, and project-specific governance before coding.
- **Clarify before planning:** Ask only the questions that would materially change scope, acceptance criteria, tests, or architecture; otherwise record assumptions.
- **Separate plan from tasks:** Keep architecture and approach in `/plan`, then create ordered, traceable work slices in `/tasks`.
- **Traceability:** Maintain `AC-n` acceptance criteria IDs from spec through plan, tasks, implementation evidence, and validation.
- **Analyze for coverage:** Before implementation, confirm every user story and acceptance criterion is represented by tests, tasks, or documented non-goals.
- **Dedicated quality gates:** Keep clarification and cross-artifact analysis as first-class workflow steps so agents do not bury uncertainty inside implementation.

## Local workflow mapping

| Spec Kit concept | Local asset |
|------------------|-------------|
| Constitution | `AGENTS.md`, `.agent/SAFETY.md`, `.agent/PROJECT_GUIDE.md` |
| Specify | `/spec`, `.agent/workflows/spec.md` |
| Clarify | `/clarify`, clarification pass in `/spec` |
| Analyze | `/analyze`, traceability checks in `/tasks` and `validate-implementation` |
| Plan | `/plan`, `.agent/workflows/plan.md` |
| Tasks | `/tasks`, `.agent/workflows/tasks.md` |
| Implement | `/implement`, `.agent/workflows/implement.md` |
| Validate / converge | `/test-and-fix`, `validate-implementation`, `/pr-ready` |

## Best practices for maintainers

1. Preserve stable IDs: do not renumber `AC-n` or `T-n` IDs after review unless the artifact is explicitly regenerated.
2. Prefer assumptions over hidden decisions: if a question does not block progress, document the assumption and continue.
3. Keep scope pressure visible: put follow-up ideas in an out-of-scope section instead of quietly expanding implementation tasks.
4. Make tests traceable: cite the `AC-n` IDs covered by each test or verification command.
5. Regenerate mirrors after command or skill changes: edit `.claude/` and `.agent/`, then run `python scripts/sync_assistant_trees.py`.
