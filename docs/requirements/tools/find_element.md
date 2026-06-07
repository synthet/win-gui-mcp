# find_element tool requirements

Schema: [tools.md#find_element](../../tools.md#find_element).

**Target path:** `src/WinGuiMcp/Tools/FindElementTool.cs`

---

### TR-P1-TOOL-FindElement-001 - Locator query

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-003, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [tools.md#find_element](../../tools.md#find_element)

**Requirement:** Tool SHALL locate element by `handle`, `by`, `value` with optional `nth`, `match` (`exact`|`contains`|`regex`).

**Acceptance criteria:**
- [ ] Single match returns ref, role, name.
- [ ] No match → `ElementNotFound`.
- [ ] Multiple matches without disambiguation → `Ambiguous`.

**Verification:** MCP `tools/call` name=`find_element`, arguments=`{"handle":<h>,"by":"name","value":"="}`

**Out of scope:** Phase 2 rich query string; list mode (Phase 2).

---

### TR-P1-TOOL-FindElement-002 - Auto-snapshot when cold

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FindElement-001  
**Spec refs:** [tools.md#find_element](../../tools.md#find_element)

**Requirement:** If no snapshot exists for handle, server SHALL auto-run `snapshot` with defaults before search (same invalidation rules).

**Acceptance criteria:**
- [ ] First find_element on fresh handle triggers snapshot.
- [ ] Subsequent find uses warm index without mandatory re-snapshot.

**Verification:** `(manual)` find_element without prior snapshot.

**Out of scope:** MSAA index (Phase 2).

---

### TR-P1-TOOL-FindElement-003 - Text locator

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FindElement-001  
**Spec refs:** [tools.md#find_element](../../tools.md#find_element)

**Requirement:** `by: text` SHALL match against node `name` or `value` fields in UEM index.

**Acceptance criteria:**
- [ ] Save dialog title find works in Notepad gate.

**Verification:** `/phase-gate` Notepad save dialog step.

**Out of scope:** OCR text match (Phase 3).

---

## Phase 3 extensions

### TR-P3-TOOL-FindElement-004 - Vision selector resolution

**Priority:** Should (Phase 3)  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [tools.md#selector-resolution-phase-2](../../tools.md#selector-resolution-phase-2)

**Requirement:** find_element resolution order SHALL include vision OCR/icon match on window ROI after MSAA per architecture.md.

**Verification:** Phase 3 demo.

**Open questions:** Exact selector syntax — confirm tools.md update before coding.
