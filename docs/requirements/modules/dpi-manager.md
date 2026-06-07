# DPI manager module requirements

**Target path:** `src/WinGuiMcp/Platform/DpiManager.cs`

**Spec refs:** [architecture.md#dpi](../../architecture.md#dpi), [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

---

### TR-P1-MOD-DpiManager-001 - Per-Monitor-V2 manifest

**Priority:** Must  
**Depends on:** —  
**Spec refs:** [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

**Requirement:** Application manifest SHALL enable **Per-Monitor-V2** DPI awareness. DpiManager SHALL expose conversion helpers if needed for capture vs UIA reconciliation.

**Acceptance criteria:**
- [ ] Manifest committed and applied at build.
- [ ] RefTable stores physical pixel bboxes.

**Verification:** `dotnet build WinGuiMcp.sln`; inspect manifest in output.

**Out of scope:** uiAccess manifest (Phase 4 doc).

---

### TR-P1-MOD-DpiManager-002 - Bbox physical pixel contract

**Priority:** Must  
**Depends on:** TR-P1-MOD-DpiManager-001  
**Spec refs:** [data-model.md#internal-session-table-server-only](../../data-model.md#internal-session-table-server-only)

**Requirement:** All bounding rectangles from UIA SHALL be normalized to physical pixels before storage in RefTable.

**Acceptance criteria:**
- [ ] SendInput click center matches visible control on 125%/150% scaling in manual test.

**Verification:** `(manual)` TR-P1-X-Dpi-001 mixed scaling test.

**Out of scope:** Agent-visible DPI metadata in JSON.
