# click tool requirements

Schema: [tools.md#click](../../tools.md#click).

**Target path:** `src/WinGuiMcp/Tools/ClickTool.cs`

---

### TR-P1-TOOL-Click-001 - Ref click actuation order

**Priority:** Must  
**Depends on:** TR-P1-MOD-PatternExecutor-001, TR-P1-MOD-PatternExecutor-002, TR-P1-MOD-InputSimulator-002  
**Spec refs:** [tools.md#click](../../tools.md#click)

**Requirement:** Ref path SHALL try InvokePattern, then TogglePattern for checkboxes, then SendInput at bbox center. Response includes `method`: `InvokePattern`, `TogglePattern`, or `SendInput`.

**Acceptance criteria:**
- [ ] Never uses mouse_event.
- [ ] ElementNotEnabled, VisibilityMismatch, PatternNotSupported paths per errors.md.

**Verification:** `(manual)` Calculator digit click; inspect method field.

**Out of scope:** CV-sourced refs (Phase 3 SendInput only).

---

### TR-P1-TOOL-Click-002 - includeSnapshot contract

**Priority:** Must  
**Depends on:** TR-P1-TOOL-Click-001, TR-P1-TOOL-Snapshot-001  
**Spec refs:** [tools.md#includesnapshot-on-action-tools](../../tools.md#includesnapshot-on-action-tools)

**Requirement:** When `includeSnapshot: true`, response SHALL include full `snapshotDelta` per tools.md (invalidates prior refs for window).

**Acceptance criteria:**
- [ ] snapshotDelta contains snapshotId, handle, title, processName, timestamp, tree.

**Verification:** MCP click with includeSnapshot true.

**Out of scope:** Required `_warning` on unchanged UI (Phase 2).

---

### TR-P1-TOOL-Click-003 - Point click debug path

**Priority:** Must  
**Depends on:** TR-P1-MOD-InputSimulator-004  
**Spec refs:** [tools.md#click](../../tools.md#click)

**Requirement:** `point` path SHALL use SendInput only (oneOf with ref). Gate flows MUST NOT use point-only clicks for pass.

**Acceptance criteria:**
- [ ] ref and point mutually exclusive per oneOf schema.

**Verification:** `(manual)` point click; gate docs forbid for acceptance.

**Out of scope:** Gate pass via coordinates.
