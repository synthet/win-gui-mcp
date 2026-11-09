# type_text tool requirements

Schema: [tools.md#type_text](../../tools.md#type_text).

**Target path:** `src/WinGuiMcp/Tools/TypeTextTool.cs`

---

### TR-P1-TOOL-TypeText-001 - ValuePattern vs SendInput

**Priority:** Must  
**Depends on:** TR-P1-MOD-PatternExecutor-003, TR-P1-MOD-InputSimulator-003  
**Spec refs:** [tools.md#type_text](../../tools.md#type_text)

**Requirement:** Tool SHALL use ValuePattern.SetValue when element supports ValuePattern AND (`clearFirst: true` OR full field replace). Otherwise SendInput Unicode keystrokes. Report `method: ValuePattern` or SendInput equivalent.

**Acceptance criteria:**
- [ ] Optional `ref` focuses element via UIA SetFocus first.
- [ ] No PostMessage keyboard.

**Verification:** `(manual)` Notepad type_text without ref (foreground).

**Out of scope:** Per-character delay simulation.

---

### TR-P1-TOOL-TypeText-002 - submit and clearFirst

**Priority:** Must  
**Depends on:** TR-P1-TOOL-TypeText-001  
**Spec refs:** [tools.md#type_text](../../tools.md#type_text)

**Requirement:** `submit: true` SHALL send Enter after text. `clearFirst: true` SHALL clear before entry when using ValuePattern path.

**Acceptance criteria:**
- [ ] File Explorer rename flow can confirm with submit if needed.

**Verification:** `/phase-gate` flows using type_text.

**Out of scope:** IME composition exhaustive testing.

---

### TR-P1-TOOL-TypeText-003 - includeSnapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-TypeText-001  
**Spec refs:** [tools.md#includesnapshot-on-action-tools](../../tools.md#includesnapshot-on-action-tools)

**Requirement:** Optional includeSnapshot behaves same as click tool TR.

**Acceptance criteria:**
- [ ] Full tree refresh in snapshotDelta when true.

**Verification:** `(manual)` type_text with includeSnapshot.

**Out of scope:** Phase 2 warnings.

---

### TR-P1-TOOL-TypeText-004 - Keyboard target window

**Priority:** Must  
**Depends on:** TR-P1-TOOL-TypeText-001  
**Spec refs:** [tools.md#keyboard-input-target](../../tools.md#keyboard-input-target)

**Requirement:** Tool SHALL accept optional `handle`. Without `ref`, it SHALL focus that window and verify it holds the foreground before sending keystrokes, failing with `FocusFailed` otherwise. SendInput-path responses SHALL include `target: { handle, title }`.

**Acceptance criteria:**
- [ ] `type_text` with a `handle` that cannot take the foreground returns `FocusFailed` and types nothing.
- [ ] SendInput-path responses name the foreground window in `target`.

**Verification:** unit test with an invalid handle; `(manual)` typing into Notepad by handle.

**Out of scope:** Typing into a background window without focusing it.
