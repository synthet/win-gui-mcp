---
name: hook-loopback-safety
description: "Use when implementing agent lifecycle hooks, approval callbacks, or authenticated loopback event queues."
capability: "Use when implementing agent lifecycle hooks, approval callbacks, or authenticated loopback event queues."
side_effect_level: local_write
approval_required: false
requires_tools: "Project-documented tools; see body."
output_schema: "Evidence, changes and verification report."
risk_class: medium
---

# hook loopback safety

Read the harness's current hook schema and the application's event/approval contract. Treat stdin and network events as untrusted.
Normalize through a versioned typed schema. Authenticate loopback requests, validate origin/session/correlation identity, and constrain payload size and lifetime. Correlate each decision with one pending action.
Use idempotent delivery and replay rejection. Expired, malformed or unavailable decisions must fail closed for blocking approvals; observation hooks may fail softly according to the explicit policy.
Never let a hook return permission that the parent harness or user has not granted. Keep secrets and unrestricted tool payloads out of logs and external notifications.
Test malformed input, unavailable bridge, timeout, duplicate delivery, stale event and cross-session replay. Preserve native exit-code/output semantics for each harness; do not copy an event name between harnesses without a verified mapping.


Resolve helper paths from this skill directory. Run project commands from the target workspace.
