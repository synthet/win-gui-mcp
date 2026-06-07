# drag tool requirements (Phase 2)

Schema: [tools.md#drag](../../tools.md#drag).

---

### TR-P2-TOOL-Drag-001 - SendInput drag sequence

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-001  
**Spec refs:** [tools.md#drag](../../tools.md#drag)

**Requirement:** Tool SHALL accept fromRef|fromPoint and toRef|toPoint and perform SendInput drag sequence.

**Acceptance criteria:**
- [ ] Ref paths resolve bbox centers in physical pixels.
- [ ] focus_window semantics before drag.

**Verification:** `(manual)` drag test on sample control.

**Out of scope:** UIA drag pattern (uncommon on Windows).
