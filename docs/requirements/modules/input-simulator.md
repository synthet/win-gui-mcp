# Input simulator module requirements

**Target path:** `src/WinGuiMcp/Action/InputSimulator.cs`

**Spec refs:** [architecture.md#action-layer-puppeteer](../../architecture.md#action-layer-puppeteer), [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

---

### TR-P1-MOD-InputSimulator-001 - SendInput-only input

**Priority:** Must  
**Depends on:** —  
**Spec refs:** [architecture.md#control-stack-resolution-order](../../architecture.md#control-stack-resolution-order)

**Requirement:** Fallback input SHALL use P/Invoke **SendInput** only. The server SHALL NOT use `mouse_event`, keyboard `PostMessage`, hooks, or virtual HID.

**Acceptance criteria:**
- [ ] Code search shows no `mouse_event` for actuation.
- [ ] Hotkey and type SendInput paths documented.

**Verification:** `dotnet build WinGuiMcp.sln`; code review / grep.

**Out of scope:** SendMessage for known contracts (Phase 2 opt-in).

---

### TR-P1-MOD-InputSimulator-002 - Focus before fallback click

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-001, TR-P1-MOD-WindowEnumerator-001  
**Spec refs:** [tools.md#click](../../tools.md#click), [security.md#threat-model](../../security.md#threat-model)

**Requirement:** Before SendInput click at bbox center, the server SHALL ensure target window is foreground via `focus_window` semantics.

**Acceptance criteria:**
- [ ] SendInput click after focus succeeds on standard apps.
- [ ] Focus failure surfaces `FocusFailed`.

**Verification:** `(manual)` Click via ref on background window.

**Out of scope:** Cross-thread input without focus.

---

### TR-P1-MOD-InputSimulator-003 - Hotkey and unicode typing

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-001  
**Spec refs:** [tools.md#hotkey](../../tools.md#hotkey), [tools.md#type_text](../../tools.md#type_text)

**Requirement:** Simulator SHALL parse modifier chords (`ctrl+s`, etc.) and emit Unicode keystrokes for `type_text` SendInput path.

**Acceptance criteria:**
- [ ] Invalid chord → `InvalidArgument`.
- [ ] `submit: true` sends Enter after text.

**Verification:** `(manual)` Notepad hotkey Ctrl+S and type_text.

**Out of scope:** Locale-specific dead keys exhaustive matrix.

---

### TR-P1-MOD-InputSimulator-004 - Point click escape hatch

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-002  
**Spec refs:** [tools.md#click](../../tools.md#click)

**Requirement:** `click` with `point` `{x,y}` SHALL use SendInput only (no ref lookup). This path is for debugging, not gate acceptance.

**Acceptance criteria:**
- [ ] Point clicks work in physical pixels.
- [ ] Gate docs forbid point-only flows.

**Verification:** `(manual)` Debug click at known coordinate.

**Out of scope:** Gate validation via point clicks.
