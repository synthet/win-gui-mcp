# Deployment requirements (Phase 4)

**Spec refs:** [security.md#deployment-recommendations](../../security.md#deployment-recommendations), [phases.md#phase-4--security-and-hardening](../../phases.md#phase-4--security-and-hardening)

---

### TR-P4-SEC-Deployment-001 - Stdio-only default

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-002  
**Spec refs:** [security.md#threat-model](../../security.md#threat-model)

**Requirement:** MVP/Phase 4 default deployment remains **stdio only** (local). No network MCP without auth design implemented.

**Acceptance criteria:**
- [ ] Deployment guide warns VM/Windows Sandbox for unattended use.

**Verification:** Doc review of security.md + deployment section.

---

### TR-P4-SEC-Deployment-002 - Elevated helper option

**Priority:** Should  
**Depends on:** TR-P4-SEC-Elevation-001  
**Spec refs:** [security.md#integrity-levels-and-uipi](../../security.md#integrity-levels-and-uipi)

**Requirement:** Document separate privileged helper process for elevation-bound ops; main daemon stays user-level.

**Verification:** Deployment guide completeness.

---

### TR-P4-SEC-Deployment-003 - Remote transport auth (design)

**Priority:** Could  
**Depends on:** —  
**Spec refs:** [security.md](../../security.md), [architecture.md#transport](../../architecture.md#transport)

**Requirement:** Future HTTP/SSE transport spec: Bearer + IP allowlist — design documented unless implemented.

**Out of scope:** Phase 4 implementation unless explicitly scoped.

---

### TR-P4-SEC-Deployment-004 - uiAccess manifest

**Priority:** Could  
**Depends on:** —  
**Spec refs:** [security.md#uiaccess-manifest-optional-advanced-path](../../security.md#uiaccess-manifest-optional-advanced-path)

**Requirement:** Document uiAccess=true manifest path for enterprise edge cases.

**Out of scope:** Default shipping configuration.
