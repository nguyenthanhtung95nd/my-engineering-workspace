---
allowed-tools: Read(*)
description: Perform a targeted code review of the codebase
---

# Code Review

**MODE:** $ARGUMENTS

## Review Modes

| Mode | Focus |
|------|-------|
| `BUGS` | Logical errors, null references, off-by-one, incorrect conditions |
| `SECURITY` | OWASP Top 10, injection, broken auth, IDOR, insecure data exposure |
| `PERFORMANCE` | N+1 queries, missing AsNoTracking, unbounded queries, memory leaks |

Modes can be combined (e.g., `BUGS,SECURITY`) — perform all selected review types together.

If MODE is empty or unrecognized, perform a **thorough general code review**.

## Instructions

1. Run `git diff HEAD` to understand what changed.
2. Read each changed file in full context — do not review diffs in isolation.
3. Apply rules from `@.claude/rules/` throughout.
4. For each issue found, note the file path, line number, severity, and recommended fix.

## Report Format

```
## Code Review Summary
Brief overview of changes and overall assessment.

## Findings

### [SEVERITY] Short title — `path/to/file.cs:line`
**Issue:** What is wrong and why it matters.
**Fix:** Concrete recommendation with code example if needed.
```

Severity levels: `Critical` | `High` | `Medium` | `Low` | `Info`

If no findings in a severity level, omit that section.
