# Phase 1 gate - WinForms sample

Source: [phases.md#winforms-sample](../../phases.md#winforms-sample).

**Prerequisite:** WinForms sample app available in test environment (path documented in gate run notes).

**Pass rule:** Ref-based only; no `click.point`.

---

### TR-P1-GATE-WinForms-001 - Launch and snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-LaunchApp-001, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#winforms-sample](../../phases.md#winforms-sample)

**Requirement:** Launch sample app; `snapshot`.

**Verification:** Gate step 1.

---

### TR-P1-GATE-WinForms-002 - Button click

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#winforms-sample](../../phases.md#winforms-sample)

**Requirement:** `click` button via ref (InvokePattern).

**Verification:** Gate step 2.

---

### TR-P1-GATE-WinForms-003 - Text box fill

**Priority:** Must  
**Depends on:** TR-P1-TOOL-SetValue-001 or TR-P1-TOOL-TypeText-001  
**Spec refs:** [phases.md#winforms-sample](../../phases.md#winforms-sample)

**Requirement:** `set_value` or `type_text` on text box ref.

**Verification:** Gate step 3.

---

### TR-P1-GATE-WinForms-004 - Verify output

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#winforms-sample](../../phases.md#winforms-sample)

**Requirement:** `snapshot` verifies label or output updated.

**Verification:** Gate step 4.

**Out of scope:** UIA2 fallback testing unless WinForms requires it.
