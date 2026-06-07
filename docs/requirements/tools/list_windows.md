# list_windows tool requirements

MCP tool: enumerate top-level windows. Schema: [tools.md#list_windows](../../tools.md#list_windows).

**Target path:** `src/WinGuiMcp/Tools/ListWindowsTool.cs` (or equivalent)

---

### TR-P1-TOOL-ListWindows-001 - Window list output

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowEnumerator-001, TR-P1-MOD-McpHost-003  
**Spec refs:** [tools.md#list_windows](../../tools.md#list_windows)

**Requirement:** Tool SHALL return `{ ok: true, windows: [...] }` with entries containing handle, title, processName, pid, isForeground, isMinimized.

**Acceptance criteria:**
- [ ] Empty filter returns all visible top-level windows (subject to includeMinimized).
- [ ] No bbox fields in output.

**Verification:**
1. `dotnet build WinGuiMcp.sln`
2. MCP `tools/call` name=`list_windows`, arguments=`{}`

**Out of scope:** attach/snapshot side effects.

---

### TR-P1-TOOL-ListWindows-002 - Filter and minimized semantics

**Priority:** Must  
**Depends on:** TR-P1-TOOL-ListWindows-001  
**Spec refs:** [tools.md#list_windows](../../tools.md#list_windows)

**Requirement:** Tool SHALL support optional `filter` (case-insensitive substring on title or processName) and `includeMinimized` (default false).

**Acceptance criteria:**
- [ ] Filter `notepad` matches Notepad process.
- [ ] Minimized windows excluded when includeMinimized false.

**Verification:** MCP `tools/call` name=`list_windows`, arguments=`{"filter":"Calculator"}`

**Out of scope:** Regex filter on title.
