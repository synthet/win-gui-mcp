# Explicit setup synchronization

Run these commands from a maintained checkout of synthet-code-framework with Python 3.11+; use the locally configured Python runner on Windows. The installer itself is stdlib-only.

```sh
python scripts/agent_setup.py inventory --projects-root <projects> --user-root <profile>
python scripts/agent_setup.py plan --projects-root <projects> --user-root <profile> --project <name> --out .agent/local/setup/pilot.json
python scripts/agent_setup.py apply --projects-root <projects> --user-root <profile> --input .agent/local/setup/pilot.json
python scripts/agent_setup.py check --input <receipt.json>
python scripts/agent_setup.py rollback --input <receipt.json> --projects-root <projects> --user-root <profile>
```

Omit --project to include eligible maintained roots. --scope global or projects limits the plan. Inventory includes upstream checkouts and archives; installation excludes them, linked aliases and duplicate worktrees. Remotes identify synthet-owned roots; local source/config evidence identifies projects without remotes. Review eligibility before apply.

Plans contain exact content and before/after hashes. Receipts, baselines and backups live under ignored .agent/local/setup and may contain existing private configuration. Never commit or export them. Changed-after-plan destinations and unmanaged conflicts are preserved. Rollback restores only unchanged installer writes and verifies backup hashes.

Claude uses .claude assets and CLAUDE.md; Codex uses .agents/skills, .codex/agents and AGENTS.md; Cursor discovers shared skills and uses .cursor rules/commands/agents; Antigravity uses .agents/skills/rules/agents and native mcp_config.json. User deployments additionally use .gemini/config for Antigravity. Existing legacy installations are retained.

The library manifest selects core and specialist skills. Project-native skills take precedence in packaging. MCP adapters add missing validated registrations, never replace a conflicting user entry or copy literal credentials. Missing executables, paths, disabled sources and unsupported auth are diagnostics, not successful activation.

Use the local receipt and subsequent plan as evidence; file installation alone does not prove a remote MCP is healthy or an IDE has reloaded it.
