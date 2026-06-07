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
