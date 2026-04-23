# Rules: Testing

## Testing Pyramid (target ratios)

| Type | Target | Tool | Purpose |
|------|--------|------|---------|
| Unit | ~80% | xUnit + NSubstitute | Verify individual methods/classes in isolation |
| Integration | ~15% | xUnit + Testcontainers (real DB) | Verify layer interactions |
| E2E / Functional | ~5% | xUnit + Testcontainers / Playwright | Verify full system behavior |

## Unit Test Rules

- Every **public method** must have unit tests.
- Tests must be **fast** (< 5ms each). Mock all external dependencies: HTTP, DB, file system, clock.
- **Do not mock the database** — integration tests must hit a real DB. Mock/prod divergence causes silent failures.
- Tests must be **deterministic** — no `DateTime.Now`, no random values. Inject `IDateTimeProvider`.
- Structure: **Arrange → Act → Assert** per test.
- One logical scenario per test method.
- Test method name format: `MethodName_Scenario_ExpectedResult`
- Test code may repeat itself (**DAMP over DRY**) to keep each test self-contained and readable.

```csharp
public class OrderServiceTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_repository, NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ReturnsSuccess()
    {
        // Arrange
        var order = new Order { Id = 1, Status = "pending" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(order, result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                   .Returns((Order?)null);

        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
```

## CI Enforcement

- All unit tests must pass before any PR is merged — enforced in CI (`dotnet test`).
- Integration tests run in nightly build or on PR to `main`.
- Tests must **always pass**. Fix or delete failing tests — never leave `[Skip]` permanently without a linked issue.
