# Phase 1 gate - WPF sample

Source: [phases.md#wpf-sample](../../phases.md#wpf-sample).

**Prerequisite:** WPF sample app in test environment.

**Pass rule:** Ref-based only; no `click.point`.

---

### TR-P1-GATE-Wpf-001 - Launch and snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-LaunchApp-001, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#wpf-sample](../../phases.md#wpf-sample)

**Requirement:** Launch WPF sample; `snapshot`.

**Verification:** Gate step 1.

---

### TR-P1-GATE-Wpf-002 - Menu navigation

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#wpf-sample](../../phases.md#wpf-sample)

**Requirement:** `click` menu item via ref.

**Verification:** Gate step 2.

---

### TR-P1-GATE-Wpf-003 - Dialog wait and interact

**Priority:** Must  
**Depends on:** TR-P1-TOOL-WaitFor-001, TR-P1-TOOL-Click-001, TR-P1-TOOL-SetValue-001  
**Spec refs:** [phases.md#wpf-sample](../../phases.md#wpf-sample)

**Requirement:** `wait_for` dialog; `click` or `set_value` in dialog via refs.

**Verification:** Gate step 3.

---

### TR-P1-GATE-Wpf-004 - Verify dialog result

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#wpf-sample](../../phases.md#wpf-sample)

**Requirement:** `snapshot` verifies dialog outcome in main window tree.

**Verification:** Gate step 4.

**Out of scope:** Custom chrome coordinate hacks.
