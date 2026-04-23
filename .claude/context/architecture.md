# Architecture Reference

## Stack A — ASP.NET Core Web API

### Standard Layer Flow
```
HTTP Request
  → Controller          validate input, call service, return HTTP response
  → Service             orchestrate: validate → process → persist → return Result<T>
  → Repository          EF Core / Dapper queries, no business logic
  → Database            SQL Server / PostgreSQL
```

### Layer Responsibilities

| Layer | Class | Responsibility |
|-------|-------|---------------|
| Entry | `Controller` | Map HTTP ↔ domain. Zero business logic. Return `Result<T>` as HTTP response |
| Orchestration | `Service` | Coordinate steps. Own the transaction boundary |
| Data | `Repository` | DB queries only. No business logic. Return domain entities |
| Cross-cutting | `Middleware` | Auth, logging, error handling, correlation ID |

### Request Lifecycle
```
1. Middleware pipeline  → set CorrelationId, authenticate, authorize
2. Controller           → validate request model (FluentValidation / DataAnnotations)
3. Service              → business rules, call repository, publish events if needed
4. Repository           → EF Core / Dapper, AsNoTracking() on reads
5. Controller           → map Result<T> to IActionResult and return
```

### DI Lifetime Rules (ASP.NET Core built-in)
```
Scoped      → default for Services, Repositories, DbContext (one per HTTP request)
Singleton   → stateless, thread-safe only (config wrappers, feature flags)
Transient   → lightweight stateless utilities

⚠️ NEVER register DbContext as Singleton — causes InvalidOperationException under concurrency
```

---

## Stack B — AWS Lambda (.NET 8)

### Standard Lambda Flow
```
Event (SQS / SNS / EventBridge / API Gateway)
  → Function.cs         build Autofac container, set CorrelationId, catch BrokenCircuitException
  → SyncService         parse → validate → process → log → return SQSBatchResponse
  → ParserService       deserialize message body, separate ValidMessage / InvalidMessage
  → ProcessingService   domain logic + external API calls (wrapped with Polly)
  → Repository          Dapper + MySQL, batch 50 rows, transactional
```

### Layer Responsibilities

| Layer | Class | Responsibility |
|-------|-------|---------------|
| Entry | `Function.cs` | Build Autofac container, set CorrelationId, catch `BrokenCircuitException` |
| Orchestration | `SyncService` | Coordinate parse → process → log flow |
| Parsing | `ParserService` | Deserialize + validate. No business logic |
| Domain | `ProcessingService` | Business logic, external API calls |
| Data | `*Repository` | Dapper queries, transactions, no business logic |

### DI Lifetime Rules (Autofac)
```
InstancePerLifetimeScope  → default for services + repositories (one per Lambda invocation)
SingleInstance            → stateless, thread-safe config only (shared across warm invocations)
InstancePerDependency     → lightweight stateless utilities

Registration order in {Project}Module.cs:
  1. Configuration providers  (SingleInstance)
  2. Repositories             (InstancePerLifetimeScope)
  3. Services                 (InstancePerLifetimeScope)
  4. External providers       (InstancePerLifetimeScope)
```

### Resilience
```
All external HTTP calls → Polly circuit breaker
Function.cs            → catch BrokenCircuitException, log, rethrow
SQS failures           → return MessageId in SQSBatchResponse.BatchItemFailures (DLQ retry)
```
