# Pattern executor module requirements

**Target path:** `src/WinGuiMcp/Action/PatternExecutor.cs`

**Spec refs:** [architecture.md#action-layer-puppeteer](../../architecture.md#action-layer-puppeteer)

---

### TR-P1-MOD-PatternExecutor-001 - InvokePattern for click

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-001  
**Spec refs:** [tools.md#click](../../tools.md#click)

**Requirement:** For UIA-sourced elements with Invoke pattern, `click` SHALL use `InvokePattern.Invoke()` and report `method: InvokePattern`.

**Acceptance criteria:**
- [ ] Buttons and menu items prefer Invoke over SendInput.
- [ ] Response includes `method` field on success.

**Verification:** `(manual)` Click Calculator digit via ref; inspect `method`.

**Out of scope:** ExpandCollapse, Scroll patterns (Phase 2 tools).

---

### TR-P1-MOD-PatternExecutor-002 - TogglePattern for checkboxes

**Priority:** Must  
**Depends on:** TR-P1-MOD-PatternExecutor-001  
**Spec refs:** [tools.md#click](../../tools.md#click)

**Requirement:** For checkbox-like controls, `click` SHALL try `TogglePattern` when Invoke is unavailable and report `method: TogglePattern`.

**Acceptance criteria:**
- [ ] Settings toggle can be actuated via ref click without SendInput when pattern exists.

**Verification:** `/phase-gate` Settings toggle step.

**Out of scope:** Dedicated `toggle` tool (Phase 2).

---

### TR-P1-MOD-PatternExecutor-003 - ValuePattern for set and type

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-001  
**Spec refs:** [tools.md#set_value](../../tools.md#set_value), [tools.md#type_text](../../tools.md#type_text)

**Requirement:** Executor SHALL implement `ValuePattern.SetValue` for `set_value` and for `type_text` when spec conditions met (clearFirst or full replace).

**Acceptance criteria:**
- [ ] `set_value` without ValuePattern → `PatternNotSupported`.
- [ ] `type_text` reports `method: ValuePattern` or `SendInput` appropriately.

**Verification:** `(manual)` Notepad `type_text` and save dialog `set_value`.

**Out of scope:** TextPattern (Phase 2 `get_text`).

---

### TR-P1-MOD-PatternExecutor-004 - Phase 2 pattern expansion note

**Priority:** Should (Phase 2)  
**Depends on:** TR-P1-MOD-PatternExecutor-003  
**Spec refs:** [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness)

**Requirement:** Phase 2 SHALL extend executor for ExpandCollapse, Scroll, Selection, Toggle dedicated tools without breaking P1 Invoke/Value paths.

**Acceptance criteria:**
- [ ] Documented in Phase 2 `pattern-executor` update TRs.

**Verification:** Phase 2 tool tests.

**Out of scope:** Phase 1 full pattern coverage.

---

## Phase 2 extensions

### TR-P2-MOD-PatternExecutor-005 - Full pattern coverage

**Priority:** Must (Phase 2)  
**Depends on:** TR-P2-TOOL-Toggle-001, TR-P2-TOOL-ExpandCollapse-001, TR-P2-TOOL-Select-001, TR-P2-TOOL-Scroll-001  
**Spec refs:** [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness)

**Requirement:** Executor SHALL support ExpandCollapse, Scroll, Selection, dedicated Toggle per Phase 2 tools.

**Verification:** Phase 2 tool manual tests.
