---
allowed-tools: Read(*)
description: Review all changed files for bugs and performance issues. Runs automatically on git diff. Use before every PR. For security review use /security-review.
---

# Code Review — Bugs & Performance

Reviews all files changed since the last commit for logic errors and performance issues.

## Instructions

1. Run `git diff HEAD` to identify all changed `.cs` files.
2. Read each changed file in full — do not review diffs in isolation.
3. Apply rules from `@.claude/rules/` throughout.
4. For each issue found, note the file path, line number, severity, and recommended fix.

## Review Scope

### Bugs
- Logic errors, incorrect conditions, off-by-one
- Null reference risks — unguarded nullable dereferences
- Async anti-patterns — `.Result`, `.Wait()`, missing `CancellationToken`
- Result\<T\> misuse — swallowed failures, incorrect success/failure returns
- DI lifetime mismatches — Singleton holding Scoped dependency
- Missing guard clauses — input not validated at entry point

### Performance
- N+1 queries — queries inside loops
- Missing `AsNoTracking()` on read-only EF Core queries
- Unbounded queries — no pagination, no `Take()` limit
- Unnecessary allocations — `ToList()` on large sets before filtering
- Sync I/O on hot paths — blocking calls in async context

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
If no findings at all: `✅ No bugs or performance issues found.`
