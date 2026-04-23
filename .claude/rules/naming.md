# Rules: Naming

Follow Microsoft's official C# naming conventions without exception.

| Element | Convention | Example |
|---------|------------|---------|
| Class, Interface, Enum | `PascalCase` | `OrderService`, `IOrderRepository` |
| Public method / property | `PascalCase` | `CalculateTotal()`, `OrderId` |
| Private field | `_camelCase` | `_orderRepository` |
| Local variable / param | `camelCase` | `orderId`, `isValid` |
| Constant | `PascalCase` | `MaxRetryCount` |
| Interface | `I` prefix | `IPaymentGateway` |
| Async method | `Async` suffix | `GetOrderAsync()` |
| Generic type param | `T` prefix | `TEntity`, `TResult` |

- Names must be **short yet descriptive**. No abbreviations, no vague names (`data`, `obj`, `temp`, `mgr`).
- If naming is hard → the design is probably wrong (SRP violation). Stop and redesign.
- No magic numbers — declare as a named `const` or `static readonly`.

```csharp
// Bad
Thread.Sleep(86400000);

// Good
private const int OneDayInMilliseconds = 86_400_000;
Thread.Sleep(OneDayInMilliseconds);
```
