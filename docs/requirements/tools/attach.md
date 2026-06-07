# attach tool requirements (Phase 2)

Schema: [tools.md#attach](../../tools.md#attach).

---

### TR-P2-TOOL-Attach-001 - Focus plus initial snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FocusWindow-001, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [tools.md#attach](../../tools.md#attach)

**Requirement:** Tool SHALL alias focus_window + initial snapshot. Input: handleOrTitleRegex. Output: `{ handle, snapshotId, tree }`.

**Acceptance criteria:**
- [ ] Single call yields warm session for find_element.

**Verification:** `(manual)` attach to Notepad by title regex.

**Out of scope:** Persistent attach across sessions.
