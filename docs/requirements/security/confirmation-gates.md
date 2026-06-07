# Confirmation gates requirements (Phase 4)

**Spec refs:** [errors.md#error-codes](../../errors.md#error-codes), [security.md#threat-model](../../security.md#threat-model)

---

### TR-P4-SEC-Confirmation-001 - Destructive action block

**Priority:** Must  
**Depends on:** TR-P4-SEC-Elevation-001  
**Spec refs:** [errors.md#error-codes](../../errors.md#error-codes)

**Requirement:** Phase 4 SHALL block destructive UI patterns (delete confirmations, format drive) with `ConfirmationRequired` until user ack via config/human-in-the-loop hook.

**Acceptance criteria:**
- [ ] Retry with confirmationId after user approval (schema per errors.md when specified).

**Verification:** Supervised destructive pattern test.

**Out of scope:** Phase 1-3 unrestricted destructive ops (operator uses VM/sandbox per security.md).
