---
name: mcp-server-design
description: Design and implement the WinGuiMcp MCP server — C# ModelContextProtocol SDK, stdio transport, tool schemas, structured errors.
---

# MCP server design (WinGuiMcp)

## Use this skill when

- Working in `src/WinGuiMcp/Transport/` or `src/WinGuiMcp/Tools/`
- Adding or reviewing MCP tools, resources, or prompts
- User runs `/mcp-tool-review`
- Configuring `.cursor/mcp.example.json`

## Procedure

1. Read `.cursor/rules/20-security-and-safety.mdc` and [MCP specification](https://modelcontextprotocol.io).
2. Use `ModelContextProtocol` NuGet package; server name **`win-gui-mcp`**.
3. **Transport:** stdio only for MVP (no HTTP without auth — Phase 4).
4. **Tool I/O:** match JSON schemas in [docs/tools.md](../../../docs/tools.md).
5. **Errors:** structured responses with codes from [docs/errors.md](../../../docs/errors.md) and recovery hints for agents.
6. **Registration:** central tool registry in `Transport/` or `Tools/`; handlers delegate to Session/Perception/Action.
7. On startup failure: return diagnostics in logs; do not throw opaque errors to MCP clients.

## Safety checks

- No arbitrary shell, file, or network tools without explicit spec and security review.
- No secrets in tool responses or `mcp.example.json`.
- UIPI/elevation failures return `UIPIBlocked` with actionable message.
- Secure desktop / UAC: fail fast with `SecureDesktop`.

## Done criteria

- Tool list matches [docs/tools.md](../../../docs/tools.md) for current phase.
- `dotnet build WinGuiMcp.sln` passes.
- `.cursor/mcp.example.json` points to built `dotnet run` entry.

## Related

- Command: `/mcp-tool-review`
- Skill: `security-review`, `win-gui-mcp-tools`
- Docs: [docs/security.md](../../../docs/security.md)
