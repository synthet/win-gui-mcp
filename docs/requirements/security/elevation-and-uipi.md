# Elevation and UIPI requirements (Phase 4)

**Spec refs:** [security.md#integrity-levels-and-uipi](../../security.md#integrity-levels-and-uipi), [errors.md#uipi-and-elevation](../../errors.md#uipi-and-elevation)

---

### TR-P4-SEC-Elevation-001 - UIPIBlocked on action paths

**Priority:** Must  
**Depends on:** TR-P1-X-ErrorEnvelope-002  
**Spec refs:** [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Requirement:** All action paths crossing process boundary SHALL return structured `UIPIBlocked` when target integrity exceeds server (non-elevated server vs elevated app).

**Acceptance criteria:**
- [ ] Message includes actionable hint per errors.md.
- [ ] details.serverElevated, targetProcess, targetPid when available.

**Verification:** Phase 4 gate Task Manager test.

---

### TR-P4-SEC-Elevation-002 - SecureDesktop fail-fast

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-001  
**Spec refs:** [security.md#hard-failure-scenarios](../../security.md#hard-failure-scenarios)

**Requirement:** UAC/secure desktop/locked workstation SHALL return `SecureDesktop` or `FocusFailed` without hang.

**Acceptance criteria:**
- [ ] No infinite wait on secure desktop.

**Verification:** Phase 4 gate UAC prompt test (supervised).

---

### TR-P4-SEC-Elevation-003 - Elevated server down-target

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-001  
**Spec refs:** [phases.md#acceptance-gate-3](../../phases.md#acceptance-gate-3)

**Requirement:** Elevated server SHALL automate non-elevated Notepad successfully.

**Verification:** Phase 4 gate.

**Out of scope:** Elevation bypass techniques.
