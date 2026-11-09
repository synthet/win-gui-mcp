# screenshot tool requirements

Schema: [tools.md#screenshot](../../tools.md#screenshot).

**Target path:** `src/WinGuiMcp/Tools/ScreenshotTool.cs`

---

### TR-P1-TOOL-Screenshot-001 - PNG image content output

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowCapture-001, TR-P1-MOD-WindowCapture-002  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** Tool SHALL return the PNG as an MCP `image` content block, plus JSON metadata (`mimeType`, `width`, `height`, `origin`, `scale`) in a text block and `structuredContent`. The JSON SHALL NOT embed the image as base64. WGC primary, PrintWindow / BitBlt fallback.

**Acceptance criteria:**
- [ ] Window handle capture matches visible window bounds.
- [ ] Omit handle for full screen per spec.
- [ ] MCP result content contains exactly one `image` block; text and structured content contain no `imageBase64`.

**Verification:** MCP `tools/call` name=`screenshot`, arguments=`{"handle":<hwnd>}`

**Out of scope:** Primary navigation via screenshots; vision pipeline.

---

### TR-P1-TOOL-Screenshot-002 - scale and region

**Priority:** Should  
**Depends on:** TR-P1-TOOL-Screenshot-001  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** Tool SHALL support optional `scale` (0.1–1.0) and `region` crop.

**Acceptance criteria:**
- [ ] scale reduces output dimensions appropriately.

**Verification:** `(manual)` screenshot with scale 0.5.

**Out of scope:** DXGI capture (Phase 2).

---

### TR-P1-TOOL-Screenshot-003 - Errors

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Screenshot-001  
**Spec refs:** [errors.md#per-tool-error-mapping](../../errors.md#per-tool-error-mapping)

**Requirement:** Tool SHALL return `WindowGone`, `CaptureFailed` as documented.

**Acceptance criteria:**
- [ ] Occluded or invalid window fails structured, no hang.

**Verification:** `(manual)` closed window screenshot.

**Out of scope:** Secure desktop capture.

---

### TR-P1-TOOL-Screenshot-004 - Coordinate mapping metadata

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Screenshot-001  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** Tool SHALL report `origin` (physical screen coordinates of the image's top-left pixel, after region crop) and `scale` (image pixels per physical pixel), so that `x = origin.x + round(px / scale)` addresses the same screen point for `click`.

**Acceptance criteria:**
- [ ] Full-screen capture reports `origin` `(0, 0)`, offset by any region crop.
- [ ] `scale` equals output width divided by pre-scale width.

**Verification:** unit test of origin/scale arithmetic; `(manual)` click at a mapped point.

**Out of scope:** Multi-monitor virtual-screen capture.
