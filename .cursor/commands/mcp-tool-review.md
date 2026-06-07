# /mcp-tool-review — Review MCP tool changes

Use when adding, changing, or reviewing MCP tool handlers, schemas, or transport config.

## Goal

Review MCP server changes for capability boundaries, JSON schemas, security surface, and spec alignment.

## Context to read

- `.cursor/rules/20-security-and-safety.mdc`
- `.cursor/skills/mcp-server-design/SKILL.md`
- [docs/tools.md](../../docs/tools.md), [docs/errors.md](../../docs/errors.md), [docs/security.md](../../docs/security.md)
- `src/WinGuiMcp/Transport/`, `src/WinGuiMcp/Tools/`

## Steps

1. List all tools added or changed.
2. For each tool: verify name, parameters, and return shape match [docs/tools.md](../../docs/tools.md).
3. Confirm typed errors use codes from [docs/errors.md](../../docs/errors.md) with recovery hints.
4. Confirm UIA patterns are preferred over coordinate/SendInput clicks.
5. Check for escape hatches: arbitrary shell execution, unrestricted file access, network calls.
6. Verify `.cursor/mcp.example.json` has no secrets; stdio transport only for MVP.
7. Confirm UIPI/elevation behavior matches [docs/security.md](../../docs/security.md).

## Acceptance criteria

- Every tool mapped to a module in [docs/architecture.md](../../docs/architecture.md).
- Abuse cases noted (elevated targets, secure desktop, stale refs).
- No tool names or schemas invented beyond spec.

## Expected output

Table: Tool | Spec match | Security | Error handling | Issues

## Related

- Skill: `mcp-server-design`, `security-review`
- Docs: [docs/CANONICAL_SOURCES.md](../../docs/CANONICAL_SOURCES.md)
