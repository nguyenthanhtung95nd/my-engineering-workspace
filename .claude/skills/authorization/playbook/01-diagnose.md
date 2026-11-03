# 01 — Diagnose

Before changing anything, establish two facts: **where decisions are made today**, and
**whether anyone can explain them**.

## The diagnostic question

> Pick any access decision the system made last week. In under five minutes, answer:
> **who** was allowed · **why** · **which rule** decided it · **who owns** that rule ·
> **when it last changed and for what reason**.

Failing this does not mean something is broken. It means the organisation's ability to
*explain* access has fallen behind its ability to *implement* features. That gap is the real
problem, and it widens silently.

## Find every decision point

Authorization decisions hide in more places than people expect. Search for all of them:

```bash
# Attribute-based gates
rg -n '\[Authorize' --type cs
rg -n 'AllowAnonymous' --type cs

# Role and claim checks scattered in logic
rg -n 'IsInRole|HasClaim|FindFirstValue|ClaimTypes\.' --type cs

# Ownership checks buried in queries — the most commonly missed
rg -n '\.Where\(.*UserId|\.FirstOrDefaultAsync\(.*UserId|TenantId ==' --type cs

# Hand-rolled gates
rg -n 'if \(.*\.(Role|Permission|IsAdmin|CanEdit|Owner)' --type cs

# Paths that bypass middleware entirely — adjust the broker/host names to this project
rg -n 'IHostedService|BackgroundService|Consumer|Worker|Scheduled|Timer' --type cs
```

Record each hit in a table: *file · what it decides · what it reads · who owns it*. The table
itself is usually the finding — when one business rule appears in six places, that is the
diagnosis.

## Stages

Name the stage honestly. Most systems are at stage 1 and that is normal.

| Stage | Name | What it looks like |
|---|---|---|
| **1** | **Local** | Decision logic embedded in features. Static roles, permission tables, ownership checks inside queries, inconsistent enforcement, hard-coded delegation, little visibility |
| **2** | **Coordinated** | Decisions partly pulled out of features. Context signals growing. Ownership emerging. The model is still moving |
| **3** | **Strategic** | Rules evaluated at runtime from one place. One model. Delegation is a capability. Signals have owners. Leadership understands it |

What separates 2 from 3 is not technology. It is **delegation treated as a capability**,
**someone owning each signal**, and **leadership awareness**.

## Red flags, and what each one actually means

| Red flag | What it means |
|---|---|
| Roles encoding *conditions* (`support-temp-tier1`, `regional-sales-north`) | The model is being asked to express something it cannot. Not a role problem — a modelling problem |
| The same rule implemented in two services with different results | No shared decision; two implementations drifting |
| "Temporary" exceptions older than a quarter | No rule could express the real intent |
| Nobody can say *why* someone has access, only *that* they do | Ownership information was never captured |
| A background job reads data no endpoint would return | Enforcement covers HTTP only |
| Permission names mirror UI controls (`canSeeShareButton`) | The interface is dictating the decision model |
| Tenant separation is "we always filter by `TenantId`" | A column is being used as a trust boundary |

## The tenancy check

A `TenantId` column is a **database convention**, not a boundary. It becomes a boundary only
when a missing or wrong filter **cannot** produce data. Test it directly:

```csharp
// If this test can be made to pass by deleting a Where clause somewhere,
// tenancy is a convention, not a boundary.
[Fact]
public async Task GetOrder_WhenOrderBelongsToAnotherTenant_ReturnsNotFound()
{
    var sut = CreateService(currentTenant: "tenant-a");

    var result = await sut.GetByIdAsync(OrderOwnedBy("tenant-b"), CancellationToken.None);

    Assert.False(result.IsSuccess);
}
```

## Output of this step

1. The decision-point table.
2. The stage, named.
3. Whether the diagnostic question can be answered — yes or no, not "partly".
4. The **one** rule that is already causing visible friction. That is where work starts —
   not the most important rule in theory, the one already hurting. It justifies itself.

→ Next: [02-model-selection.md](02-model-selection.md)
