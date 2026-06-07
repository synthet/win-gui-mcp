# Phase 1 gate - Settings

Source: [phases.md#settings](../../phases.md#settings).

**Pass rule:** Ref-based only; no `click.point`. Record snapshot latency (TR-P1-X-Performance-002).

---

### TR-P1-GATE-Settings-001 - Open and snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#settings](../../phases.md#settings)

**Requirement:** Open Settings app; `snapshot`. Record elapsed time.

**Acceptance criteria:**
- [ ] Latency logged for performance metric.

**Verification:** Gate step 1.

---

### TR-P1-GATE-Settings-002 - Navigate to page

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FindElement-001, TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#settings](../../phases.md#settings)

**Requirement:** `find_element` / `click` navigate (e.g. System page) via ref.

**Verification:** Gate step 2.

---

### TR-P1-GATE-Settings-003 - Toggle control

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001, TR-P1-MOD-PatternExecutor-002  
**Spec refs:** [phases.md#settings](../../phases.md#settings)

**Requirement:** `click` toggle or button via ref (TogglePattern when applicable).

**Verification:** Gate step 3.

---

### TR-P1-GATE-Settings-004 - Verify state change

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#settings](../../phases.md#settings)

**Requirement:** `snapshot` shows state change in tree (value/name/states).

**Verification:** Gate step 4.

**Out of scope:** All Settings pages (one page sufficient).
