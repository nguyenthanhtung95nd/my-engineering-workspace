---
name: dotnet-patterns
description: >
  Load automatically when working with C# files, .NET projects,
  ASP.NET Core controllers, EF Core repositories, or any .cs file.
  Provides .NET 8 conventions, production-ready code patterns,
  and team standards for this workspace.
context: auto
patterns:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/appsettings*.json"
---

# .NET Engineering Patterns

## Error Handling — Result<T> (mandatory)

Every service and repository method returns `Result<T>`. Never return null. Never throw a generic exception without logging.

```csharp
public async Task<Result<T>> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    if (id <= 0)
        return Result.Failure<T>("ID must be greater than zero.");

    try
    {
        var entity = await _db.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity is null
            ? Result.Failure<T>($"{typeof(T).Name} with ID {id} was not found.")
            : Result.Success(entity);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve {Entity} {Id}", typeof(T).Name, id);
        return Result.Failure<T>("An unexpected error occurred.");
    }
}
```

## EF Core Query Conventions

```csharp
// Read-only queries — always AsNoTracking()
var items = await _db.Orders
    .AsNoTracking()
    .Where(o => o.UserId == userId)
    .OrderBy(o => o.CreatedAt)
    .ToListAsync(cancellationToken);

// Write operations — track for change detection
var entity = await _db.Orders
    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
```

## Async Conventions

```csharp
// Always include CancellationToken with a default value
public async Task<Result<User>> GetUserAsync(
    int id,
    CancellationToken cancellationToken = default)

// Never sync-over-async
// ❌  var result = service.GetAsync().Result;
// ✅  var result = await service.GetAsync();
```

## Constructor Injection

```csharp
public sealed class UserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }
}
```

## XML Doc Comments (required on public APIs)

```csharp
/// <summary>
/// Retrieves a user by their unique identifier.
/// </summary>
/// <param name="id">The user ID. Must be greater than zero.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// <see cref="Result{T}.Success"/> with the user if found;
/// <see cref="Result{T}.Failure"/> with an error message otherwise.
/// </returns>
public async Task<Result<User>> GetUserAsync(
    int id,
    CancellationToken cancellationToken = default)
```

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes / Interfaces | PascalCase | `UserService`, `IUserRepository` |
| Methods | PascalCase | `GetUserAsync`, `CreateOrderAsync` |
| Private fields | `_camelCase` | `_db`, `_logger`, `_cache` |
| Parameters / locals | camelCase | `userId`, `cancellationToken` |
| Constants | PascalCase | `DefaultPageSize`, `MaxRetryCount` |

## DI Lifetime Rules

```csharp
// Scoped — one instance per HTTP request (DbContext, application services)
builder.Services.AddScoped<IUserService, UserService>();

// Singleton — one instance for the application lifetime (config, thread-safe caches)
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

// Transient — new instance every time (lightweight, stateless utilities)
builder.Services.AddTransient<IEmailFormatter, EmailFormatter>();

// ⚠️ Never register DbContext as Singleton — causes InvalidOperationException under concurrent load
```

## Pre-Submission Checklist

Before committing any C# code:

- [ ] All service/repository methods return `Result<T>`
- [ ] All async methods have `CancellationToken`
- [ ] All read-only EF Core queries use `AsNoTracking()`
- [ ] All public methods have XML doc comments
- [ ] Input validated at method entry — fail fast
- [ ] No bare `catch (Exception)` without structured logging
- [ ] No hardcoded connection strings, passwords, or API keys
