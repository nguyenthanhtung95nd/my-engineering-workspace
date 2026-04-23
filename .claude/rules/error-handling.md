# Rules: Error Handling

## Core Pattern — Result<T> (ASP.NET Core)

Never return `null` or throw generic exceptions from service/repository layers.
Always use `Result<T>` so callers are forced to handle both success and failure.

```csharp
// Bad — caller doesn't know if null means "not found" or "error"
public async Task<Order?> GetOrderAsync(int id) { ... }

// Good — explicit success/failure with reason
public async Task<Result<Order>> GetOrderAsync(int id, CancellationToken ct = default)
{
    if (id <= 0) return Result.Failure<Order>("Invalid ID.");

    try
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null
            ? Result.Failure<Order>($"Order {id} not found.")
            : Result.Success(order);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve order {OrderId}", id);
        return Result.Failure<Order>("An unexpected error occurred.");
    }
}
```

## Lambda Error Handling

- In Lambda: unhandled exceptions propagate to SQS batch item failure mechanism.
- Do **not** wrap every handler in `try/catch` — use `reportBatchItemFailures` for retry via DLQ.
- Log exceptions before they escape a processing boundary.

```csharp
// Bad — silently swallowed
try { ... }
catch (Exception) { }

// Good — logged, wrapped, re-thrown
try { ... }
catch (SqlException ex)
{
    _logger.LogError(ex, "Database error for order {OrderId}", orderId);
    throw new OrderPersistenceException(orderId, ex);
}
```

## General Rules

- Throw **specific, meaningful exceptions** — never throw raw `Exception` or `ApplicationException`.
- Create custom exception types for domain errors: `OrderNotFoundException`, `InsufficientStockException`.
- Do not silently swallow exceptions. At minimum, log before re-throwing.
- Use `try/catch` only where you can meaningfully recover. Let unhandled exceptions bubble to global middleware (ASP.NET Core) or Lambda error handling.
