# Window enumerator module requirements

**Target path:** `src/WinGuiMcp/Platform/WindowEnumerator.cs`

**Spec refs:** [tools.md#list_windows](../../tools.md#list_windows), [tools.md#focus_window](../../tools.md#focus_window)

---

### TR-P1-MOD-WindowEnumerator-001 - EnumWindows listing

**Priority:** Must  
**Depends on:** TR-P1-MOD-DpiManager-001  
**Spec refs:** [tools.md#list_windows](../../tools.md#list_windows)

**Requirement:** Window enumerator SHALL list top-level visible windows via Win32 `EnumWindows` with UIA root validation, returning handle, title, processName, pid, isForeground, isMinimized.

**Acceptance criteria:**
- [ ] `filter` case-insensitive substring on title or process name.
- [ ] `includeMinimized: false` excludes minimized by default.

**Verification:** `(manual)` MCP `list_windows` with filter `calc`.

**Out of scope:** Bbox in list_windows output.

---

### TR-P1-MOD-WindowEnumerator-002 - Focus window

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowEnumerator-001  
**Spec refs:** [tools.md#focus_window](../../tools.md#focus_window)

**Requirement:** Focus SHALL use `SetForegroundWindow` with `AttachThreadInput` fallback when needed.

**Acceptance criteria:**
- [ ] Success returns `{ ok: true, handle, focused: true }`.
- [ ] Invalid handle → `WindowGone`.
- [ ] UIPI block → `UIPIBlocked`.

**Verification:** `(manual)` Focus Calculator from another app.

**Out of scope:** Restoring minimized window policy beyond spec errors.

---

### TR-P1-MOD-WindowEnumerator-003 - HWND validation

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowEnumerator-001  
**Spec refs:** [tools.md#common-response-contract](../../tools.md#common-response-contract)

**Requirement:** Tools accepting `handle` SHALL reject `handle: 0` with `InvalidArgument`.

**Acceptance criteria:**
- [ ] Zero handle never passed to FlaUI attach.

**Verification:** `(manual)` snapshot with handle 0.

**Out of scope:** Hex string handles (rejected by schema).
