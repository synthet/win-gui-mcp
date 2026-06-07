# TR entry template

Copy this block for each requirement. Do not duplicate JSON schemas from canonical specs; link instead.

```markdown
### TR-P{Phase}-{Category}-{Feature}-{Seq} - {Title}

**Priority:** Must | Should | Could  
**Depends on:** TR-P1-MOD-Example-001 (or —)  
**Spec refs:** [tools.md#`{tool}`](../../tools.md#{tool}), [architecture.md](../../architecture.md)

**Requirement:** The server SHALL …

**Acceptance criteria:**
- [ ] Given …, when …, then …
- [ ] …

**Verification:**
1. `dotnet build WinGuiMcp.sln`
2. MCP `tools/call` name=`{tool}`, arguments=`{…}` (or `(manual)` step)

**Out of scope:** …

**Open questions:** (omit if none)
```

## Category codes

| Code | Meaning |
|------|---------|
| `MOD` | Module |
| `TOOL` | MCP tool |
| `GATE` | Acceptance gate step |
| `X` | Cross-cutting |
| `SEC` | Security / deployment |

## Verification conventions

| Type | When | Example |
|------|------|---------|
| Build | All module/tool TRs | `dotnet build WinGuiMcp.sln` |
| Unit test | When test project exists | `dotnet test --filter "FullyQualifiedName~RefTableTests"` |
| MCP invoke | Tool TRs | Document full `tools/call` JSON in the TR |
| Phase gate | Gate TRs | Step N of `/phase-gate` for named app |
| Manual | Until harness exists | Mark `(manual)` and list exact tool sequence |
