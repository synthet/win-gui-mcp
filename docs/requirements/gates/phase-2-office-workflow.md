# Phase 2 gate - Office-style workflow

Source: [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness).

**Pass rule:** Multi-step Notepad to WordPad (or equivalent): open, edit, format, save without stale-ref failures.

---

### TR-P2-GATE-Office-001 - End-to-end office workflow

**Priority:** Must  
**Depends on:** Phase 2 tool and event TRs  
**Spec refs:** [phases.md#acceptance-gate-1](../../phases.md#acceptance-gate-1)

**Requirement:** Agent completes office-style workflow using refs and patterns; no coordinate-only clicks.

**Acceptance criteria:**
- [ ] No stale-ref failures mid-flow with event-enhanced wait_for.
- [ ] wait_for succeeds on slow dialog (< 10 s).

**Verification:** `/phase-gate` Phase 2 extension.

---

### TR-P2-GATE-Office-002 - Benchmark recording

**Priority:** Should  
**Depends on:** TR-P1-X-Performance-004  
**Spec refs:** [phases.md#acceptance-gate-1](../../phases.md#acceptance-gate-1)

**Requirement:** Record p50 snapshot latency and median token count for File Explorer snapshot.

**Acceptance criteria:**
- [ ] Metrics documented in gate output.

**Verification:** Manual timing during gate run.

**Out of scope:** SLA enforcement.
