# Observability module requirements (Phase 2)

**Target path:** `src/WinGuiMcp/Observability/`

**Spec refs:** [architecture.md#observability-phase-2](../../architecture.md#observability-phase-2)

---

### TR-P2-MOD-Observability-001 - Trajectory logging

**Priority:** Should  
**Depends on:** TR-P1-MOD-McpHost-004  
**Spec refs:** [architecture.md#observability-phase-2](../../architecture.md#observability-phase-2)

**Requirement:** Optional env-flag trajectory log SHALL record tool name, params (refs only, not bboxes), snapshotId, timing, warnings.

**Acceptance criteria:**
- [ ] Log not sent to agent by default.
- [ ] Disabled by default in Phase 1 builds.

**Verification:** `(manual)` Enable flag; run gate flow; inspect log file.

**Out of scope:** Replay export.

---

### TR-P2-MOD-Observability-002 - traceId in errors

**Priority:** Should  
**Depends on:** TR-P2-MOD-Observability-001  
**Spec refs:** [errors.md#optional-details-fields-phase-2](../../errors.md#optional-details-fields-phase-2)

**Requirement:** Errors MAY include details.traceId linking to trajectory log entry.

**Acceptance criteria:**
- [ ] traceId present when logging enabled.

**Verification:** Phase 2 failure injection test.

**Out of scope:** External telemetry exporters.
