---
name: dotnet-patterns
description: >
  .NET 8 engineering guidelines: TDD, C# 12, behavior-driven testing,
  immutability, and Lambda-specific patterns (Autofac, Dapper, Moq).
  Auto-loads on .cs and .csproj files.
context: auto
---

# .NET 8 Engineering Patterns

## Core Philosophy

**TDD is non-negotiable.** RED → GREEN → REFACTOR.

1. Define expected behavior.
2. Write a failing test.
3. Implement the minimum code to pass.
4. Refactor only if it improves clarity or maintainability.

Never design a large solution before establishing behavior through tests.

**Prefer boring and obvious solutions over clever ones.** If a junior engineer cannot understand it quickly, simplify it.

## Test Behavior, NOT Implementation

Treat the system under test as a black box. Validate inputs, outputs, state transitions, and business outcomes.

**Do NOT test:**
- Private methods or internal implementation details
- Framework mechanics
- Method invocation counts unless behavior depends on them
- Repository calls in isolation from their SQL

Good tests describe business rules. Bad tests describe implementation.

## DRY — Knowledge, Not Code

DRY means **do not duplicate knowledge** — not "remove every piece of similar-looking code."

Two code blocks that represent different business concepts must remain separate even if they look similar.

> **Wrong abstractions are more expensive than duplication.**

Abstract only when behavior and meaning are truly shared.

## Business-First Architecture

Organize code around business capabilities, not technical layers.

```
// Prefer — developer understands WHAT the system does
Sync/
Products/
Categories/

// Avoid — developer only knows HOW it's structured
Services/
Repositories/
DTOs/
```

## C# 12 (.NET 8)

```csharp
// Primary constructors — preferred over traditional boilerplate
public sealed class SyncService(IParserService parser, ILogger<SyncService> logger) { }

// Collection expressions
string[] empty = [];
List<string> skus = ["SKU-001", "SKU-002"];

// Pattern matching guard
if (entity is null) throw new ArgumentNullException(nameof(entity));
var label = status switch { SyncStatus.Success => "ok", _ => "fail" };
```

## Immutability — Records for DTOs

```csharp
// DTOs and value objects → record (immutable, structural equality)
public record SyncContext(string StoreCode, SyncType SyncType, string? Cursor = null);

// Strongly typed IDs — prevent primitive obsession
public readonly record struct StoreId(string Value)
{
    public override string ToString() => Value;
}
```

## Nullable Reference Types (mandatory)

```xml
<!-- Every .csproj must have -->
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

```csharp
ArgumentNullException.ThrowIfNull(request);  // .NET 6+ guard
string? cursor = null;                        // always declare intent
```

## Async Conventions

```csharp
// Always propagate CancellationToken — never .Result or .Wait()
public async Task<List<ItemEntity>> GetBySkusAsync(
    IEnumerable<string> skus,
    CancellationToken ct = default)

// ConfigureAwait(false) in library / infrastructure code
var result = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
```

## Test Data — Builder Pattern

Builders produce valid objects by default, require minimal setup, and hide irrelevant details. Tests should emphasize intent, not setup.

```csharp
// Fluent builder with sensible defaults — one per domain object
public class SyncContextBuilder
{
    private string _store = "store-001";
    private SyncType _type = SyncType.Incremental;

    public SyncContextBuilder WithStore(string store) { _store = store; return this; }
    public SyncContextBuilder WithType(SyncType type) { _type = type; return this; }
    public SyncContext Build() => new(_store, _type);
}
```

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes / Interfaces | PascalCase | `SyncService`, `ISyncService` |
| Methods | PascalCase | `DispatchAsync`, `GetBySkusAsync` |
| Private fields | `_camelCase` | `_parser`, `_logger` |
| Parameters / locals | camelCase | `sqsEvent`, `cancellationToken` |
| Constants | PascalCase | `BatchSize`, `MaxRetryCount` |

## DI Lifetime Rules (Autofac — Lambda)

```csharp
// SingleInstance — stateless, shared across warm invocations
containerBuilder.RegisterType<ApiClient>().As<IApiClient>().SingleInstance();

// InstancePerLifetimeScope — one per Lambda invocation (DEFAULT for everything else)
containerBuilder.RegisterType<SyncService>().As<ISyncService>().InstancePerLifetimeScope();
```

## Error Handling (Lambda)

Lambda services **throw and rethrow** — `Result<T>` is **not** used in the Lambda layer.
`BrokenCircuitException` must propagate to the runtime — never catch it in `SyncService`.

```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Request failed for SKU {Sku}", sku);
    throw;
}
```

## Code Review Framework

When reviewing code, evaluate in this order:

| Dimension | Question |
|-----------|----------|
| **Correctness** | Does it work? Are edge cases handled? |
| **Testability** | Can behavior be tested easily without mocking internals? |
| **Maintainability** | Will future developers understand it? |
| **Simplicity** | Is there a simpler solution? |
| **Architecture** | Does the code belong in the correct layer? |

Suggest improvements ordered by impact. Do not refactor unless there is measurable value.

## Performance Mindset

Never optimize prematurely. Follow this order:

**Correctness → Readability → Maintainability → Measurement → Optimization**

Never introduce complexity for hypothetical performance gains. Measure first, optimize second.

## TDD Output Format (when implementing a feature)

1. Explain the expected behavior.
2. Identify test cases (happy path + edge cases).
3. Write tests first.
4. Implement the minimum code to pass.
5. Refactor if it improves clarity.
6. Explain key architectural decisions.

## Pre-Submission Checklist

- [ ] Tests written **before** production code (TDD — Red → Green → Refactor)
- [ ] Tests validate behavior, not implementation details
- [ ] All async methods have `CancellationToken`
- [ ] No `.Result` or `.Wait()`
- [ ] `<Nullable>enable</Nullable>` active — no unchecked null dereferences
- [ ] All public methods have XML doc comments
- [ ] No bare `catch (Exception)` without structured logging
- [ ] No SQL string concatenation — parameterized `DynamicParameters` only
- [ ] No hardcoded credentials, API keys, or connection strings
- [ ] Abstraction justified — not extracted just because code looks similar
