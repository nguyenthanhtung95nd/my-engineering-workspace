# Pre-PR Checklist

Before opening a Pull Request, verify every item below:

## Code Quality
- [ ] Methods < 20 lines, classes < 50 lines
- [ ] No copy-pasted logic — extracted into a shared method or service
- [ ] No magic numbers — named `const` or `static readonly` only
- [ ] No `bool` parameters in method signatures
- [ ] No string comparisons for domain logic — enums used, converted at system boundary
- [ ] Guard clauses used — max 2 levels of nesting
- [ ] No commented-out code

## Async & Nullability
- [ ] No `.Result` or `.Wait()` on async calls
- [ ] `CancellationToken` propagated through the entire async call chain
- [ ] `<Nullable>enable</Nullable>` active — no unchecked null dereferences

## Architecture
- [ ] Third-party libraries wrapped behind an `interface`
- [ ] No business logic in controllers (ASP.NET Core) or Function.cs (Lambda)
- [ ] DI lifetimes correct — no Singleton holding Scoped dependencies

## Security
- [ ] No hardcoded credentials, API keys, or connection strings
- [ ] No SQL string concatenation — parameterized queries only
- [ ] Authorization checks on all resource-access endpoints (IDOR prevention)
- [ ] No PII in logs

## Tests
- [ ] All public methods have unit tests (`Arrange / Act / Assert`)
- [ ] No `[Skip]` on tests without a linked tracked issue
- [ ] `dotnet test` passes locally with zero failures

## Error Handling
- [ ] Result<T> used for all service/repository returns (ASP.NET Core)
- [ ] No silent exception swallowing — logged before re-throwing
- [ ] Custom exception types for domain errors
