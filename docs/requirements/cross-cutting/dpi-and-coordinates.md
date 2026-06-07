# DPI and coordinate requirements

Physical pixel semantics and SendInput safety. Canonical: [architecture.md#dpi](../../architecture.md#dpi), [security.md#dpi-and-coordinate-safety](../../security.md#dpi-and-coordinate-safety).

---

### TR-P1-X-Dpi-001 - Per-Monitor-V2 awareness

**Priority:** Must  
**Depends on:** TR-P1-MOD-DpiManager-001  
**Spec refs:** [stack.md](../../stack.md), [architecture.md#dpi](../../architecture.md#dpi)

**Requirement:** The application SHALL declare Per-Monitor-V2 DPI awareness via application manifest. All RefTable bboxes SHALL be stored in physical pixels.

**Acceptance criteria:**
- [ ] Manifest present in `WinGuiMcp.csproj` or app manifest file.
- [ ] Mixed-DPI manual test: snapshot + ref click on secondary monitor does not systematically miss control.

**Verification:**
1. `dotnet build WinGuiMcp.sln`
2. `(manual)` Mixed-DPI two-monitor click test on Calculator.

**Out of scope:** Per-monitor DPI override APIs for agents.

---

### TR-P1-X-Dpi-002 - Bbox validation before SendInput

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-001, TR-P1-MOD-RefTable-001  
**Spec refs:** [security.md#dpi-and-coordinate-safety](../../security.md#dpi-and-coordinate-safety)

**Requirement:** Before SendInput fallback click, the server SHALL validate the target bbox lies within captured frame bounds when capture is available. If UIA bbox vs capture disagree by **> 2 px**, the server SHALL log reconciliation offset.

**Acceptance criteria:**
- [ ] Out-of-frame bbox does not silently click arbitrary coordinates.
- [ ] UIA pattern path preferred when available (no SendInput solely for DPI convenience).

**Verification:** `(manual)` Inspect logs on SendInput fallback path.

**Out of scope:** Automatic DPI correction without logging.

---

### TR-P1-X-Dpi-003 - Agent JSON omits bboxes

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-001  
**Spec refs:** [data-model.md#design-principles](../../data-model.md#design-principles)

**Requirement:** UEM nodes and tool success responses exposed to agents SHALL NOT include bounding boxes or raw HWNDs in JSON. Coordinates exist only in server-side RefTable.

**Acceptance criteria:**
- [ ] `snapshot`, `find_element`, `get_properties` outputs contain no `bbox` field.
- [ ] `click.point` accepts coordinates in tool input only (debug escape hatch).

**Verification:** `(manual)` Inspect Calculator snapshot JSON for bbox absence.

**Out of scope:** Phase 2 `inspect_at` (coordinate-based debug tool).
