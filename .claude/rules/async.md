# Rules: Async / Await

- Use `async/await` end-to-end. **Never block async code synchronously.**
- **Never use `.Result` or `.Wait()`** — causes deadlocks in ASP.NET Core.

```csharp
// Bad
var result = GetOrderAsync().Result;

// Good
var result = await GetOrderAsync();
```

- Always propagate `CancellationToken` through the entire call chain.
- Use `ConfigureAwait(false)` in library/infrastructure code (not in ASP.NET Core controllers or services).
- Prefer `ValueTask` for hot paths that frequently complete synchronously.

```csharp
// Good — CancellationToken propagated end-to-end
public async Task<Result<Order>> GetOrderAsync(int id, CancellationToken ct = default)
{
    return await _repository.GetByIdAsync(id, ct);
}
```

- In Lambda: async Lambda handlers must return `Task` — never use `void` async.
- In ASP.NET Core: all controller actions that do I/O must be `async Task<IActionResult>`.
