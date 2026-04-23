# Rules: Methods & Classes

## Methods

- Do **one thing only**.
- Max **3 parameters**. If more are needed, group into a dedicated request/options object.
- **Never pass `bool` as a method argument** — it is a hidden SRP violation.

```csharp
// Bad
ProcessOrder(order, isExpress: true);

// Good
ProcessExpressOrder(order);
ProcessStandardOrder(order);
```

- Avoid `out` parameters — return a `record`, `tuple`, or Result object instead.
- Methods must have **no hidden side effects** unless that is their explicit, named purpose.

## Classes & Interfaces

- High **cohesion**: fields and methods must be tightly related.
- Low **coupling**: inject dependencies via constructor — never `new` a service inside a class.
- Constructors must be **simple** — no I/O, no network calls, no business logic.
- Prefer **composition over inheritance**. Use `interface` to define contracts.
- Seal classes by default unless explicitly designed for extension (`sealed class`).
- Use `record` for immutable data transfer objects (DTOs / value objects).

```csharp
// Good — primary constructor (.NET 8+), sealed, constructor injection
public sealed class OrderService(IOrderRepository repository, ILogger<OrderService> logger)
{
    public async Task<Result<Order>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0) return Result.Failure<Order>("ID must be greater than zero.");
        var order = await repository.GetByIdAsync(id, ct);
        return order is null
            ? Result.Failure<Order>("Order not found.")
            : Result.Success(order);
    }
}
```

## Dependency Injection

### ASP.NET Core (built-in DI)
```csharp
// Program.cs
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>(); // stateless only

// ⚠️ NEVER AddSingleton<DbContext> — causes concurrency exceptions
```

### AWS Lambda (Autofac)
```csharp
// {Project}Module.cs
builder.RegisterType<OrderService>()
       .As<IOrderService>()
       .InstancePerLifetimeScope();

builder.RegisterType<OrderRepository>()
       .As<IOrderRepository>()
       .InstancePerLifetimeScope();

// SingleInstance → stateless, thread-safe config only
```
