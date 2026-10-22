---
description: Safe MCP diagnostics — prefer read-only tools and schema checks
---

# Safe MCP diagnostics

## Purpose

Use project MCP servers for triage **without** accidental writes, config mutation, or unsafe code
execution.

## When to use

- Any agent or operator connects an MCP server that can attach to a live process or database.
- Before any destructive or write-capable action.

## Default posture

- Prefer the compact **`search` + `dispatch`** surface (or explicit read-only tools) over raw
  write-capable tools.
- Read before you write: inspect schema, status, logs, and config **values** first.

## Read-only first (examples — adapt to your servers)

- Health / status / version probes
- Schema or contract inspection
- Log tail / error summaries
- Read-only queries (e.g. `SELECT`-only SQL)

## Treat as HIGH RISK (require explicit user request)

- Arbitrary code execution tools
- Config setters / feature-flag mutation
- Job runners / batch processors / destructive maintenance
- `INSERT` / `UPDATE` / `DELETE` / DDL SQL

## Do not

- Do not run write-capable tools "to see what happens".
- Do not assume an MCP endpoint is isolated — on shared machines it may attach to a live process.
- Do not pass secrets or full config in tool arguments.
