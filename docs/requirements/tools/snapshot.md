# snapshot tool requirements

Schema: [tools.md#snapshot](../../tools.md#snapshot). Envelope: [data-model.md#snapshot-response-envelope](../../data-model.md#snapshot-response-envelope).

**Target path:** `src/WinGuiMcp/Tools/SnapshotTool.cs`

---

### TR-P1-TOOL-Snapshot-001 - Pruned UIA tree with refs

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-004, TR-P1-MOD-SnapshotSession-002  
**Spec refs:** [tools.md#snapshot](../../tools.md#snapshot), [data-model.md#tree-pruning-rules](../../data-model.md#tree-pruning-rules)

**Requirement:** Given valid HWND, tool SHALL return snapshot envelope with pruned UEM tree, assign refs depth-first, populate RefTable, invalidate prior refs for that window index.

**Acceptance criteria:**
- [ ] Agent JSON omits bboxes.
- [ ] Re-snapshot returns new snapshotId.
- [ ] Stale ref → TR-P1-X-SessionInvalidation-002.

**Verification:**
1. MCP `tools/call` name=`snapshot`, arguments=`{"handle": <Calculator hwnd>}`
2. Re-snapshot; verify stale ref behavior.

**Out of scope:** `useVision` (Phase 3).

---

### TR-P1-TOOL-Snapshot-002 - Scope and detail parameters

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [tools.md#snapshot](../../tools.md#snapshot), [data-model.md#control-view-vs-content-view](../../data-model.md#control-view-vs-content-view)

**Requirement:** Tool SHALL support `scope` (`window`|`focused-control`), `detail` (`control`|`content`), `includeOffscreen` per tools.md defaults.

**Acceptance criteria:**
- [ ] `detail: content` includes read-only text useful for verification.
- [ ] Offscreen nodes excluded unless includeOffscreen true.

**Verification:** `(manual)` Notepad content vs control snapshots.

**Out of scope:** Subtree-only snapshot optimization.

---

### TR-P1-TOOL-Snapshot-003 - Error paths

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [errors.md#per-tool-error-mapping](../../errors.md#per-tool-error-mapping)

**Requirement:** Tool SHALL map `WindowGone`, `CaptureFailed`, `UIPIBlocked` as listed in errors.md.

**Acceptance criteria:**
- [ ] Closed window handle fails fast with structured error.

**Verification:** `(manual)` Snapshot after closing window.

**Out of scope:** Sparse-tree auto vision trigger (Phase 3).

---

## Phase 3 extensions

### TR-P3-TOOL-Snapshot-004 - useVision parameter

**Priority:** Must (Phase 3)  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [tools.md#snapshot](../../tools.md#snapshot)

**Requirement:** `useVision: true` SHALL enable CV fallback pipeline on snapshot.

**Verification:** Phase 3 gate on UIA-blind window.
