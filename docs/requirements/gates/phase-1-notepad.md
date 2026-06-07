# Phase 1 gate - Notepad

Source: [phases.md#notepad](../../phases.md#notepad).

**Pass rule:** Ref-based actions only; no `click.point`.

| Step | Pass | Notes |
|------|------|-------|

---

### TR-P1-GATE-Notepad-001 - Launch and snapshot

**Priority:** Must  
**Depends on:** TR-P1-TOOL-LaunchApp-001, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `launch_app` `{ path: "notepad.exe" }` then `snapshot`.

**Verification:** Gate step 1.

---

### TR-P1-GATE-Notepad-002 - Type text

**Priority:** Must  
**Depends on:** TR-P1-TOOL-TypeText-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `type_text` `{ text: "Hello WinGuiMcp" }`.

**Acceptance criteria:**
- [ ] Text appears in document area (verify via snapshot).

**Verification:** Gate step 2.

---

### TR-P1-GATE-Notepad-003 - Save hotkey

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Hotkey-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `hotkey` `{ keys: "ctrl+s" }`.

**Verification:** Gate step 3.

---

### TR-P1-GATE-Notepad-004 - Wait for Save dialog

**Priority:** Must  
**Depends on:** TR-P1-TOOL-WaitFor-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `wait_for` `{ condition: "element-exists", target: "Save As" }` (or dialog title match).

**Verification:** Gate step 4.

---

### TR-P1-GATE-Notepad-005 - Filename and Save click

**Priority:** Must  
**Depends on:** TR-P1-TOOL-FindElement-001, TR-P1-TOOL-SetValue-001, TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `find_element` / `set_value` filename field; `click` Save button via ref.

**Verification:** Gate step 5.

---

### TR-P1-GATE-Notepad-006 - Verify saved

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#notepad](../../phases.md#notepad)

**Requirement:** `snapshot` verify title or content reflects save.

**Verification:** Gate step 6.

**Out of scope:** Overwrite confirmation handling beyond wait_for.
