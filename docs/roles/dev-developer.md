# Role: DEV — Developer

> Responsible for implementing features, fixing bugs, writing tests, and
> ensuring code meets production standards before every PR.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `grill-me` | Clarify implementation approach before writing any code |
| `prd-to-plan` | Slice the PRD into phased vertical slices |
| `do-work` | Implement phase by phase — build/test loop, Work Summary |
| `/code-review BUGS,SECURITY` | Final gate before raising a PR |
| `/debug` | Root cause analysis when something breaks |
| `/migrate` | Validate schema changes before applying |

---

## Workflow

### New Feature

```
1. Read PRD from prd/{feature}-prd.md
2. prd-to-plan      → Slice into vertical phases
3. do-work          → Implement phase 1: skeleton builds green
4. do-work          → Implement phase 2: add domain logic
5. ...repeat per phase...
6. /code-review BUGS,SECURITY → Final review before PR
```

### Bug Fix

```
1. /incident        → If production is down: triage first
2. /debug           → Root cause — never fix without understanding why
3. do-work          → Minimal fix + regression test
4. /code-review     → Before PR
```

### Quick Task (no PRD needed)

```
Just describe the task — do-work handles it without a plan file.
"Fix the null reference in ProductService when description is missing."
```

---

## Real-World Example

**Scenario:** Implement the Product CRUD Lambda from the PRD.
Stack: .NET 8 Lambda, API Gateway, MySQL via RDS Proxy, Autofac, Dapper.
Source: GitLab. Deploy: CDK.

---

### Step 1 — prd-to-plan: Vertical slices

```
prd-to-plan
"PRD: prd/product-crud-prd.md"
```

Claude produces `plans/product-crud-plan.md`:

```
## Architectural Decisions
- Single Lambda function, HTTP API Gateway proxy integration
- Autofac DI, InstancePerLifetimeScope for services and repositories
- Dapper for DB access, parameterized queries only
- RDS Proxy connection string from AWS SSM Parameter Store
- Soft delete: IsActive flag, no physical DELETE

## Phase 1 — Skeleton
Build Function.cs, ProductModule.cs, ISyncService stub.
dotnet build must pass. No business logic yet.

## Phase 2 — Domain models and repository
ProductEntity, IProductRepository, ProductRepository (Dapper).
Schema migration: products table with indexes.

## Phase 3 — CRUD service layer
IProductService, ProductService.
Create, GetById, List (paginated), Update, SoftDelete.
All return Result<T>.

## Phase 4 — API Gateway routing
Map HTTP method + path to service calls in Function.cs.
Return correct HTTP status codes per PRD.

## Phase 5 — Auth: Cognito group check
Read groups from JWT claims. Enforce Admin vs Viewer per endpoint.

## Phase 6 — Unit tests + integration tests
Unit: ProductService, ProductRepository.
Integration: Testcontainers MySQL, full CRUD flow.
```

---

### Step 2 — do-work: Phase 1 (skeleton)

```
do-work
"Implement Phase 1 from plans/product-crud-plan.md"
```

Claude implements:
- `Function.cs` with Autofac container setup
- `ProductModule.cs` with placeholder registrations
- `IProductSyncService` stub
- Runs `dotnet build` → fixes any errors → reports clean build

**Work Summary (Phase 1):**
```
Problem:      No Lambda entry point exists yet.
What Changed: Function.cs, ProductModule.cs, IProductSyncService.cs created.
Test Results: Build: PASS · Unit: 0 tests (none written yet) · Integration: not run
```

---

### Step 3 — do-work: Phase 2 (repository + schema)

```
do-work
"Implement Phase 2: ProductEntity, IProductRepository, ProductRepository,
 and the MySQL schema migration."
```

Claude implements:

```csharp
// App/Models/Entity/ProductEntity.cs
public sealed class ProductEntity
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price      { get; set; }
    public int    StockCount  { get; set; }
    public bool   IsActive    { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// App/Data/Impl/ProductRepository.cs
public sealed class ProductRepository(
    IDatabaseConnectionProvider connectionProvider,
    ILogger<ProductRepository> logger)
    : BaseRepository(connectionProvider, logger), IProductRepository
{
    public async Task<ProductEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = await GetConnectionAsync();
        return await conn.QueryFirstOrDefaultAsync<ProductEntity>(
            new CommandDefinition(
                "SELECT * FROM products WHERE id = @Id AND is_active = 1",
                new { Id = id },
                cancellationToken: ct));
    }

    public async Task<(IEnumerable<ProductEntity> Items, int TotalCount)> ListActiveAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await GetConnectionAsync();
        var offset = (page - 1) * pageSize;

        var items = await conn.QueryAsync<ProductEntity>(
            new CommandDefinition(
                "SELECT * FROM products WHERE is_active = 1 ORDER BY created_at DESC LIMIT @Size OFFSET @Offset",
                new { Size = pageSize, Offset = offset },
                cancellationToken: ct));

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM products WHERE is_active = 1",
                cancellationToken: ct));

        return (items, total);
    }

    // CreateAsync, UpdateAsync, SoftDeleteAsync follow same pattern
}
```

Schema migration (SQL):
```sql
CREATE TABLE products (
    id          INT          NOT NULL AUTO_INCREMENT,
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    price       DECIMAL(10,2) NOT NULL DEFAULT 0,
    stock_count INT          NOT NULL DEFAULT 0,
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_products_active_created (is_active, created_at)
);
```

Then runs `/migrate` on the SQL file to confirm safety before applying.

---

### Step 4 — /migrate: Safety check before applying schema

```
/migrate
[paste the SQL migration above]
```

Claude confirms:
- No data loss risk (new table)
- Index on (is_active, created_at) — correct for the list query filter + sort
- No NOT NULL columns without defaults on existing tables
- Rollback: `DROP TABLE products`

---

### Step 5 — /code-review BUGS,SECURITY before PR

```
/code-review BUGS,SECURITY
```

Claude flags:

```
Critical
  ProductRepository.cs:34 — GetByIdAsync does not filter by is_active = 1.
  A soft-deleted product is still retrievable by ID.
  Fix: Add AND is_active = 1 to the WHERE clause.

Warning
  Function.cs:67 — HTTP method check uses string comparison ("GET", "POST").
  Recommend converting to enum at entry point to avoid case mismatch bugs.

Suggestion
  ProductRepository.cs:52 — magic number pageSize = 20 in default.
  Extract to ProductConstants.DefaultPageSize.
```

DEV fixes Critical and Warning before raising the PR.

---

## DEV Checklist

```
□ prd-to-plan completed — phases defined before writing any code
□ do-work Work Summary written at end of each phase
□ dotnet build passes — no warnings suppressed
□ dotnet test passes — unit + integration
□ /migrate run on every schema change
□ /code-review BUGS,SECURITY — no Critical issues open
□ PR description generated via /workflow Trigger B
```
