# SDLC core

Read root **AGENTS.md** first when present; it overrides generic guidance for commands, layout, and boundaries.

## Execution

- Prefer **small, focused** changes over drive-by refactors.
- **Run** `dotnet build WinGuiMcp.sln` before claiming work is complete; if tests exist, run `dotnet test` too. If they cannot run, say why.
- Use the **terminal** to investigate; do not only suggest commands for the user to run.
- **Do not** commit secrets, API keys, or `.env` files with real credentials.

## Communication

- Use proper **code citations** when referencing existing code (line-range blocks with path).
- Keep explanations proportional to task complexity.

## Scope

- Implement what was asked; avoid expanding scope unless the user explicitly widens it.
