# screenshot tool requirements

Schema: [tools.md#screenshot](../../tools.md#screenshot).

**Target path:** `src/WinGuiMcp/Tools/ScreenshotTool.cs`

---

### TR-P1-TOOL-Screenshot-001 - PNG base64 output

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowCapture-001, TR-P1-MOD-WindowCapture-002  
**Spec refs:** [tools.md#screenshot](../../tools.md#screenshot)

**Requirement:** Tool SHALL return imageBase64, mimeType image/png, width, height. WGC primary, BitBlt fallback.

**Acceptance criteria:**
- [ ] Window handle capture matches visible window bounds.
- [ ] Omit handle for full screen per spec.

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
