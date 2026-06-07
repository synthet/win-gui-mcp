# Error envelope requirements

Cross-cutting error handling for all MCP tool responses. Canonical schemas: [errors.md](../../errors.md).

## Scope

| Phase | Coverage |
|-------|----------|
| 1 | Failed calls return structured envelope; no required `_warning` emission |
| 2 | Post-action `_warning` on success; optional `details.backend`, `traceId` |

---

### TR-P1-X-ErrorEnvelope-001 - Failed tool response shape

**Priority:** Must  
**Depends on:** TR-P1-MOD-McpHost-001  
**Spec refs:** [errors.md#error-envelope](../../errors.md#error-envelope)

**Requirement:** Every failed tool call SHALL return JSON with `ok: false`, `code`, `message`, and optional `details` matching the error catalog. Codes MUST be from the catalog; no free-text-only failures.

**Acceptance criteria:**
- [ ] Invalid input returns `InvalidArgument` with actionable message.
- [ ] Recoverable failures use catalog severity and recovery hints from errors.md.
- [ ] MCP tool error content is parseable JSON (not unstructured prose only).

**Verification:**
1. `dotnet build WinGuiMcp.sln`
2. `(manual)` Call a tool with invalid params; assert `ok: false` and known `code`.

**Out of scope:** HTTP transport error mapping (Phase 4 design).

---

### TR-P1-X-ErrorEnvelope-002 - Per-tool error mapping

**Priority:** Must  
**Depends on:** TR-P1-X-ErrorEnvelope-001  
**Spec refs:** [errors.md#per-tool-error-mapping](../../errors.md#per-tool-error-mapping)

**Requirement:** Each Phase 1 tool handler SHALL map domain failures to the codes listed for that tool in errors.md (e.g. `snapshot` → `WindowGone`, `CaptureFailed`, `UIPIBlocked`).

**Acceptance criteria:**
- [ ] Stale ref on action tools returns `ElementNotFound` (see session-invalidation TRs).
- [ ] Closed window returns `WindowGone` where applicable.
- [ ] Pattern missing on `set_value` returns `PatternNotSupported`.

**Verification:**
1. `(manual)` Per-tool negative cases documented in each `tools/{name}.md` file.

**Out of scope:** Phase 2 `details.candidates` on `Ambiguous` (specified; enrich in P2).

---

### TR-P2-X-ErrorEnvelope-003 - Post-action warning emission

**Priority:** Must (Phase 2)  
**Depends on:** TR-P1-X-ErrorEnvelope-001  
**Spec refs:** [errors.md#post-action-warnings](../../errors.md#post-action-warnings)

**Requirement:** Starting Phase 2, successful action responses MAY include `_warning` when UI state is unchanged within 500 ms or when SendInput fallback was used, per the warning table in errors.md. Warnings MUST NOT set `ok: false`.

**Acceptance criteria:**
- [ ] Phase 1 builds pass without emitting warnings.
- [ ] Phase 2: unchanged UI after click can include documented `_warning` text.

**Verification:**
1. Phase 2: `(manual)` click with no UI delta; inspect response for `_warning`.

**Out of scope:** Phase 1 MVP warning emission (contract exists; implementation deferred).
