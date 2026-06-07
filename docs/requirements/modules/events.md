# Events module requirements (Phase 2)

**Target path:** `src/WinGuiMcp/Events/`

**Spec refs:** [architecture.md#auto-wait-and-events-phase-2](../../architecture.md#auto-wait-and-events-phase-2)

---

### TR-P2-MOD-Events-001 - UIA event subscriptions

**Priority:** Must  
**Depends on:** TR-P1-TOOL-WaitFor-001  
**Spec refs:** [architecture.md#auto-wait-and-events-phase-2](../../architecture.md#auto-wait-and-events-phase-2)

**Requirement:** Phase 2 SHALL subscribe to UIA events: StructureChanged, PropertyChanged, FocusChanged for event-driven waits.

**Acceptance criteria:**
- [ ] wait_for uses events before polling fallback.

**Verification:** Phase 2 wait_for slow dialog < 10 s gate.

**Out of scope:** Phase 1 poll-only implementation.

---

### TR-P2-MOD-Events-002 - SetWinEventHook

**Priority:** Must  
**Depends on:** TR-P2-MOD-Events-001  
**Spec refs:** [architecture.md#auto-wait-and-events-phase-2](../../architecture.md#auto-wait-and-events-phase-2)

**Requirement:** Module SHALL use SetWinEventHook for window focus/title changes and stale-element detection support.

**Acceptance criteria:**
- [ ] Focus/title change detected without full tree poll where possible.

**Verification:** `(manual)` wait_for window-active with event path.

**Out of scope:** Global hook injection beyond documented WinEvents.
