---
name: data-config-tools
description: Use when inspecting, querying, validating, or editing JSON, YAML, TOML, CSV, HTTP responses, or local SQLite/config data. Prefer jq/yq/sqlite/curl over raw text search, especially for minified or structured data.
capability: "data-config-tools agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Data and config tools

## Purpose

Structured queries against config files and lightweight API checks.

## When to Use

- Read `package.json`, `docker-compose.yml`, CI YAML keys
- Probe local HTTP endpoints
- Inspect sqlite schemas (read-only)

## Required Tools

`jq`, `yq`, `curl`; optional `dasel`, `sqlite3`, `httpie`.

## Install

Install blocks are shared — See [install-blocks.md](../cli-tools-overview/references/install-blocks.md).

### Windows PowerShell

Use winget blocks from the reference when provisioning a new machine.

### WSL2 Ubuntu

Use apt/curl blocks from the reference; symlink `fdfind` → `fd` if needed.

### macOS

Use Homebrew blocks from the reference.


## Common Commands

```bash
jq '.scripts' package.json
jq -r '.dependencies | keys[]' package.json | head -20
yq '.services' docker-compose.yml
curl -fsS -o /dev/null -w '%{http_code}\n' http://localhost:8080/health
sqlite3 app.db '.schema' | head -40
```

## Agent-Safe Patterns

- Prefer jq/yq over `rg` on JSON/YAML.
- curl: no credentials in URLs; See [bounded-output-patterns.md](../cli-tools-overview/references/bounded-output-patterns.md).

## Commands Requiring Confirmation

POST/PUT/DELETE via curl against production; sqlite writes. See [commands-requiring-confirmation.md](../cli-tools-overview/references/commands-requiring-confirmation.md).

## Troubleshooting

- Invalid JSON: `jq empty < file` to validate.
- yq version differences: note v4 vs mikefarah syntax.

## Windows Notes

- jq via winget; quote JSON paths in PowerShell carefully.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## WSL2 Notes

- Prefer WSL curl against WSL-hosted services.
- See [windows-wsl-split.md](../cli-tools-overview/references/windows-wsl-split.md).

## Verification Checklist

- [ ] `jq --version`, `yq --version`
- [ ] Queries return bounded output
