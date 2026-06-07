# inspect_at tool requirements (Phase 2)

Schema: [tools.md#inspect_at](../../tools.md#inspect_at).

---

### TR-P2-TOOL-InspectAt-001 - FromPoint debugging

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-001  
**Spec refs:** [tools.md#inspect_at](../../tools.md#inspect_at)

**Requirement:** Tool SHALL accept `{ x, y }` physical pixels and return element info via AutomationElement.FromPoint for debugging.

**Acceptance criteria:**
- [ ] Not used in gate pass criteria (debug only).

**Verification:** `(manual)` inspect_at on known control center.

**Out of scope:** Agent primary navigation via coordinates.
