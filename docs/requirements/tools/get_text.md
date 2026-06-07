# get_text tool requirements (Phase 2)

Schema: [tools.md#get_text](../../tools.md#get_text).

---

### TR-P2-TOOL-GetText-001 - TextPattern and ValuePattern

**Priority:** Must  
**Depends on:** TR-P2-MOD-PatternExecutor-004  
**Spec refs:** [tools.md#get_text](../../tools.md#get_text)

**Requirement:** Phase 2 tool SHALL read text via TextPattern or ValuePattern for `{ ref }`.

**Acceptance criteria:**
- [ ] Document text readable without full content snapshot when possible.

**Verification:** `(manual)` get_text on Edit control.

**Out of scope:** OCR fallback until Phase 3 (see get_text Phase 3 TR in same file after P3 section added).

---

### TR-P3-TOOL-GetText-001 - OCR fallback (Phase 3)

**Priority:** Must (Phase 3)  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [tools.md#get_text](../../tools.md#get_text), [phases.md#phase-3--vision-fallback](../../phases.md#phase-3--vision-fallback)

**Requirement:** When UIA text unavailable, get_text MAY use ROI OCR fallback.

**Acceptance criteria:**
- [ ] CVLowConfidence when below threshold.

**Verification:** Phase 3 blind region test.

**Out of scope:** Phase 2 OCR.
