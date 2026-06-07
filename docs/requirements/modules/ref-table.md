# Ref table module requirements

Server-side ref resolution. **Target path:** `src/WinGuiMcp/Session/RefTable.cs`

**Spec refs:** [data-model.md#internal-session-table-server-only](../../data-model.md#internal-session-table-server-only)

---

### TR-P1-MOD-RefTable-001 - Ref storage fields

**Priority:** Must  
**Depends on:** TR-P1-MOD-SnapshotSession-001  
**Spec refs:** [data-model.md#internal-session-table-server-only](../../data-model.md#internal-session-table-server-only)

**Requirement:** For each ref, RefTable SHALL store: `hwnd`, opaque automation element reference, `bbox` (physical px), `source` (`uia` in P1), `patterns[]`, and `snapshotId`.

**Acceptance criteria:**
- [ ] Ref format matches `^w\d+e\d+$`.
- [ ] Bbox never serialized in agent-facing tool output.

**Verification:** `(manual)` Snapshot + internal debug hook or unit test on RefTable.

**Out of scope:** CV `source: cv` until Phase 3.

---

### TR-P1-MOD-RefTable-002 - Ref resolution API

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-001  
**Spec refs:** [tools.md#get_properties](../../tools.md#get_properties)

**Requirement:** RefTable SHALL provide lookup by ref string returning element metadata or failure for unknown/stale refs per TR-P1-X-SessionInvalidation-002.

**Acceptance criteria:**
- [ ] Unknown ref → `ElementNotFound` without `stale: true`.
- [ ] Stale ref → `ElementNotFound` with `details.stale: true`.

**Verification:** `(manual)` Stale ref scenario after re-snapshot.

**Out of scope:** Fuzzy ref matching.

---

### TR-P1-MOD-RefTable-003 - In-memory find index

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-001, TR-P1-MOD-UiaSnapshotBuilder-001  
**Spec refs:** [tools.md#find_element](../../tools.md#find_element)

**Requirement:** RefTable (or companion index) SHALL support querying latest snapshot nodes by `name`, `role`, `automationId`, `className`, and `text` with `exact` / `contains` / `regex` match and `nth` disambiguation.

**Acceptance criteria:**
- [ ] Query runs against latest snapshot for given handle only.
- [ ] Multiple matches without valid `nth` → `Ambiguous` at tool layer.

**Verification:** `(manual)` `find_element` on Calculator digit buttons.

**Out of scope:** MSAA/IA2 index (Phase 2).
