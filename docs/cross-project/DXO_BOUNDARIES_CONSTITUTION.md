# Cross-project constitution: WinGuiMcp ↔ DxO PhotoLab

**Status:** Normative for agents working across both repositories.  
**Parties:** [`win-gui-mcp`](https://github.com/synthet/win-gui-mcp) (generic Windows desktop MCP) and [`dxo-photolab-mcp`](https://github.com/synthet/dxo-photolab-mcp) (local checkout `D:\Projects\dxo-photolab-mcp`; DxO PhotoLab workflow + domain MCP).  
**Sibling copy:** [`D:\Projects\dxo-photolab-mcp\docs\win-gui-mcp-boundaries.md`](file:///D:/Projects/dxo-photolab-mcp/docs/win-gui-mcp-boundaries.md) (obligations from the PhotoLab MCP repo).

---

## 1. Purpose

Both projects automate **DxO PhotoLab 10** on Windows, but they solve different layers of the stack. This document prevents duplicated tools, conflicting specs, unsafe automation paths, and documentation drift when agents operate across repos.

**Terminology:** DxO **Camera and Lens Module** is optics calibration data—not the Model Context Protocol. **MCP** in this document always means [Model Context Protocol](https://modelcontextprotocol.io).

---

## 2. Charters (who owns what)

| Concern | **win-gui-mcp** | **dxo** |
|--------|-----------------|---------|
| **Mission** | Playwright-style **generic** Windows UI automation (UIA3, refs, Phase 1 tools) for any desktop app | **PhotoLab-specific** workflow automation: launch, sidecars, presets, exports (MCP + scripts) |
| **MCP server** | `win-gui-mcp` — stdio, `dotnet run` / published host | `dxo-photolab` — `DxoMcpServer.exe`, domain tools only |
| **Tool names** | Only names in [`tools.md`](../tools.md) (`snapshot`, `click`, `set_value`, …) | Only `dxo_*` tools documented in [`mcp-server-guide.md`](file:///D:/Projects/dxo-photolab-mcp/docs/mcp-server-guide.md) |
| **Normative DxO product spec** | Stub + link: [`docs/dxo-photolab/README.md`](../dxo-photolab/README.md) → **canonical in dxo-photolab-mcp** | [`docs/photolab-automation/SPEC.md`](file:///D:/Projects/dxo-photolab-mcp/docs/photolab-automation/SPEC.md) |
| **Implementation plan (UIA)** | Gate tests in `tests/WinGuiMcp.Tests/`; generic guides in [`docs/guides/`](../guides/README.md) | [`docs/photolab-automation/PLAN.md`](file:///D:/Projects/dxo-photolab-mcp/docs/photolab-automation/PLAN.md) |
| **Sidecar / file I/O** | OS waits in `wait_for` (`file-stable`, `process-exists`) — [`tools.md`](../tools.md) | **Owner** of `dxo_*` sidecar tools and `scripts/` |
| **UI recipes (PhotoLab)** | [`docs/guides/integrating-sibling-mcp-servers.md`](../guides/integrating-sibling-mcp-servers.md) | [`docs/gui-automation-win-gui-mcp.md`](file:///D:/Projects/dxo-photolab-mcp/docs/gui-automation-win-gui-mcp.md) |
| **Reverse engineering** | — | Not stored in either public GitHub repo. Merged product notes: `dxo-photolab-mcp/docs/photolab-automation/RESEARCH.md`. |
| **Cross-links** | [`LINKS.json`](LINKS.json) | [`cross-project-LINKS.json`](file:///D:/Projects/dxo-photolab-mcp/docs/cross-project-LINKS.json) |

---

## 3. Layer model (how the two MCP servers compose)

```text
┌─────────────────────────────────────────────────────────────┐
│  AI client (Cursor / Claude Desktop / Codex)                │
└───────────────┬─────────────────────────┬───────────────────┘
                │ stdio MCP                 │ stdio MCP
                ▼                           ▼
     ┌──────────────────────┐    ┌──────────────────────┐
     │  win-gui-mcp         │    │  DxoMcpServer (dxo)   │
     │  Generic UIA layer   │    │  Domain adapter       │
     │  snapshot, click, …  │    │  dxo_open_images, …   │
     └──────────┬───────────┘    └──────────┬───────────┘
                │ UIA3 / FlaUI              │ Process, files, presets
                ▼                           ▼
     ┌──────────────────────────────────────────────────────┐
     │  DxO PhotoLab 10 (DxO.PhotoLab.exe + ProcessingCore) │
     └──────────────────────────────────────────────────────┘
```

**Rules:**

1. **Prefer the lowest layer that is correct and stable.** Batch parameter changes → `dxo` sidecar tools; modal export wizard or undocumented UI → `win-gui-mcp` UIA tools.
2. **Do not merge servers into one binary** without an ADR in `win-gui-mcp` and a matching update to this constitution.
3. **Do not add `dxo_*` tools to `win-gui-mcp`** or Phase 1 tool names to `DxoMcpServer`—keeps schemas, security review, and versioning independent.

---

## 4. Hard boundaries (non‑negotiable)

### 4.1 Shared safety & compliance

Both projects **MUST NOT**:

- Inject DLLs, patch install dirs, or hook PhotoLab / ProcessingCore internals for production automation.
- Rely on undocumented `DopCor` or community batch wrappers as a supported API.
- Automate license bypass, module extraction, or EULA-prohibited service-bureau patterns.
- Commit secrets, real `config.json` with credentials, or user-specific export paths as normative defaults.

Both **MUST** follow [`.agent/SAFETY.md`](../../.agent/SAFETY.md) (win-gui-mcp). Elevated or reverse-engineering work stays on a local lab machine and is not stored in either GitHub repo.

### 4.2 win-gui-mcp MUST NOT

- Implement PhotoLab-only MCP tools (`dxo_*`) in `src/WinGuiMcp/Tools/`.
- Treat `docs/research/*` or deep-research PDFs as normative for tool schemas.
- Own canonical paths for `DxoMcpServer` binaries or `dxo` script contracts.

### 4.3 dxo MUST NOT

- Fork or vendor `WinGuiMcp` source as a substitute for the generic MCP server.
- Invent generic tool names (`snapshot`, `find_element`, …) that duplicate [`tools.md`](../tools.md).
- Publish UIA gate acceptance criteria without aligning with [`docs/photolab-automation/SPEC.md`](file:///D:/Projects/dxo-photolab-mcp/docs/photolab-automation/SPEC.md) scenarios.

---

## 5. Documentation authority (conflict resolution)

When documents disagree:

| Question | Wins |
|----------|------|
| Generic MCP tool shape / errors | `win-gui-mcp` → [`tools.md`](../tools.md), [`errors.md`](../errors.md) |
| PhotoLab functional requirements (UIA acceptance) | `dxo` → [`docs/photolab-automation/SPEC.md`](file:///D:/Projects/dxo-photolab-mcp/docs/photolab-automation/SPEC.md) |
| PhotoLab “what is officially supported” / legal posture | `dxo` → [`docs/synthesis.md`](file:///D:/Projects/dxo-photolab-mcp/docs/synthesis.md) |
| `dxo_*` tool arguments & behavior | `dxo` → [`docs/mcp-server-guide.md`](file:///D:/Projects/dxo-photolab-mcp/docs/mcp-server-guide.md) + `Program.cs` |
| Cross-repo process | This file |

**Research markdown** (Downloads exports, long RE reports) is **input only** until merged into the authority files above.

---

## 6. Typical workflows (which repo to touch)

| Goal | Primary | Secondary |
|------|---------|-----------|
| Click Export, drive export dialog, read UIA tree | win-gui-mcp | dxo (`dxo_list_exports` after render) |
| Open RAWs with `--mode=openwith` | dxo | win-gui-mcp `launch_app` only if matching documented args |
| Set exposure via `.dop` without UI | dxo | win-gui-mcp to refresh UI if needed |
| Backup sidecars before batch | dxo `scripts/` or `dxo_backup_sidecars` | — |
| Phase 1 gate test for PhotoLab | win-gui-mcp tests | SPEC scenarios in `docs/dxo-photolab/` |
| Ghidra / static RE follow-up | Local workstation only (out of repo) | Do not commit RE artifacts to public remotes; fold product-safe notes into `RESEARCH.md` in dxo-photolab-mcp |

---

## 7. MCP client configuration

Register **two** servers (names are convention; paths are machine-local):

| Server key | Command | Repo |
|------------|---------|------|
| `win-gui-mcp` | `dotnet run --project …/WinGuiMcp.csproj` | win-gui-mcp |
| `dxo-photolab` | `D:\Projects\dxo-photolab-mcp\bin\mcp\DxoMcpServer.exe` | dxo-photolab-mcp |

Agents should declare which server they call in logs and prompts. Never assume a single server exposes both `snapshot` and `dxo_tweak_sliders`.

---

## 8. Change management

1. **Contract-first:** Update the authority doc in the **owner** repo before changing behavior (see [`.agent/workflows/cross_repo_contract_change.md`](../../.agent/workflows/cross_repo_contract_change.md)).
2. **DxO SPEC / PLAN changes** → `win-gui-mcp` first; then adjust `dxo` tools/scripts if scenarios reference file-level vs UI-level steps.
3. **New `dxo_*` tool** → `dxo` only; add row to `mcp-server-guide.md`; optional pointer in `docs/photolab-automation/README.md`.
4. **New generic tool or error code** → `win-gui-mcp` only; update `tools.md` / `errors.md`; do not shadow in `dxo`.

---

## 9. Future: application profiles

**Application profiles** (per-target recipes, launch paths, preferred tool routing) are expected to live in **win-gui-mcp** as configuration over the generic tool surface, referencing **dxo** for PhotoLab file/domain operations. Until [`docs/application-profiles.md`](../application-profiles.md) exists, treat this section as intent: profiles **orchestrate**; they do not replace either MCP server.

---

## 10. Amendment

Changes to this constitution require:

- An edit to this file in `win-gui-mcp`.
- A matching update to `dxo-photolab-mcp/docs/win-gui-mcp-boundaries.md` (summary + link).
- A one-line entry in `docs/dxo-photolab/README.md` changelog or `docs/LESSONS_LEARNED.md` when behavior-visible.

---

*Last updated: 2026-10-22*
