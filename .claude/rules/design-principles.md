# Rules: Design Principles

## SOLID

| Principle | C# Application |
|-----------|---------------|
| **S** — Single Responsibility | One class, one reason to change. Split `UserService` into `UserAuthService` + `UserProfileService` when responsibilities diverge |
| **O** — Open/Closed | Extend via new classes; do not edit existing ones. Use strategy or decorator pattern |
| **L** — Liskov Substitution | Derived classes must honor the base interface contract. Never throw `NotImplementedException` in a real implementation |
| **I** — Interface Segregation | Small, focused interfaces over fat ones. `IReader` + `IWriter` instead of `IReadWriter` |
| **D** — Dependency Inversion | Always inject `IOrderRepository`; never `new SqlOrderRepository()` inside a service |

## Additional Rules

- **YAGNI** — Do not implement features not required by the current ticket.
- **Design for testability** — if it's hard to mock, the design is wrong. Refactor to use interfaces.
- **Wrap third-party libraries** — never call NuGet SDK types directly from business logic. Always wrap behind an interface.

```csharp
// Bad — business logic coupled to Stripe SDK
public async Task ChargeAsync(decimal amount)
{
    var service = new ChargeService(); // Stripe concrete class
    await service.CreateAsync(...);
}

// Good — depend on abstraction
public async Task ChargeAsync(decimal amount)
{
    await _paymentGateway.ChargeAsync(amount); // IPaymentGateway
}
```

- **Controllers are Humble Objects** — they map HTTP requests to service calls and return responses. Zero business logic inside controllers.
- **Dependencies flow inward** — outer layers (Infrastructure, API) depend on inner layers (Application, Domain). Never the reverse.
- **Right-size your solution** — do not introduce microservices, event sourcing, or CQRS unless the problem genuinely demands it. Solve today's problem; design for tomorrow's scale.
