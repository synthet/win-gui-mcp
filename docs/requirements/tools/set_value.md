# set_value tool requirements

Schema: [tools.md#set_value](../../tools.md#set_value).

**Target path:** `src/WinGuiMcp/Tools/SetValueTool.cs`

---

### TR-P1-TOOL-SetValue-001 - ValuePattern only

**Priority:** Must  
**Depends on:** TR-P1-MOD-PatternExecutor-003  
**Spec refs:** [tools.md#set_value](../../tools.md#set_value)

**Requirement:** Tool SHALL set value via UIA ValuePattern.SetValue for `ref` and `value`. No SendInput fallback in Phase 1.

**Acceptance criteria:**
- [ ] Success returns `{ ok: true }`.
- [ ] Missing ValuePattern → `PatternNotSupported`.

**Verification:** `(manual)` Save dialog filename field in Notepad gate.

**Out of scope:** RangeValuePattern spinners (Phase 2).

---

### TR-P1-TOOL-SetValue-002 - includeSnapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-SetValue-001  
**Spec refs:** [tools.md#includesnapshot-on-action-tools](../../tools.md#includesnapshot-on-action-tools)

**Requirement:** Optional includeSnapshot returns snapshotDelta on success.

**Acceptance criteria:**
- [ ] Same contract as click/type_text.

**Verification:** `(manual)` set_value with includeSnapshot true.

**Out of scope:** Warning if value unchanged (Phase 2).
