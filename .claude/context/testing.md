# Testing Guide

## Overview

Two test types used across both stacks.

| Type | Tool | Speed | When to run |
|------|------|-------|-------------|
| Unit | xUnit + NSubstitute | < 5ms/test | Every code change |
| Integration | xUnit + Testcontainers (real DB) | Seconds | Before PR |

---

## Unit Tests

### Naming convention
`MethodName_Scenario_ExpectedResult`

Examples:
- `GetByIdAsync_WhenOrderExists_ReturnsSuccess`
- `GetByIdAsync_WhenIdIsZero_ReturnsFailure`
- `DispatchAsync_WithInvalidJson_ReturnsBatchFailure`

### ASP.NET Core unit test pattern (NSubstitute)
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
        Assert.Equal(order.Id, result.Value.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WhenIdInvalid_ReturnsFailure(int invalidId)
    {
        var result = await _sut.GetByIdAsync(invalidId);
        Assert.False(result.IsSuccess);
    }
}
```

### Lambda unit test pattern (Moq)
```csharp
public class SyncServiceTests
{
    private readonly Mock<IParserService> _mockParser = new();
    private readonly Mock<IProcessingService> _mockProcessor = new();
    private readonly Mock<IEntityRepository> _mockRepo = new();
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _sut = new SyncService(
            NullLogger<SyncService>.Instance,
            _mockParser.Object,
            _mockProcessor.Object,
            _mockRepo.Object);
    }

    [Fact]
    public async Task DispatchAsync_WithValidMessages_ProcessesSuccessfully()
    {
        // Arrange
        var sqsEvent = BuildSqsEvent(new { Sku = "SKU-001" });
        var parsed = new BatchValidationResult
        {
            ValidMessages = [new ValidMessage { MessageId = "msg-1" }],
            InvalidMessages = []
        };
        _mockParser.Setup(p => p.ValidateAndParseMessageBatch(sqsEvent)).Returns(parsed);
        _mockProcessor.Setup(p => p.ProcessBatchAsync(It.IsAny<List<ValidMessage>>()))
                      .ReturnsAsync([new ProcessResult { MessageId = "msg-1", IsSuccess = true }]);

        // Act
        var result = await _sut.DispatchAsync(sqsEvent);

        // Assert
        Assert.Empty(result.BatchItemFailures);
        _mockRepo.Verify(r => r.BulkCreateLogsAsync(It.IsAny<List<EntityLogEntity>>()), Times.Once);
    }

    private static SQSEvent BuildSqsEvent<T>(T payload) => new()
    {
        Records = [new() { MessageId = "msg-1", Body = JsonSerializer.Serialize(payload) }]
    };
}
```

---

## Integration Tests (Testcontainers)

### Database fixture
```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await ApplyMigrationsAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task ApplyMigrationsAsync()
    {
        // Run EF Core migrations or SQL scripts against the test DB
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
```

### Integration test
```csharp
[Collection("Database")]
public class OrderRepositoryIntegrationTests(DatabaseFixture db)
{
    [Fact]
    public async Task CreateAsync_ValidOrder_PersistsAndReturns()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(db.ConnectionString).Options;
        await using var context = new AppDbContext(options);
        var repo = new OrderRepository(context, NullLogger<OrderRepository>.Instance);
        var entity = new Order { Status = "pending", CreatedAt = DateTime.UtcNow };

        // Act
        var result = await repo.CreateAsync(entity);

        // Assert
        Assert.True(result.Id > 0);
        var persisted = await context.Orders.FindAsync(result.Id);
        Assert.NotNull(persisted);
    }
}
```

---

## Running Tests

```bash
# Unit tests only
dotnet test --filter "Category!=Integration"

# Integration tests
dotnet test --filter "Category=Integration"

# Single test by name
dotnet test --filter "FullyQualifiedName~GetByIdAsync_WhenOrderExists"

# All tests
dotnet test
```
