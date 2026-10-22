# Agent environment — how Cursor uses CLI tools

Cursor does **not** maintain a separate registry of shell CLIs. Agents discover tools the same way your shell does: **if the binary is on `PATH` in the terminal session the agent uses, it can run it.**

## After installing tools

1. **Quit Cursor completely** (not just close the window).
2. Reopen the project.
3. Open a **new** integrated terminal (`Terminal → New Terminal`).
4. Run the smoke test below.

Existing terminals and agent shells may still see the old PATH until restart.

## Tool kinds

| Kind | Examples | Requirement |
|------|----------|-------------|
| **Shell CLI** | `fd`, `rg`, `bat`, `jq`, `yq`, `just`, `ruff`, `sg` | On PATH in agent shell |
| **Project MCP** | optional `fff-mcp` | [`.cursor/mcp.json`](../../../../.cursor/mcp.example.json) copied from template |
| **User MCP** | `github`, `subagent-orchestrator` | `~/.cursor/mcp.json` — **not** fff |
| **IDE built-in** | Grep, SemanticSearch, Glob | Always available in Cursor; no install |
| **Project-local** | `eslint`, `pytest` in subpackages | `npm install` / `uv sync` in that package |

Shell tools (`fd`, `bat`, etc.) do **not** need MCP entries.

## Smoke test (PowerShell, post-restart)

```powershell
fd --version
bat --version
sg --version
ruff --version
yq --version
just --version
```

If these work in the integrated terminal, the agent can use them too.

## Agent chat smoke

In a **new Agent chat**, ask:

> Run `fd --version`, `sg --version`, and `ruff --version` in the project root.

If the agent returns versions (not "command not found"), the environment is wired correctly.

## Windows gotchas

### `ast-grep` / `sg` and PowerShell execution policy

npm global install may resolve as `sg.ps1`. If the agent gets "running scripts is disabled":

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Or use `sg.cmd --version` if npm created a cmd shim.

### Standalone `rg` vs Cursor-bundled ripgrep

Winget installs ripgrep on user PATH. Cursor also bundles `rg` for IDE search. Both are fine; the **agent shell** uses whichever comes first on PATH.

### Windows vs WSL PATH

Winget installs on Windows are **not** visible inside WSL. If the agent runs in WSL:

- Use WSL install blocks from [install-blocks.md](install-blocks.md)
- Keep heavy repos under `~/src` when possible — see [windows-wsl-split.md](windows-wsl-split.md)

## fff MCP (opt-in, project-level)

**fff** is optional. When enabled, configure in **project** `.cursor/mcp.json` with `cwd` set to the git repo — user-level config fails for repo-scoped indexing.

See [AGENTS.md](../../../../AGENTS.md) and the `fff-mcp` entry in [`.cursor/mcp.example.json`](../../../../.cursor/mcp.example.json).

After adding fff:

1. Reload MCP in Cursor Settings.
2. Confirm `fff-mcp` shows connected.
3. Prefer `ffgrep` / `fffind` for repeated search; use `rg`/`fd` for one-off probes.

## Optional: log provisioning

Log that CLI tools were provisioned for future agent sessions:

```bash
python scripts/agent-memory/log_session.py \
  --summary "CLI tools installed per install-tiers + agent-environment" \
  --outcome "fd, bat, sg, ruff, yq, just on PATH; Cursor restarted" \
  --candidate "After winget CLI installs restart Cursor so agent shells inherit PATH|working_rule|high"
```

This does not change PATH — it helps future agent sessions know the environment was set up.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `command not found` in agent but works in old terminal | Restart Cursor; open new terminal |
| Tool works in PowerShell but not WSL | Install separately in WSL |
| MCP tools missing | Check `.cursor/mcp.json`; reload MCP in Settings |
| fff errors about home/root directory | Move fff to **project** mcp.json with repo `cwd` |

Install order reference: [install-tiers.md](install-tiers.md).
