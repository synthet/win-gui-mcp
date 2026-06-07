# Phase 3 gate - UIA-blind demo

Source: [phases.md#acceptance-gate-2](../../phases.md#acceptance-gate-2)

---

### TR-P3-GATE-UiaBlind-001 - Vision locate and click

**Priority:** Must  
**Depends on:** TR-P3-MOD-VisionFusion-003, TR-P3-TOOL-Snapshot-004  
**Spec refs:** [phases.md#acceptance-gate-2](../../phases.md#acceptance-gate-2)

**Requirement:** Agent locates and clicks control in UIA-blind window (canvas demo or game menu) using vision-generated refs.

**Acceptance criteria:**
- [ ] No reliance on empty UIA tree for targeting.
- [ ] click uses SendInput on cv refs.

**Verification:** `/phase-gate` Phase 3.

---

### TR-P3-GATE-UiaBlind-002 - Latency and fusion metrics

**Priority:** Must  
**Depends on:** TR-P3-GATE-UiaBlind-001  
**Spec refs:** [phases.md#acceptance-gate-2](../../phases.md#acceptance-gate-2)

**Requirement:** Document added latency per vision snapshot (target < 3 s on GPU-class hardware; CPU documented separately). Log uiaNodeCount, cvNodeCount, fusedNodeCount.

**Acceptance criteria:**
- [ ] Metrics recorded in gate output.

**Verification:** Manual timing during demo.

**Out of scope:** Production SLA.
