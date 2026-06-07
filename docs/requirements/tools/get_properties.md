# get_properties tool requirements

Schema: [tools.md#get_properties](../../tools.md#get_properties).

**Target path:** `src/WinGuiMcp/Tools/GetPropertiesTool.cs`

---

### TR-P1-TOOL-GetProperties-001 - Full UEM node by ref

**Priority:** Must  
**Depends on:** TR-P1-MOD-RefTable-002  
**Spec refs:** [tools.md#get_properties](../../tools.md#get_properties), [data-model.md#uem-node-schema](../../data-model.md#uem-node-schema)

**Requirement:** Tool SHALL return full UEM node for valid non-stale `ref` matching pattern `^w\d+e\d+$`.

**Acceptance criteria:**
- [ ] Output matches UEM schema; no bbox.
- [ ] Invalid pattern → `InvalidArgument` or `ElementNotFound` per validation layer.

**Verification:** MCP `tools/call` name=`get_properties`, arguments=`{"ref":"w1e1"}`

**Out of scope:** Exposing server-only confidence (Phase 2 CV).

---

### TR-P1-TOOL-GetProperties-002 - Stale and unknown refs

**Priority:** Must  
**Depends on:** TR-P1-TOOL-GetProperties-001  
**Spec refs:** [errors.md#stale-ref-detection](../../errors.md#stale-ref-detection)

**Requirement:** Unknown ref → `ElementNotFound`; stale ref → `ElementNotFound` with `details.stale: true`.

**Acceptance criteria:**
- [ ] Same stale semantics as action tools.

**Verification:** Re-snapshot then get_properties with old ref.

**Out of scope:** Live re-query of UIA without snapshot.
