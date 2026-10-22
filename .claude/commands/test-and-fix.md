# /test-and-fix — Run tests and repair failures

Use when CI is red, tests fail locally, or the user asks for a test pass.

## Inputs

- **AGENTS.md** — canonical test commands.
- Failing log output or error messages if available.

## Steps

1. Run `dotnet build WinGuiMcp.sln`.
2. If test projects exist, run `dotnet test`.
3. For each failure: locate root cause, fix **minimal** code or test expectation.
4. Re-run until green or blocked; if blocked, document what is needed (env, flaky test).

## Done when

- Build (and tests if present) pass, or there is a clear written blocker with owner/next step.

## Avoid

- Disabling tests or weakening assertions without explicit user approval.

## Related

- Skill: none
- Docs: [AGENTS.md](../../AGENTS.md)
