---
name: test-driven-development
description: Use when implementing a feature, bug fix, refactor, or behavior change that can be tested. Apply before production code changes to enforce red-green-refactor, prove tests fail for the intended reason, and keep implementation minimal. Also use for risky vertical slices or when the user asks for TDD, red/green/refactor, or tests first.
capability: "Test-driven development red-green-refactor workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "Project test runner and editor; use repo-specific test commands when documented"
output_schema: "TDD log with failing test evidence, minimal implementation, passing verification, and refactor notes"
risk_class: medium
---

# Test-driven development

## Purpose

Use tests to define expected behavior before changing production code. A passing test that never
failed does not prove the behavior is covered. Prefer short feedback loops so agent edits stay grounded
in executable evidence rather than broad speculative changes.

## When to use

- Adding behavior where a unit, integration, CLI, or snapshot test can observe the outcome.
- Fixing a bug that should never regress.
- Refactoring behavior behind an existing public seam.
- Any change where the user asks for TDD, red/green/refactor, or tests first.

Do **not** force TDD for pure documentation, mechanical formatting, generated asset sync, or when the
repo has no practical test seam. State the limitation and choose the closest verification.

## Use with

- [`systematic-debugging`](../systematic-debugging/SKILL.md) when a bug's root cause is not yet known.
- [`verification-before-completion`](../verification-before-completion/SKILL.md) before claiming the
  implementation is complete.

## Workflow

1. **Choose one vertical slice.** Name the externally visible behavior and the smallest seam that
   can verify it.
2. **RED — write one failing test.** Name the behavior clearly, exercise public contracts and real
   code where practical, and keep one assertion focus. For bug fixes, reproduce the reported symptom.
3. **Verify RED.** Run the smallest relevant test command. Confirm it fails for the intended reason,
   not typos, imports, fixtures, or setup. If it passes immediately, rewrite it.
4. **GREEN — implement the minimum.** Write only enough production code to pass. Do not add
   unrequested options, abstractions, cleanup, or drive-by edits. If the test is hard to write, treat
   that as design feedback and improve the seam.
5. **Verify GREEN.** Re-run the targeted test. If the expectation still matches the requirement, fix
   production code rather than weakening the test.
6. **REFACTOR — improve while green.** Remove duplication or clarify names only after GREEN. Do not
   add behavior during refactor. Re-run targeted tests after meaningful refactors.
7. **Widen.** Repeat for the next slice, then run the surrounding package/module tests or the
   documented fast suite.

## Existing production code

If exploratory code was written first, treat it as a spike: set it aside, write the failing test,
then implement from the test. If discarding would be risky or expensive, tell the user and convert
the existing behavior into tests before changing it further.

## Test quality rules

Prefer tests that:

- Assert behavior at a stable public interface rather than implementation details.
- Would fail on the original bug or missing feature.
- Use small fixtures and deterministic data.
- Cover one reason to fail per test and name the scenario in domain language.
- Cover edge cases and errors that define the contract.

Avoid tests that:

- Only assert mocks were called when visible behavior can be checked.
- Freeze incidental formatting or timestamps without a product reason.
- Require network, secrets, production services, or broad sleeps.
- Reproduce the implementation line-for-line.

## Agent-safe patterns

- Read existing nearby tests before inventing infrastructure.
- Prefer the repo's documented test command over ad hoc runners.
- Keep each red/green step small enough to explain in the final response.
- If a failing test exposes an unrelated pre-existing issue, narrow the command or document the
  blocker before changing scope.

## Red flags

- Code before test, or test added only after implementation.
- Test passes immediately.
- “Manual testing is enough” / “Too simple to test.”
- Weakening assertions to go green.

## Output

```markdown
## TDD Log
- Behavior slices:
- RED test:
- RED command and failure:
- GREEN change:
- GREEN command and pass:
- Refactor notes:
- Broader verification:
- Seams that could not be created (if any):
```

## Verification checklist

- [ ] New behavior has a focused test.
- [ ] The test failed for the intended reason before implementation.
- [ ] Production code is the minimal change needed to pass.
- [ ] Targeted and relevant broader tests pass.
- [ ] No test was weakened to fit the implementation.
