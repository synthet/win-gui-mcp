# Phase 1 gate - Calculator

Source flow: [phases.md#calculator](../../phases.md#calculator). Run via `/phase-gate`.

**Pass rule:** Complete all steps using **ref-based** actions only. **`click.point` fails the gate.**

## Recording template

| Step | Pass | Notes / error codes |
|------|------|---------------------|
| 1–4 | yes/no | |

---

### TR-P1-GATE-Calculator-001 - Launch or focus

**Priority:** Must  
**Depends on:** TR-P1-TOOL-LaunchApp-001, TR-P1-TOOL-FocusWindow-001  
**Spec refs:** [phases.md#calculator](../../phases.md#calculator)

**Requirement:** Agent SHALL `launch_app` `{ path: "calc.exe" }` or focus existing Calculator window.

**Acceptance criteria:**
- [ ] Valid handle obtained.
- [ ] No coordinate clicks.

**Verification:** `/phase-gate` step 1.

---

### TR-P1-GATE-Calculator-002 - Snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#calculator](../../phases.md#calculator)

**Requirement:** `focus_window` then `snapshot` on Calculator handle.

**Acceptance criteria:**
- [ ] Tree contains digit and operator buttons with refs.
- [ ] Record token size for TR-P1-X-Performance-003.

**Verification:** `/phase-gate` step 2.

---

### TR-P1-GATE-Calculator-003 - Digit and operator clicks

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#calculator](../../phases.md#calculator)

**Requirement:** Click sequence via refs (e.g. `2`, `+`, `3`, `=`) using InvokePattern where available.

**Acceptance criteria:**
- [ ] All clicks use `ref`, not `point`.
- [ ] Calculator display updates.

**Verification:** `/phase-gate` step 3.

---

### TR-P1-GATE-Calculator-004 - Read result

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#calculator](../../phases.md#calculator)

**Requirement:** `snapshot` and read result from tree `name` or `value` (expect `5` for 2+3).

**Acceptance criteria:**
- [ ] Result visible in UEM without screenshot OCR.

**Verification:** `/phase-gate` step 4.

**Out of scope:** Vision, point clicks.
