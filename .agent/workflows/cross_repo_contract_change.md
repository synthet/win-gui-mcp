---
description: Cross-repo contract change — owner repo first, then consumers
---

# Cross-repo contract change

## Purpose

Change a **shared contract** (REST API, DB schema, shared types, or user-facing vocabulary) without
breaking a sibling/consumer repo or letting docs drift.

## When to use

- Any change to a public API path/field, a DB column a consumer reads, a shared type, or a
  user-facing label that another repo depends on.

## Principle

The **owner repo** (the one that defines the contract) changes **first**, in this order: written
contract → owner implementation → consumer integration. Never ship code contradicted by the written
contract.

## Steps

1. **Owner canonical contract** — update the authoritative contract docs/artifacts (e.g.
   `docs/technical/API_CONTRACT.md`, an OpenAPI/schema file, `docs/CANONICAL_SOURCES.md`) **before or
   with** code.
2. **Owner implementation** — implement the change behind the now-updated contract; add/adjust tests.
3. **Regenerate artifacts** — regenerate any generated types/clients the consumer relies on.
4. **Consumer integration** — update the sibling repo(s) to the new contract; run their checks.
5. **Coordinate the merge** — land owner first (or behind a flag), then consumers; avoid a window
   where the consumer assumes the new shape before the owner ships it.

## Do not

- Do not change consumer-side first for an owner-owned field.
- Do not rename shared columns/fields/types without updating every consumer in the same change set.
