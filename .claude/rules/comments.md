# Rules: Comments

- **Bad comments**: describe *what* the code does — the code must be self-documenting.
- **Good comments**: explain *why* a decision was made, or document non-obvious constraints.
- Use `///` XML doc comments on all `public` API members (methods, classes, properties).
- No commented-out code — delete it. Git history is the backup.

```csharp
// Bad
orderId++; // increment orderId

// Good
// Shopify order IDs are 1-based; subtract 1 to align with our 0-based internal index
var internalIndex = shopifyOrderId - 1;
```

```csharp
/// <summary>
/// Retrieves an order by its unique identifier.
/// </summary>
/// <param name="id">The order ID. Must be greater than zero.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>
/// <see cref="Result{T}.Success"/> with the order if found;
/// <see cref="Result{T}.Failure"/> with an error message otherwise.
/// </returns>
public async Task<Result<Order>> GetByIdAsync(int id, CancellationToken ct = default)
```
