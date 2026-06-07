# Phase 1 gate - File Explorer

Source: [phases.md#file-explorer](../../phases.md#file-explorer).

**Pass rule:** Ref-based only; no `click.point`.

---

### TR-P1-GATE-FileExplorer-001 - Open Explorer

**Priority:** Must  
**Depends on:** TR-P1-TOOL-LaunchApp-001 or TR-P1-TOOL-FocusWindow-001  
**Spec refs:** [phases.md#file-explorer](../../phases.md#file-explorer)

**Requirement:** Launch or focus File Explorer; `snapshot`.

**Verification:** Gate step 1.

---

### TR-P1-GATE-FileExplorer-002 - Open folder

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001  
**Spec refs:** [phases.md#file-explorer](../../phases.md#file-explorer)

**Requirement:** `click` folder in tree or list via ref.

**Verification:** Gate step 2.

---

### TR-P1-GATE-FileExplorer-003 - Wait for content

**Priority:** Must  
**Depends on:** TR-P1-TOOL-WaitFor-001  
**Spec refs:** [phases.md#file-explorer](../../phases.md#file-explorer)

**Requirement:** `wait_for` window-active or content update.

**Verification:** Gate step 3.

---

### TR-P1-GATE-FileExplorer-004 - Rename file

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001, TR-P1-TOOL-Hotkey-001, TR-P1-TOOL-TypeText-001  
**Spec refs:** [phases.md#file-explorer](../../phases.md#file-explorer)

**Requirement:** Select file via ref click; `hotkey` F2 or menu Rename; `type_text` or `set_value` new name; confirm.

**Acceptance criteria:**
- [ ] No coordinate-only navigation.

**Verification:** Gate steps 4–5.

---

### TR-P1-GATE-FileExplorer-005 - Verify rename

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Snapshot-001  
**Spec refs:** [phases.md#file-explorer](../../phases.md#file-explorer)

**Requirement:** `snapshot` shows new filename in tree.

**Verification:** Gate step 6.

**Out of scope:** Destructive delete operations.
