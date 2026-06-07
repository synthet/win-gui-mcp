# wait_for tool requirements

Schema: [tools.md#wait_for](../../tools.md#wait_for).

**Target path:** `src/WinGuiMcp/Tools/WaitForTool.cs`

---

### TR-P1-TOOL-WaitFor-001 - Poll with backoff

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-003, TR-P1-MOD-UiaSnapshotBuilder-001  
**Spec refs:** [tools.md#wait_for](../../tools.md#wait_for)

**Requirement:** Tool SHALL poll UIA conditions with exponential backoff **100 ms → 500 ms cap** until `timeoutMs` (default 5000).

**Acceptance criteria:**
- [ ] Success returns ok, optional ref, elapsedMs.
- [ ] Timeout → `Timeout`; closed window → `WindowGone`.

**Verification:** MCP wait_for element-exists after opening save dialog.

**Out of scope:** UIA event subscriptions (Phase 2).

---

### TR-P1-TOOL-WaitFor-002 - Condition types

**Priority:** Must  
**Depends on:** TR-P1-TOOL-WaitFor-001  
**Spec refs:** [tools.md#wait_for](../../tools.md#wait_for)

**Requirement:** Tool SHALL support conditions: `element-exists`, `element-enabled`, `text-visible`, `window-active` with `target` locator string.

**Acceptance criteria:**
- [ ] Notepad gate: wait_for Save As dialog title.
- [ ] window-active succeeds when handle is foreground.

**Verification:** `/phase-gate` Notepad and File Explorer flows.

**Out of scope:** Custom predicate language.

---

### TR-P1-TOOL-WaitFor-003 - Slow dialog gate

**Priority:** Should  
**Depends on:** TR-P1-TOOL-WaitFor-001  
**Spec refs:** [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness)

**Requirement:** Phase 1 implementation SHOULD succeed on slow dialogs within 10 s when timeoutMs increased (informational for Phase 2 gate).

**Acceptance criteria:**
- [ ] timeoutMs configurable per call.

**Verification:** `(manual)` wait_for with timeoutMs 10000.

**Out of scope:** Event-driven wait (Phase 2).
