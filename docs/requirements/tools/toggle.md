# toggle tool requirements (Phase 2)

Schema: [tools.md#toggle](../../tools.md#toggle).

---

### TR-P2-TOOL-Toggle-001 - TogglePattern actuation

**Priority:** Must  
**Depends on:** TR-P1-MOD-PatternExecutor-004  
**Spec refs:** [tools.md#toggle](../../tools.md#toggle)

**Requirement:** Tool SHALL accept `{ ref }` and invoke TogglePattern.

**Acceptance criteria:**
- [ ] PatternNotSupported when unavailable.
- [ ] Typed errors per errors.md.

**Verification:** `(manual)` Settings toggle via toggle tool.

**Out of scope:** Phase 1 checkbox click path remains valid.
