# scroll tool requirements (Phase 2)

Schema: [tools.md#scroll](../../tools.md#scroll).

---

### TR-P2-TOOL-Scroll-001 - ScrollPattern and wheel fallback

**Priority:** Must  
**Depends on:** TR-P2-MOD-PatternExecutor-004, TR-P1-MOD-InputSimulator-001  
**Spec refs:** [tools.md#scroll](../../tools.md#scroll)

**Requirement:** Tool SHALL accept ref or point, direction (`up`|`down`|`left`|`right`), amount; prefer ScrollPattern else mouse wheel SendInput.

**Acceptance criteria:**
- [ ] Scrollable regions in Explorer or long lists reachable.

**Verification:** `(manual)` scroll in list view.

**Out of scope:** Phase 1 (tool not registered).
