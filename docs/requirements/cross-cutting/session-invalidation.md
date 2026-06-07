# Session invalidation requirements

Ref lifecycle and snapshot invalidation. Canonical rules: [data-model.md#session-invalidation](../../data-model.md#session-invalidation), [architecture.md#session-model](../../architecture.md#session-model).

---

### TR-P1-X-SessionInvalidation-001 - Per-window snapshot invalidation

**Priority:** Must  
**Depends on:** TR-P1-MOD-SnapshotSession-001, TR-P1-MOD-RefTable-001  
**Spec refs:** [data-model.md#session-invalidation](../../data-model.md#session-invalidation), [architecture.md#invalidation-rules](../../architecture.md#invalidation-rules)

**Requirement:** When `snapshot(handle)` completes for a window index, the server SHALL assign a new `snapshotId` for that index and invalidate all prior refs for that index. Refs for other window indices remain valid until their own snapshot or session reset.

**Acceptance criteria:**
- [ ] Second `snapshot` on same handle yields different `snapshotId`.
- [ ] Ref from prior snapshot fails lookup on action tools.
- [ ] `includeSnapshot: true` on action tools triggers same invalidation as explicit `snapshot`.

**Verification:**
1. `(manual)` MCP: `snapshot` → note ref → `snapshot` again → `get_properties` with old ref.

**Out of scope:** Diff-only snapshots (Phase 2 optimization).

---

### TR-P1-X-SessionInvalidation-002 - Stale ref error shape

**Priority:** Must  
**Depends on:** TR-P1-X-SessionInvalidation-001  
**Spec refs:** [errors.md#stale-ref-detection](../../errors.md#stale-ref-detection)

**Requirement:** When a ref's `snapshotId` does not match the current snapshot for its window, ref-consuming tools SHALL return `ElementNotFound` with `details.stale: true`, `details.ref`, and snapshot id fields as specified in errors.md.

**Acceptance criteria:**
- [ ] Message instructs agent to take a new snapshot.
- [ ] `details.stale` is boolean `true`, not omitted.
- [ ] `get_properties`, `click`, `type_text`, `set_value` all honor stale detection.

**Verification:**
1. `(manual)` After re-snapshot, call `click` with stale ref; assert JSON matches stale-ref example in errors.md.

**Out of scope:** Auto-refresh refs without new snapshot.

---

### TR-P1-X-SessionInvalidation-003 - Window closed and idle timeout

**Priority:** Must  
**Depends on:** TR-P1-MOD-SnapshotSession-001  
**Spec refs:** [architecture.md#invalidation-rules](../../architecture.md#invalidation-rules)

**Requirement:** When the target HWND is invalid or the window closes, the server SHALL purge refs for that window and return `WindowGone` on subsequent handle-based calls. After 30 minutes idle (default), the entire session SHALL be cleared.

**Acceptance criteria:**
- [ ] Closed window: `snapshot` / `focus_window` return `WindowGone`.
- [ ] New MCP connection starts a fresh session with empty ref table.

**Verification:**
1. `(manual)` Close automated window; retry `snapshot` with same handle.

**Out of scope:** Configurable idle timeout UI (implementation may use env var later).
