# Window capture module requirements

**Target path:** `src/WinGuiMcp/Capture/WindowCapture.cs`

**Spec refs:** [architecture.md#capture-layer](../../architecture.md#capture-layer)

---

### TR-P1-MOD-WindowCapture-001 - WGC per-window capture

**Priority:** Must  
**Depends on:** TR-P1-MOD-DpiManager-001  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot), [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

**Requirement:** Per-window screenshot SHALL use **Windows.Graphics.Capture** when available.

**Acceptance criteria:**
- [ ] Response includes `imageBase64`, `mimeType: image/png`, `width`, `height`.
- [ ] Optional `scale` downscales per tools.md.

**Verification:** `(manual)` MCP `screenshot` with Calculator handle.

**Out of scope:** CV interpretation of pixels (Phase 3).

---

### TR-P1-MOD-WindowCapture-002 - BitBlt fallback

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowCapture-001  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** When WGC is unavailable, capture SHALL fall back to BitBlt without hanging.

**Acceptance criteria:**
- [ ] Failure after fallbacks → `CaptureFailed`.
- [ ] Full-screen capture works when `handle` omitted.

**Verification:** `(manual)` Screenshot with and without handle.

**Out of scope:** DXGI Desktop Duplication (Phase 2).

---

### TR-P1-MOD-WindowCapture-003 - Region crop

**Priority:** Should  
**Depends on:** TR-P1-MOD-WindowCapture-001  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** Optional `region` `{x,y,width,height}` SHALL crop capture output in physical pixels.

**Acceptance criteria:**
- [ ] Region outside frame rejected or clipped per implementation with no hang.

**Verification:** `(manual)` Partial region screenshot.

**Out of scope:** ROI OCR pipeline (Phase 3).

---

## Phase 2 extensions

### TR-P2-MOD-WindowCapture-004 - DXGI Desktop Duplication

**Priority:** Should (Phase 2)  
**Depends on:** TR-P1-MOD-WindowCapture-002  
**Spec refs:** [architecture.md#capture-layer](../../architecture.md#capture-layer), [stack.md#stack-by-layer](../../stack.md#stack-by-layer)

**Requirement:** Phase 2 SHALL add DXGI capture path for exclusive fullscreen scenarios.

**Verification:** Phase 2 manual game/fullscreen test if available.
