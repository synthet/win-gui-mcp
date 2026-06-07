# MCP host module requirements

Transport layer: stdio MCP server, DI, tool registration. Layout: [architecture.md#proposed-module-layout](../../architecture.md#proposed-module-layout).

**Target path:** `src/WinGuiMcp/Transport/`, `Program.cs`

---

### TR-P1-MOD-McpHost-001 - .NET 9 hosting bootstrap

**Priority:** Must  
**Depends on:** —  
**Spec refs:** [stack.md](../../stack.md), [architecture.md#transport](../../architecture.md#transport)

**Requirement:** `Program.cs` SHALL use `Microsoft.Extensions.Hosting` to bootstrap a console MCP server targeting **.NET 9**, replacing the Hello World stub.

**Acceptance criteria:**
- [ ] `dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj` starts without unhandled exception.
- [ ] NuGet packages match stack.md baseline: `ModelContextProtocol`, `Microsoft.Extensions.Hosting`, FlaUI 5.

**Verification:** `dotnet build WinGuiMcp.sln` && `dotnet run --project src/WinGuiMcp/WinGuiMcp.csproj` (stdio; interrupt after start).

**Out of scope:** HTTP/SSE transport (Phase 4 design).

---

### TR-P1-MOD-McpHost-002 - Stdio MCP transport

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-001  
**Spec refs:** [stack.md#stack-by-layer](../../stack.md#stack-by-layer), [AGENTS.md](../../AGENTS.md)

**Requirement:** The server SHALL expose MCP over **stdio JSON-RPC 2.0** using the `ModelContextProtocol` SDK. Server key for local dev: `win-gui-mcp`.

**Acceptance criteria:**
- [ ] MCP client can connect via `.cursor/mcp.json` `dotnet run` entry.
- [ ] `tools/list` returns registered Phase 1 tools.

**Verification:** `(manual)` Cursor MCP reload; verify tool list.

**Out of scope:** Bearer auth, network binding.

---

### TR-P1-MOD-McpHost-003 - Phase 1 tool registration

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-002  
**Spec refs:** [tools.md#phase-1--mvp-tools](../../tools.md#phase-1--mvp-tools), [phases.md#phase-1--mvp-uia-only](../../phases.md#phase-1--mvp-uia-only)

**Requirement:** The host SHALL register all **12 Phase 1 tools** with schemas derived from tools.md: `list_windows`, `launch_app`, `focus_window`, `snapshot`, `find_element`, `get_properties`, `click`, `type_text`, `set_value`, `hotkey`, `screenshot`, `wait_for`.

**Acceptance criteria:**
- [ ] Tool names match tools.md exactly (snake_case).
- [ ] Input JSON schemas reject `additionalProperties` where spec requires.

**Verification:** `(manual)` MCP `tools/list` count and name check.

**Out of scope:** Phase 2+ tools in registration until Phase 2.

---

### TR-P1-MOD-McpHost-004 - Structured error routing

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-003  
**Spec refs:** [errors.md#error-envelope](../../errors.md#error-envelope)

**Requirement:** Tool dispatch SHALL catch domain failures and return structured JSON errors per TR-P1-X-ErrorEnvelope-001. Unhandled exceptions MAY map to `InternalError`.

**Acceptance criteria:**
- [ ] No silent swallow of tool failures.
- [ ] MCP tool result content is JSON-parseable for failures.

**Verification:** `(manual)` Trigger `InvalidArgument`; verify envelope.

**Out of scope:** Trajectory logging (Phase 2).
