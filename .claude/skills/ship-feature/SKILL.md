---
name: ship-feature
description: >
  Pre-PR orchestrator that chains four sub-commands in sequence: /code-review →
  /security-review → /test-coverage → /pr-summary. Produces a structured report
  and a ready-to-paste PR description in one command. Use when a feature, bug fix,
  or task is done and ready to be raised as a pull request.
---

# ship-feature

Run the full pre-PR chain with one command.

## Steps

```
1. /code-review      → bugs and performance across all changed files
2. /security-review  → OWASP Top 10 across all changed files
3. /test-coverage    → coverage gap report for all changed files
4. /pr-summary       → PR description with findings surfaced
```

## Gate rules

| Outcome | Action |
|---------|--------|
| Step 1 or 2 returns **Critical** | Stop. Report the finding. Do not run Steps 3–4. |
| Step 1 or 2 returns **High** | Continue all steps. Surface findings in PR "Review notes". |
| Steps 3–4 | Always run unless blocked by a Critical above. |

## Execution

Get all files changed since the base branch, then pass the same list to every step:

```bash
git diff main...HEAD --name-only
```

Exclude test files from Step 3 only.

## Output

```
## ship-feature Results

### Step 1 — Code Review
[findings or ✅ No issues found.]

### Step 2 — Security Review
[findings or ✅ No vulnerabilities found.]

### Step 3 — Test Coverage
[gap table or ✅ All changed public methods have test coverage.]

---

## PR Description (ready to paste)

[title]
[body]
```
