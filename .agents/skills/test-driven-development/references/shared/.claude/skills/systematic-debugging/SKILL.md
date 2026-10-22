---
name: systematic-debugging
description: Use when encountering a bug, failing test, build failure, unexpected behavior, flaky behavior, regression, performance anomaly, or integration issue. Apply before proposing fixes so the agent finds root cause, compares working patterns, tests one hypothesis at a time, and avoids guess-and-check patches. Also use when the user asks to diagnose, investigate, root-cause, or fix a flaky failure.
capability: "Systematic debugging and evidence-first root-cause workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "git, project test/build commands, rg; optional logs, debugger, or instrumentation appropriate to the stack"
output_schema: "Root-cause report with reproduction, evidence, hypothesis, minimal fix, regression test, and verification commands"
risk_class: medium
---

# Systematic debugging

## Purpose

Find and fix root causes instead of treating symptoms. First make the failure observable and small,
then form hypotheses, instrument only what distinguishes them, fix the responsible boundary, and lock
the fix with a regression test.

Do not write a fix until the failure is reproduced or bounded, evidence identifies the failing layer,
and a single testable hypothesis exists.

## When to use

- A test, build, CLI, UI flow, or production-like workflow fails and the cause is unclear.
- A bug report is vague, intermittent, or seems inconsistent with the code.
- A performance regression needs measurement before optimization.
- The user asks to diagnose, investigate, root-cause, or fix a flaky failure.

For straightforward compile errors with an obvious one-line fix, use the normal edit/test flow.

## Workflow

1. **Read the failure completely.** Capture exact command, exit code, stack trace, file paths, line
   numbers, warnings, expected vs observed output, and environment notes.
2. **Reproduce or bound it.** Re-run the smallest relevant command. If it is flaky, gather frequency,
   inputs, timing, and logs before changing code.
3. **Minimise.** Reduce to the smallest command, fixture, route, or test that still fails.
4. **Check recent changes.** Inspect `git diff`, touched files, dependencies, config, generated
   files, and environment differences.
5. **Trace the data path.** For multi-component flows, inspect each boundary: input, output,
   config/env propagation, state transitions, and side effects.
6. **Find a working pattern.** Locate similar passing code or tests and compare the broken path
   line-by-line.
7. **State one hypothesis.** Write: “I think the root cause is X because evidence Y.” List 1–3
   causes only if needed, and name the evidence that would distinguish them.
8. **Instrument narrowly.** Add temporary logs, assertions, breakpoints, or focused probes. Remove
   them unless they become useful permanent diagnostics.
9. **Create or identify a regression test.** Prefer an automated failing test. If no harness exists,
   create a focused script or documented reproduction.
10. **Fix only the root cause.** Patch the smallest responsible boundary. Avoid bundled refactors
    until the regression is green.
11. **Verify.** Run the targeted reproduction, then the appropriate broader suite. Document any
    unrelated remaining failures.

## Stop signals

- About to “just try” a fix without evidence.
- Multiple simultaneous hypotheses.
- A test was edited to match the implementation rather than the requirement.
- Two fix attempts failed; re-open root-cause analysis before a third.
- Three fix attempts failed; pause and question architecture or assumptions with the user.
- Tempted to weaken assertions, delete tests, or hide errors with broad catch-alls.

## Security-sensitive failures

If the bug touches data loss, auth, secrets, payments, or destructive actions, run a security review
before finalizing.

## Relationship to other skills

- Use [`test-driven-development`](../test-driven-development/SKILL.md) once the failing regression
  test defines the behavior to restore.
- Use [`verification-before-completion`](../verification-before-completion/SKILL.md) before claiming
  the bug is fixed.
- When the failure was non-obvious, add a short RCA entry to the project failure log if the repo has
  one (date, symptom, root cause, guard).

## Evidence template

```markdown
## Root Cause
- Symptom:
- Reproduction command:
- Evidence gathered:
- Working comparison:
- Hypothesis tested:
- Root cause:
- Fix plan:
- Verification:
```

## Verification checklist

- [ ] Failure output read fully and cited in notes.
- [ ] Reproduction or bounded flaky behavior documented.
- [ ] Working examples compared where available.
- [ ] One hypothesis tested at a time.
- [ ] Regression test or equivalent reproduction verifies the fix.
- [ ] Targeted and relevant broader checks pass or remaining failures are documented.
