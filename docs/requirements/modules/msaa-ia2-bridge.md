# MSAA / IA2 bridge module requirements (Phase 2)

**Target path:** `src/WinGuiMcp/Accessibility/`

**Spec refs:** [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness), [architecture.md#control-stack-resolution-order](../../architecture.md#control-stack-resolution-order)

---

### TR-P2-MOD-MsaaIa2-001 - Legacy control bridge

**Priority:** Must  
**Depends on:** TR-P1-MOD-UiaSnapshotBuilder-001  
**Spec refs:** [phases.md#phase-2--robustness](../../phases.md#phase-2--robustness)

**Requirement:** Phase 2 SHALL provide MSAA/IAccessible2 bridge adapter when UIA tree is incomplete (legacy WinForms, Firefox-style gaps).

**Acceptance criteria:**
- [ ] find_element resolution order includes MSAA after UIA per architecture.md.
- [ ] errors may include details.backend `"msaa"`.

**Verification:** `(manual)` Legacy sample or Firefox subset if in scope.

**Out of scope:** Phase 1 MSAA-only apps.

---

### TR-P2-MOD-MsaaIa2-002 - UEM node source tagging

**Priority:** Should  
**Depends on:** TR-P2-MOD-MsaaIa2-001  
**Spec refs:** [data-model.md#uem-node-schema](../../data-model.md#uem-node-schema)

**Requirement:** Bridge-sourced nodes SHALL remain in UEM with `source: "uia"` unless spec extended; backend field in errors identifies msaa path.

**Acceptance criteria:**
- [ ] No new agent-facing source enum without data-model update.

**Verification:** Code review against data-model.md.

**Open questions:** Whether MSAA nodes need distinct source tag — defer to spec review if needed.
