# Snapshot session module requirements

Per-connection session state. **Target path:** `src/WinGuiMcp/Session/SnapshotSession.cs`

**Spec refs:** [architecture.md#session-model](../../architecture.md#session-model)

---

### TR-P1-MOD-SnapshotSession-001 - Session state containers

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-001  
**Spec refs:** [architecture.md#session-model](../../architecture.md#session-model)

**Requirement:** Each MCP connection SHALL maintain one `SnapshotSession` with: `snapshotsByWindow` (windowIndex → latest snapshotId), `windowRegistry` (windowIndex → HWND), `lastActivity`, and a RefTable instance.

**Acceptance criteria:**
- [ ] Window indices are 1-based per data-model ref format (`w1e42`).
- [ ] Session is scoped to single MCP connection lifetime.

**Verification:** `dotnet build WinGuiMcp.sln`; unit test when available.

**Out of scope:** Multi-session persistence to disk.

---

### TR-P1-MOD-SnapshotSession-002 - Per-window snapshot tracking

**Priority:** Must  
**Depends on:** TR-P1-MOD-SnapshotSession-001  
**Spec refs:** [architecture.md#invalidation-rules](../../architecture.md#invalidation-rules)

**Requirement:** On successful snapshot for a handle, the session SHALL update `snapshotsByWindow` for that window's index with a new UUID `snapshotId`.

**Acceptance criteria:**
- [ ] Same HWND re-registered keeps or updates consistent windowIndex mapping.
- [ ] Snapshot for window A does not change snapshotId for window B.

**Verification:** `(manual)` Two-window snapshot sequence; verify independent snapshotIds.

**Out of scope:** Cross-window ref sharing.

---

### TR-P1-MOD-SnapshotSession-003 - Idle timeout

**Priority:** Must  
**Depends on:** TR-P1-MOD-SnapshotSession-001  
**Spec refs:** [data-model.md#session-invalidation](../../data-model.md#session-invalidation)

**Requirement:** After **30 minutes** idle (default), the session SHALL clear all refs and snapshot state.

**Acceptance criteria:**
- [ ] Activity on any tool resets `lastActivity`.
- [ ] Post-timeout ref lookup behaves as unknown ref / stale.

**Verification:** Unit test with injectable clock preferred; else `(manual)` with shortened timeout in dev.

**Out of scope:** Agent-visible timeout warning event.
