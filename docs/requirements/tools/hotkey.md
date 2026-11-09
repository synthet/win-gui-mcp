# hotkey tool requirements

Schema: [tools.md#hotkey](../../tools.md#hotkey).

**Target path:** `src/WinGuiMcp/Tools/HotkeyTool.cs`

---

### TR-P1-TOOL-Hotkey-001 - Chord via SendInput

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-003  
**Spec refs:** [tools.md#hotkey](../../tools.md#hotkey)

**Requirement:** Tool SHALL parse `keys` chord (e.g. `ctrl+s`, `alt+f4`) and send via SendInput. Response `method: SendInput`.

**Acceptance criteria:**
- [ ] Invalid chord → `InvalidArgument`.
- [ ] No PostMessage keyboard.

**Verification:** MCP `tools/call` name=`hotkey`, arguments=`{"keys":"ctrl+s","handle":<Notepad hwnd>}`

**Out of scope:** Global system hotkeys without focus.

---

### TR-P1-TOOL-Hotkey-002 - Optional handle focus

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Hotkey-001, TR-P1-TOOL-FocusWindow-001  
**Spec refs:** [tools.md#hotkey](../../tools.md#hotkey)

**Requirement:** When `handle` provided, focus that window before sending chord.

**Acceptance criteria:**
- [ ] Focus failure → `FocusFailed`.
- [ ] UIPI → `UIPIBlocked`.

**Verification:** `(manual)` Ctrl+S on Notepad with handle param.

**Out of scope:** includeSnapshot required for gate (optional only).

---

### TR-P1-TOOL-Hotkey-003 - Foreground verification and target report

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Hotkey-002  
**Spec refs:** [tools.md#keyboard-input-target](../../tools.md#keyboard-input-target)

**Requirement:** `hotkey` and `press_key` with `handle` SHALL verify the foreground window immediately before each chord or repetition and stop with `FocusFailed` when it changed. Responses SHALL include `target: { handle, title }`.

**Acceptance criteria:**
- [ ] `press_key` with `count > 1` re-checks the foreground before every repetition.
- [ ] Responses report the foreground window in `target`.

**Verification:** `(manual)` press_key with count while another window steals focus.

**Out of scope:** Preventing other processes from taking the foreground.
