# Vision fusion module requirements (Phase 3)

**Target path:** `src/WinGuiMcp/Perception/VisionFusion.cs`

**Spec refs:** [architecture.md#cv-path-phase-3](../../architecture.md#cv-path-phase-3), [data-model.md#cv-fusion-phase-3](../../data-model.md#cv-fusion-phase-3)

---

### TR-P3-MOD-VisionFusion-001 - Three-tier perception pipeline

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowCapture-001  
**Spec refs:** [architecture.md#three-tier-perception-phase-13](../../architecture.md#three-tier-perception-phase-13)

**Requirement:** Phase 3 SHALL implement tier 2 ROI OCR and tier 3 full CV (WGC capture, downscale ~1024px, detector + captioner + OCR) when UIA insufficient.

**Acceptance criteria:**
- [ ] ROI-first before full-screen parse.

**Verification:** Phase 3 latency documentation.

**Out of scope:** Phase 1-2 UIA-only paths.

---

### TR-P3-MOD-VisionFusion-002 - IoU deduplication

**Priority:** Must  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [data-model.md#cv-fusion-phase-3](../../data-model.md#cv-fusion-phase-3)

**Requirement:** Discard CV nodes with IoU **> 10%** overlap with any UIA node (UIA wins).

**Acceptance criteria:**
- [ ] fusedNodeCount logged in snapshot when vision used.

**Verification:** Phase 3 fusion metrics in gate output.

---

### TR-P3-MOD-VisionFusion-003 - CV ref actuation routing

**Priority:** Must  
**Depends on:** TR-P3-MOD-VisionFusion-002, TR-P1-MOD-RefTable-001  
**Spec refs:** [architecture.md#action-layer-puppeteer](../../architecture.md#action-layer-puppeteer)

**Requirement:** RefTable entries with `source: cv` SHALL route click/type to SendInput only (no InvokePattern).

**Acceptance criteria:**
- [ ] click on cv ref reports method SendInput.

**Verification:** UIA-blind demo gate.

---

### TR-P3-MOD-VisionFusion-004 - OCR engines

**Priority:** Must  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [stack.md#stack-by-layer](../../stack.md#stack-by-layer), [phases.md#phase-3--vision-fallback](../../phases.md#phase-3--vision-fallback)

**Requirement:** Implement Windows.Media.Ocr and/or RapidOCR (ONNX); Tesseract as low-dependency ROI fallback per stack/phases.

**Acceptance criteria:**
- [ ] CVLowConfidence when detection below threshold.

**Verification:** Phase 3 OCR crop test.

**Out of scope:** Shipping AGPL YOLO weights without licensing decision (see cv-licensing.md).
