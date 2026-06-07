# CV licensing requirements (Phase 3)

**Spec refs:** [security.md#computer-vision-licensing-phase-3](../../security.md#computer-vision-licensing-phase-3), [phases.md#licensing-decision-required](../../phases.md#licensing-decision-required)

---

### TR-P3-SEC-CvLicensing-001 - AGPL decision gate

**Priority:** Must (before Phase 3 ship)  
**Depends on:** TR-P3-MOD-VisionFusion-001  
**Spec refs:** [phases.md#licensing-decision-required](../../phases.md#licensing-decision-required)

**Requirement:** Project SHALL resolve OmniParser icon_detect AGPL vs permissive retrain/replacement before bundling CV models in release builds.

**Acceptance criteria:**
- [ ] Chosen path documented in release notes.
- [ ] Permissive build excludes AGPL weights or ships optional plugin with notice.

**Verification:** Legal/release checklist review.

**Out of scope:** Phase 1-2 distribution.

---

### TR-P3-SEC-CvLicensing-002 - Permissible components

**Priority:** Must  
**Depends on:** TR-P3-SEC-CvLicensing-001  
**Spec refs:** [security.md#computer-vision-licensing-phase-3](../../security.md#computer-vision-licensing-phase-3)

**Requirement:** Document license impact: Florence-2 MIT OK; RapidOCR Apache-2.0 OK; Windows.Media.Ocr OS OK; YOLO icon_detect AGPL blocked for default commercial bundling.

**Verification:** security.md alignment audit.

**Out of scope:** Model training pipeline.
