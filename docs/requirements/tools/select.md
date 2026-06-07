# select tool requirements (Phase 2)

Schema: [tools.md#select](../../tools.md#select).

---

### TR-P2-TOOL-Select-001 - SelectionItemPattern

**Priority:** Must  
**Depends on:** TR-P2-MOD-PatternExecutor-004  
**Spec refs:** [tools.md#select](../../tools.md#select)

**Requirement:** Tool SHALL accept `{ ref }` and call SelectionItemPattern.Select().

**Acceptance criteria:**
- [ ] List/combo item selection without coordinate click when pattern exists.

**Verification:** `(manual)` List selection in sample app.

**Out of scope:** Multi-select extended APIs.
