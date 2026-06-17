---
allowed-tools: Read(*), Bash(git diff*)
description: Analyze test coverage gaps for all changed .cs files. Reports which public methods are missing xUnit tests. Use before every PR to ensure nothing ships untested.
---

# Test Coverage Gap Analysis

Reports coverage gaps for all `.cs` files changed since the last commit.
Does **not** generate tests — use `/test` to generate missing tests.

## Instructions

1. Run `git diff HEAD --name-only` to get all changed files.
2. Filter to source files only — exclude `*Tests.cs` and `*IntegrationTests.cs`.
3. For each source file, find the corresponding test file:
   - Unit: `{Name}Tests.cs`
   - Integration: `{Name}IntegrationTests.cs`
4. For each `public` method in the source file, check whether a test exists covering each path.

## Coverage Paths to Check

| Path | What to look for |
|------|-----------------|
| Happy path | `[Fact]` or `[Theory]` with valid input, asserts `result.IsSuccess` |
| Validation failure | Invalid input returns `Result.Failure` with correct error message |
| Not found | Missing resource returns `Result.Failure` with "not found" |
| Error path | Dependency throws → exception handled, failure returned |

## Report Format

```
## Test Coverage Report

### {FileName}.cs

| Method | Happy | Validation | Not Found | Error | Test File |
|--------|-------|------------|-----------|-------|-----------|
| CreateAsync | ✅ | ✅ | N/A | ✅ | found |
| GetByIdAsync | ✅ | ❌ | ✅ | ❌ | found |
| ListActiveAsync | ❌ | ❌ | N/A | N/A | not found |

**Gaps:**
- `GetByIdAsync` — missing validation and error path tests
- `ListActiveAsync` — no test file found → run `/test ListActiveAsync`
```

If all changed methods are fully covered: `✅ All changed public methods have test coverage.`

## Notes

- Skip private methods, constructors, and auto-properties.
- N/A = that path does not apply (e.g., Create has no Not Found path).
- If no test file exists at all, flag the entire class as uncovered.
