# Rules: Code Structure

## The Four Rules (Non-Negotiable)

1. **Write code that is easy to understand.**
2. **Write code that can have as few bugs as possible.**
3. **Constantly clean up your code (refactor).**
4. **Write code that creates value for customers.**

## Single Responsibility Principle (SRP) — Most Critical

- Every method, class, and module does **exactly one thing**.
- If a class name contains "And" → split it. (`OrderValidatorAndSaver` → two classes)
- No nested `if` blocks beyond 2 levels. No nested `try/catch`.

## Size Limits

- Methods: **< 20 lines**
- Classes: **< 50 lines** (excluding auto-properties and constructors)
- The more complex the logic, the shorter it must be.

## DRY — Don't Repeat Yourself

- Never copy-paste logic. Extract into a shared method, extension method, or service.
- Exception: duplicating 2–3 trivial lines is acceptable if abstraction adds more indirection than value.

## Levels of Abstraction

- Each method operates at **one level of abstraction** only.
- High-level methods call mid-level methods — they do not contain low-level detail.

```csharp
// Bad — controller bypasses service layer
public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
{
    var entity = new Order { /* mapping */ };
    await _dbContext.Orders.AddAsync(entity);
    await _dbContext.SaveChangesAsync();
    return Ok();
}

// Good
public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
{
    var result = await _orderService.CreateAsync(request);
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}
```

## Variables & Properties

- Declare variables **as close as possible** to where they are used.
- Always initialize at declaration.
- Use `var` when the type is obvious; explicit types when it aids clarity.
- Prefer `readonly` fields and `init`-only properties wherever possible.
- **No static mutable state** — avoid `static` fields that change at runtime.

## Conditionals & Control Flow

- **Minimize boolean logic** — it is the highest source of bugs.
- Replace `if/else if` chains with polymorphism, strategy pattern, or dictionaries.
- Use **enums** instead of string comparisons. Convert strings to enums at the system entry point.
- Use **guard clauses** (early returns) to reduce nesting.

```csharp
// Bad
public void Process(Order order)
{
    if (order != null)
    {
        if (order.IsValid)
        {
            // logic buried 2 levels deep
        }
    }
}

// Good
public void Process(Order order)
{
    if (order is null) throw new ArgumentNullException(nameof(order));
    if (!order.IsValid) return;

    // logic at top level
}
```

## Null Handling

- Enable **Nullable Reference Types** in all projects: `<Nullable>enable</Nullable>` in `.csproj`.
- Never return `null` from a method that returns a collection — return an empty collection.
- Use `ArgumentNullException.ThrowIfNull()` for null guard checks (.NET 6+).
