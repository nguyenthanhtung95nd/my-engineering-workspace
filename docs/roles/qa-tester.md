# Role: QA — Tester

> Responsible for test planning, writing automated tests, verifying behavior
> against acceptance criteria, and catching regressions before code reaches production.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `/test` | Generate xUnit test suite from existing implementation code |
| `/security-review` | OWASP-based security test scenarios |
| `/code-review BUGS` | Identify logic errors and edge cases |
| `/perf` | Performance test targets and bottleneck analysis |
| `/debug` | Root cause analysis when a test fails unexpectedly |
| `diagnose` | Hard or flaky test failures — structured feedback loop to find the root cause |

---

## Workflow

```
1. Read PRD AC        → AC are the source of truth for test cases
2. /test              → Generate xUnit tests from implementation code
3. /security-review   → OWASP scenarios: IDOR, injection, auth bypass
4. /perf              → Performance targets: p95 latency, DB query cost
5. /code-review BUGS  → Logic errors the developer may have missed
6. Regression         → Before every release, /agent audit of changed files
```

---

## Real-World Example

**Scenario:** Test the Product CRUD Lambda. API Gateway, MySQL, Cognito auth.
Source: GitLab. The PRD acceptance criteria drive every test case.

---

### Step 1 — /test: Generate test suite from implementation

```
/test
[paste ProductService.cs and ProductRepository.cs]
```

Claude generates:

```csharp
// UnitTests/Services/ProductServiceTests.cs
public class ProductServiceTests
{
    private readonly IProductRepository _repo = Substitute.For<IProductRepository>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repo, NullLogger<ProductService>.Instance);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidProduct_ReturnsSuccess()
    {
        var request = new CreateProductRequest { Name = "Widget", Price = 9.99m, StockCount = 100 };
        _repo.CreateAsync(Arg.Any<ProductEntity>(), Arg.Any<CancellationToken>())
             .Returns(new ProductEntity { Id = 1, Name = "Widget" });

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
    }

    [Theory]
    [InlineData("", 10.00, "Name is required")]
    [InlineData("Widget", -1.00, "Price cannot be negative")]
    public async Task CreateAsync_WithInvalidInput_ReturnsFailure(
        string name, decimal price, string expectedError)
    {
        var request = new CreateProductRequest { Name = name, Price = price };

        var result = await _sut.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── SoftDeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteAsync_WhenProductExists_SetsIsActiveFalse()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>())
             .Returns(new ProductEntity { Id = 1, IsActive = true });

        var result = await _sut.SoftDeleteAsync(1);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).UpdateAsync(
            Arg.Is<ProductEntity>(p => p.IsActive == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenProductNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((ProductEntity?)null);

        var result = await _sut.SoftDeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── ListActiveAsync ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 20)]   // page 0 invalid
    [InlineData(1, 0)]    // pageSize 0 invalid
    [InlineData(1, 101)]  // pageSize exceeds max
    public async Task ListActiveAsync_WithInvalidPagination_ReturnsFailure(int page, int pageSize)
    {
        var result = await _sut.ListActiveAsync(page, pageSize);
        Assert.False(result.IsSuccess);
    }
}
```

**Coverage table:**

```
Method              Happy  Validation  NotFound  Error
CreateAsync           ✅       ✅         N/A      ✅
GetByIdAsync          ✅       ✅         ✅       ✅
ListActiveAsync       ✅       ✅         ✅       ✅
UpdateAsync           ✅       ✅         ✅       ✅
SoftDeleteAsync       ✅       N/A        ✅       ✅
```

---

### Step 2 — Integration tests: real MySQL via Testcontainers

```
/test
"Write integration tests for ProductRepository using Testcontainers MySQL.
 Cover: CreateAsync, GetByIdAsync returns null after SoftDelete,
 ListActiveAsync excludes soft-deleted products."
```

Claude generates:

```csharp
[Collection("Database")]
public class ProductRepositoryIntegrationTests(DatabaseFixture db)
{
    [Fact]
    public async Task GetByIdAsync_AfterSoftDelete_ReturnsNull()
    {
        // Arrange — create and then soft-delete a product
        var conn = db.GetConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO products (name, price, is_active) VALUES ('Widget', 9.99, 1); SELECT LAST_INSERT_ID()");

        await conn.ExecuteAsync(
            "UPDATE products SET is_active = 0 WHERE id = @Id", new { Id = id });

        var repo = new ProductRepository(db.ConnectionProvider, NullLogger<ProductRepository>.Instance);

        // Act
        var result = await repo.GetByIdAsync(id);

        // Assert — soft-deleted product must not be returned
        Assert.Null(result);
    }

    [Fact]
    public async Task ListActiveAsync_ExcludesSoftDeleted()
    {
        var conn = db.GetConnection();
        await conn.ExecuteAsync(
            "INSERT INTO products (name, price, is_active) VALUES ('Active', 1.00, 1), ('Deleted', 2.00, 0)");

        var repo = new ProductRepository(db.ConnectionProvider, NullLogger<ProductRepository>.Instance);

        var (items, total) = await repo.ListActiveAsync(page: 1, pageSize: 20);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Active", items.First().Name);
    }
}
```

---

### Step 3 — /security-review: OWASP test scenarios

```
/security-review
[paste the Lambda Function.cs routing + ProductService authorization check]
```

Claude flags and generates test scenarios:

```
🔴 CRITICAL — IDOR on GET /products/{id}
   Any authenticated user (Viewer OR Admin) can retrieve any product by ID.
   The service does not check ownership — there is no ownership concept here,
   but confirm: should Viewers be able to see inactive products by ID?
   If no: add is_active = 1 filter in GetByIdAsync.

Test scenario:
  1. Create product (Admin)
  2. Soft-delete it (Admin)
  3. GET /products/{id} as Viewer → should return 404, not the deleted product

🟡 HIGH — Role enforcement missing on UPDATE
   Confirm that PUT /products/{id} returns 403 for Viewer role, not 404 or 200.

Test scenario:
  1. Authenticate as Viewer
  2. PUT /products/1 with valid body → expect HTTP 403, not 200
```

---

### Step 4 — /perf: Performance targets

```
/perf
"Endpoint: GET /products?page=1&pageSize=20
 Current implementation: Dapper query with is_active filter.
 MySQL table: products, expected 10,000 rows at launch."
```

Claude responds:

```
Primary target: p95 < 100ms (Lambda + RDS Proxy + MySQL, same VPC)

Potential issue: COUNT(*) for totalCount runs as a second query.
At 10K rows with is_active index, this is fast.
At 1M rows, consider caching totalCount with a short TTL.

Index check: INDEX idx_products_active_created (is_active, created_at)
→ EXPLAIN should show index seek, not full scan. Verify with:
  EXPLAIN SELECT * FROM products WHERE is_active = 1 ORDER BY created_at DESC LIMIT 20;

No N+1 risk — single query for list, separate scalar for count.
Performance rating: acceptable for launch. Monitor at 100K rows.
```

---

## QA Checklist

```
□ /test run — coverage table shows all methods have happy + failure cases
□ Integration tests cover soft-delete and pagination exclusion
□ /security-review run — IDOR and role enforcement scenarios tested
□ /perf checked — index usage verified, p95 target defined
□ All AC from PRD have a corresponding test case
□ dotnet test passes — zero failures, no [Skip] without linked issue
```
