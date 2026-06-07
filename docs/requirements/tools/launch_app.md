# launch_app tool requirements

Schema: [tools.md#launch_app](../../tools.md#launch_app).

**Target path:** `src/WinGuiMcp/Tools/LaunchAppTool.cs`

---

### TR-P1-TOOL-LaunchApp-001 - Path launch

**Priority:** Must  
**Depends on:** TR-P1-MOD-WindowEnumerator-001  
**Spec refs:** [tools.md#launch_app](../../tools.md#launch_app)

**Requirement:** Tool SHALL start process via `path` (and optional `args`), poll for new top-level window until `waitForWindowMs` (default 10000), return handle, title, pid.

**Acceptance criteria:**
- [ ] Successful launch returns `ok: true` with valid handle.
- [ ] Timeout → `Timeout` or `LaunchFailed` per errors.md.

**Verification:** MCP `tools/call` name=`launch_app`, arguments=`{"path":"calc.exe"}`

**Out of scope:** UAC elevation of target.

---

### TR-P1-TOOL-LaunchApp-002 - AUMID launch

**Priority:** Should  
**Depends on:** TR-P1-TOOL-LaunchApp-001  
**Spec refs:** [tools.md#launch_app](../../tools.md#launch_app)

**Requirement:** Tool SHALL support `appUserModelId` launch via shell when path not provided (oneOf schema).

**Acceptance criteria:**
- [ ] Store/UWP apps with known AUMID can be launched when specified in gate flows.

**Verification:** `(manual)` AUMID launch if used in gate; else defer to Settings app manual open.

**Out of scope:** Arbitrary URI schemes.
