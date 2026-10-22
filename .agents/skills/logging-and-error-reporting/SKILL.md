---
name: logging-and-error-reporting
description: "Use when adding or reviewing logging, telemetry, tracing, or error reporting."
capability: "Use when adding or reviewing logging, telemetry, tracing, or error reporting."
side_effect_level: local_write
approval_required: false
requires_tools: "Project-documented tools; see body."
output_schema: "Evidence, changes and verification report."
risk_class: medium
---

# logging and error reporting

Use the project's existing logger, error types, redaction helpers and telemetry conventions. Classify operational failures, expected user errors and programmer faults separately.
Record a stable event name, safe context, correlation ID and actionable cause. Avoid credentials, prompts, personal paths, raw request bodies and unrestricted tool output. Use bounded/structured context.
Preserve typed errors across boundaries; do not turn failures into empty success results. Avoid repeated reports at each layer and noisy hot-loop logs.
For new outbound telemetry, verify the existing consent and export policy. Test redaction and failure behavior; report what was observed and any unverified provider delivery.


Resolve helper paths from this skill directory. Run project commands from the target workspace.
