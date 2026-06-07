# focus_window tool requirements

Schema: [tools.md#focus_window](../../tools.md#focus_window).

**Target path:** `src/WinGuiMcp/Tools/FocusWindowTool.cs`

---

### TR-P1-TOOL-FocusWindow-001 - Foreground window

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowEnumerator-002  
**Spec refs:** [tools.md#focus_window](../../tools.md#focus_window)

**Requirement:** Tool SHALL bring `handle` to foreground and return `{ ok: true, handle, focused: true }`.

**Acceptance criteria:**
- [ ] After success, target is foreground for subsequent SendInput.
- [ ] `WindowGone`, `FocusFailed`, `UIPIBlocked` per errors.md.

**Verification:** MCP `tools/call` name=`focus_window`, arguments=`{"handle": <hwnd>}`

**Out of scope:** Restoring from tray-only apps.

---

### TR-P1-TOOL-FocusWindow-002 - Required handle

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FocusWindow-001  
**Spec refs:** [tools.md#focus_window](../../tools.md#focus_window)

**Requirement:** `handle` is required; `handle: 0` → `InvalidArgument`.

**Acceptance criteria:**
- [ ] Missing handle rejected at schema validation.

**Verification:** MCP call with invalid handle 0.

**Out of scope:** Title-based focus (use list_windows + focus).
