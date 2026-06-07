# Performance budget requirements

Informational targets for regression awareness. Not SLAs. Source: [phases.md#performance-budgets](../../phases.md#performance-budgets).

---

### TR-P1-X-Performance-001 - list_windows latency

**Priority:** Could (informational)  
**Depends on:** TR-P1-TOOL-ListWindows-001  
**Spec refs:** [phases.md#performance-budgets](../../phases.md#performance-budgets)

**Requirement:** On a local desktop, `list_windows` SHOULD complete within **30 ms p95** when no heavy filter is applied.

**Acceptance criteria:**
- [ ] Document environment when measuring (CPU, monitor count, open window count).

**Verification:** `(manual)` Time 20 calls; record p95 during `/phase-gate` or benchmark script.

**Out of scope:** Formal CI perf gate in Phase 1.

---

### TR-P1-X-Performance-002 - Settings snapshot gate metric

**Priority:** Should (Phase 1 gate)  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#phase-1--mvp-uia-only](../../phases.md#phase-1--mvp-uia-only)

**Requirement:** Full `snapshot` on Windows Settings (typical navigation page) SHOULD complete in **< 2 s** for Phase 1 gate informational metric.

**Acceptance criteria:**
- [ ] Gate runner records elapsed ms for Settings snapshot step.
- [ ] Failures do not block MVP if functional gates pass (metric is informational per phases.md).

**Verification:** `/phase-gate` Settings flow step 1.

---

### TR-P1-X-Performance-003 - Calculator token size gate metric

**Priority:** Should (Phase 1 gate)  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#phase-1--mvp-uia-only](../../phases.md#phase-1--mvp-uia-only)

**Requirement:** Calculator window snapshot JSON SHOULD be **< 8k tokens** (informational gate metric).

**Acceptance criteria:**
- [ ] Token count recorded during calculator gate flow.

**Verification:** `/phase-gate` Calculator flow after snapshot.

---

### TR-P1-X-Performance-004 - Core operation p95 table

**Priority:** Could (informational)  
**Depends on:** Phase 1 tool TRs  
**Spec refs:** [phases.md#performance-budgets](../../phases.md#performance-budgets)

**Requirement:** Phase 2 SHOULD measure and document p95 for:

| Operation | Proposed p95 |
|-----------|--------------|
| UIA tree walk (cached, modest tree) | < 100 ms |
| `screenshot` (single window, WGC) | < 40 ms |
| End-to-end `click` dispatch (excl. app response) | < 250 ms |
| Full `snapshot` (tree, business apps) | < 200 ms |

**Acceptance criteria:**
- [ ] Phase 1: no blocking requirement.
- [ ] Phase 2: benchmark results recorded in repo or gate output.

**Verification:** Phase 2 benchmark pass.

**Out of scope:** Phase 3 ROI OCR / vision latency (see Phase 3 gate TRs).
