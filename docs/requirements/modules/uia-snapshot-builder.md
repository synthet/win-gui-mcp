# UIA snapshot builder module requirements

**Target path:** `src/WinGuiMcp/Perception/UiaSnapshotBuilder.cs`

**Spec refs:** [architecture.md#uia-path-phase-1](../../architecture.md#uia-path-phase-1), [data-model.md#tree-pruning-rules](../../data-model.md#tree-pruning-rules)

---

### TR-P1-MOD-UiaSnapshotBuilder-001 - FlaUI UIA3 attachment

**Priority:** Must  
**Depends on:** TR-P1-MOD-DpiManager-001  
**Spec refs:** [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

**Requirement:** Snapshot builder SHALL attach to HWND via FlaUI **UIA3** primary backend. UIA2 fallback is allowed only for legacy WinForms per stack.md.

**Acceptance criteria:**
- [ ] Invalid HWND surfaces `WindowGone` or capture failure at tool layer.
- [ ] UIPI-blocked trees return `UIPIBlocked` where applicable.

**Verification:** `(manual)` Snapshot on Calculator and Notepad.

**Out of scope:** MSAA bridge (Phase 2).

---

### TR-P1-MOD-UiaSnapshotBuilder-002 - CacheRequest configuration

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-001  
**Spec refs:** [tools.md#snapshot](../../tools.md#snapshot), [architecture.md#uia-path-phase-1](../../architecture.md#uia-path-phase-1)

**Requirement:** Tree walk SHALL use cached requests including: Name, ControlType, AutomationId, ClassName, IsEnabled, IsOffscreen, BoundingRectangle, and supported interaction patterns.

**Acceptance criteria:**
- [ ] UEM nodes include `patterns[]` when present at snapshot time.
- [ ] `detail: control|content` affects tree shape per data-model.md.

**Verification:** `(manual)` Compare control vs content snapshot on Notepad.

**Out of scope:** Vision fusion (Phase 3).

---

### TR-P1-MOD-UiaSnapshotBuilder-003 - Pruning and ref assignment

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-002, TR-P1-MOD-RefTable-001  
**Spec refs:** [data-model.md#tree-pruning-rules](../../data-model.md#tree-pruning-rules)

**Requirement:** Builder SHALL apply pruning rules (pane hoist, offscreen exclusion, empty node drop, depth cap default 32), assign refs depth-first, and populate RefTable.

**Acceptance criteria:**
- [ ] Interactive control types always kept per data-model table.
- [ ] Truncated subtree marks parent with `states: ["truncated"]` when depth exceeded.

**Verification:** `(manual)` Settings snapshot size and structure check.

**Out of scope:** Duplicate sibling dedup logging (Should; log warning per data-model).

---

### TR-P1-MOD-UiaSnapshotBuilder-004 - Snapshot envelope assembly

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-003  
**Spec refs:** [data-model.md#snapshot-response-envelope](../../data-model.md#snapshot-response-envelope)

**Requirement:** Output SHALL match snapshot envelope: `snapshotId`, `handle`, `title`, `processName`, `timestamp`, `tree` root UEM node.

**Acceptance criteria:**
- [ ] All nodes have required fields: `ref`, `role`, `source` (`uia`).
- [ ] Agent JSON omits bboxes.

**Verification:** `(manual)` Validate envelope fields after `snapshot`.

**Out of scope:** `visionUsed` metrics (Phase 3).

---

## Phase 3 extensions

### TR-P3-MOD-UiaSnapshotBuilder-005 - Sparse tree vision hook

**Priority:** Must (Phase 3)  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [tools.md#snapshot](../../tools.md#snapshot), [phases.md#phase-3--vision-fallback](../../phases.md#phase-3--vision-fallback)

**Requirement:** When UIA tree sparse/empty or `useVision: true`, builder SHALL invoke VisionFusion and merge per data-model CV fusion rules.

**Acceptance criteria:**
- [ ] Response may include visionUsed, uiaNodeCount, cvNodeCount, fusedNodeCount.

**Verification:** Phase 3 uia-blind demo gate.
