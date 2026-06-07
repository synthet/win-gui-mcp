# Phase 4 gate - UIPI and secure desktop

Source: [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Supervision required:** UAC and elevation tests need human operator.

---

### TR-P4-GATE-Security-001 - UIPI blocked Task Manager

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-001  
**Spec refs:** [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Requirement:** Non-elevated server targeting elevated Task Manager returns `UIPIBlocked` (not hang, not silent failure).

**Verification:** Supervised gate step.

---

### TR-P4-GATE-Security-002 - Elevated server Notepad

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-003  
**Spec refs:** [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Requirement:** Elevated server automates non-elevated Notepad successfully.

**Verification:** Supervised gate step.

---

### TR-P4-GATE-Security-003 - UAC SecureDesktop

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-002  
**Spec refs:** [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Requirement:** Active UAC secure desktop prompt returns `SecureDesktop`, not hang.

**Verification:** Supervised gate step with user cancel.

**Out of scope:** Automating UAC choice.
