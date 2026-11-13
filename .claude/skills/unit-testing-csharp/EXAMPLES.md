# Worked examples

## Example 1. Mode `write`, characterization: contract for an `InventoryManager`

The production class already exists: `AddItem`, `GetItem`, `UpdateQuantity`, `IsInStock`,
`GetLowStockItems(threshold = 10)`, `GetItemsByCategory` and `DeactivateItem`. Derivation followed
rulebook Section 2.3. Rows marked `?` were inferred from code. They are hypotheses and must be
confirmed as intended behavior before any test is written. Gate 2 is reported as `N/A (characterization)`.

Partition notes. Lookups by SKU have a `not-found` row (a well-formed SKU with no match) separate
from `invalid` rows (null, empty, whitespace SKU, one row each). `GetItem` has `boundary: N/A`
because the rule has no threshold. `GetLowStockItems` has a threshold, so it tests the value at the
threshold and the adjacent values below and above it: 9, 10 and 11.
Filtering and ordering are treated as two rules (R5 and R6) and get separate tests.

| Rule | Given | When | Then | Partition | Test |
|---|---|---|---|---|---|
| R1 | valid item | AddItem | returned, `LastUpdated` set, retrievable | valid | `AddItem_ValidItem_ReturnsItemWithLastUpdatedSet` |
| R1 | null item | AddItem | `ArgumentNullException("item")` | invalid (null) | `AddItem_NullItem_ThrowsArgumentNullException` |
| R1 | SKU "" | AddItem | `ArgumentException("item")` | invalid (empty) | `AddItem_InvalidItem_ThrowsArgumentException` (Theory) |
| R1 | SKU "   " | AddItem | `ArgumentException("item")` | invalid (whitespace) | same Theory |
| R1 | Name "" | AddItem | `ArgumentException("item")` | invalid (empty) | same Theory |
| R1 | Name "   " | AddItem | `ArgumentException("item")` | invalid (whitespace) | same Theory |
| R1 | price -0.01 | AddItem | `ArgumentException("item")` | boundary-below | same Theory |
| R1 | quantity -1 | AddItem | `ArgumentException("item")` | boundary-below | same Theory |
| R1 | price 0, quantity 0 | AddItem | accepted | boundary-at `?` | `AddItem_ZeroQuantityAndPrice_IsAccepted` |
| R1 | SKU already present | AddItem | `InvalidOperationException` "already exists" | invalid (duplicate) | `AddItem_DuplicateSku_ThrowsInvalidOperationException` |
| R2 | well-formed SKU with no match | GetItem | null | not-found | `GetItem_UnknownSku_ReturnsNull` |
| R2 | SKU null | GetItem | `?` to confirm: `ArgumentNullException` or null | invalid (null) | `GetItem_NullSku_...` (name fixed after confirmation) |
| R2 | SKU "" | GetItem | `?` to confirm: null | invalid (empty) | `GetItem_EmptySku_...` |
| R2 | SKU "   " | GetItem | `?` to confirm: null | invalid (whitespace) | `GetItem_WhitespaceSku_...` |
| R3 | existing SKU, quantity 0 | UpdateQuantity | updated, timestamp moves forward | boundary-at | `UpdateQuantity_ExistingSkuValidQuantity_UpdatesQuantityAndTimestamp` (Theory) |
| R3 | existing SKU, quantity 20 | UpdateQuantity | updated, timestamp moves forward | valid | same Theory |
| R3 | existing SKU, quantity -1 | UpdateQuantity | `ArgumentException("quantity")` | boundary-below | `UpdateQuantity_JustBelowZero_ThrowsArgumentExceptionForQuantity` |
| R3 | well-formed SKU with no match | UpdateQuantity | `ArgumentException("sku")` "not found" | not-found | `UpdateQuantity_UnknownSku_ThrowsArgumentExceptionForSku` |
| R3 | SKU null | UpdateQuantity | `?` to confirm: `ArgumentNullException` or "not found" | invalid (null) | `UpdateQuantity_NullSku_...` (name fixed after confirmation) |
| R3 | SKU "" | UpdateQuantity | `?` to confirm: "not found" | invalid (empty) | `UpdateQuantity_EmptySku_...` |
| R3 | SKU "   " | UpdateQuantity | `?` to confirm: "not found" | invalid (whitespace) | `UpdateQuantity_WhitespaceSku_...` |
| R4 | qty 1, active | IsInStock | true | boundary-above-zero | `IsInStock_QuantityAndActiveFlag_ReturnsExpected` (Theory) |
| R4 | qty 0, active | IsInStock | false | boundary-at-zero | same Theory |
| R4 | qty 5, deactivated | IsInStock | false | valid (inactive) | same Theory |
| R4 | well-formed SKU with no match | IsInStock | false | not-found | `IsInStock_UnknownSku_ReturnsFalse` |
| R5 | qty 9, 10, 11 with threshold 10 | GetLowStockItems | 9 and 10 returned, 11 excluded | boundary pair `?` | `GetLowStockItems_ThresholdBoundary_IncludesAtThresholdExcludesJustAbove` |
| R5 | low qty but deactivated | GetLowStockItems | excluded | valid (inactive) `?` | `GetLowStockItems_DeactivatedItem_IsExcluded` |
| R5 | no threshold given | GetLowStockItems | default 10 `?` | valid | `GetLowStockItems_DefaultThreshold_IsTen` |
| R6 | qty 5, 2, 8 all under threshold | GetLowStockItems | ordered 2, 5, 8 `?` | valid (ordering) | `GetLowStockItems_SeveralItems_ReturnsAscendingByQuantity` |
| R7 | "Electronics", "electronics", "ELECTRONICS" | GetItemsByCategory | the same two active items `?` | valid (case) | `GetItemsByCategory_CaseInsensitiveMatch_ReturnsActiveItems` (Theory) |
| R8 | existing SKU | DeactivateItem | `IsActive=false`, timestamp moves forward | valid | `DeactivateItem_ExistingSku_SetsInactiveAndTouchesTimestamp` |
| R8 | well-formed SKU with no match | DeactivateItem | `ArgumentException("sku")` | not-found | `DeactivateItem_UnknownSku_ThrowsArgumentException` |
| R8 | SKU null | DeactivateItem | `?` to confirm: `ArgumentNullException` or "not found" | invalid (null) | `DeactivateItem_NullSku_...` (name fixed after confirmation) |
| R8 | SKU "" | DeactivateItem | `?` to confirm: "not found" | invalid (empty) | `DeactivateItem_EmptySku_...` |
| R8 | SKU "   " | DeactivateItem | `?` to confirm: "not found" | invalid (whitespace) | `DeactivateItem_WhitespaceSku_...` |

Two representative tests from the table. The first covers R5 filtering at the boundary, the second
covers R6 ordering. They are separate because the contract lists them as separate rules.

```csharp
[Fact]
[Trait("Category", "Unit")]
public void GetLowStockItems_ThresholdBoundary_IncludesAtThresholdExcludesJustAbove()
{
    // Arrange
    _manager.AddItem(ValidItem("AT", quantity: 10));
    _manager.AddItem(ValidItem("ABOVE", quantity: 11));
    _manager.AddItem(ValidItem("BELOW", quantity: 9));

    // Act
    var result = _manager.GetLowStockItems(threshold: 10).Select(i => i.Sku).ToList();

    // Assert
    Assert.Contains("AT", result);
    Assert.Contains("BELOW", result);
    Assert.DoesNotContain("ABOVE", result);
}

[Fact]
[Trait("Category", "Unit")]
public void GetLowStockItems_SeveralItems_ReturnsAscendingByQuantity()
{
    // Arrange
    _manager.AddItem(ValidItem("FIVE", quantity: 5));
    _manager.AddItem(ValidItem("TWO", quantity: 2));
    _manager.AddItem(ValidItem("EIGHT", quantity: 8));

    // Act
    var result = _manager.GetLowStockItems(threshold: 10).Select(i => i.Sku).ToList();

    // Assert
    Assert.Equal(new[] { "TWO", "FIVE", "EIGHT" }, result);
}
```

Timestamp note. `LastUpdated` is set from `DateTime.UtcNow` inside the class. On code we own, the fix
is R3.5: inject `TimeProvider` and assert an exact value from `FakeTimeProvider`. On code the test
author cannot change, `Assert.InRange(result.LastUpdated, before, DateTime.UtcNow)` is the correct
bounded assertion. The review records the limitation and does not deduct for it, because injection
is outside the author's control (R2.5).

Gate note. The implementation already exists, so this is characterization, not TDD. Gate 2 is `N/A`,
and the tests become a contract only after a human confirms each `?` row. A pre-existing hand-written
suite for the same class had 14 tests and missed eight of the rows above: the threshold boundary,
ordering, the default threshold, case-insensitivity, inactive exclusion, the null item, the zero
boundary and the timestamp on deactivate. That gap is what the contract step is for.

## Example 2. Mode `review`: a weak suite and its rewrite

Input A:

```csharp
public class Tests
{
    [Fact] public void Test1() { var c = new Calculator(); Assert.Equal(4, c.Add(2, 2)); Assert.Equal(0, c.Add(2, -2)); Assert.Equal(-4, c.Add(-2, -2)); }
    [Fact] public void Test2() { Assert.Throws<Exception>(() => new Calculator().Divide(10, 0)); }
    [Theory] [InlineData(5, 3, 2)] public void Test3(int a, int b, int e) => Assert.Equal(e, new Calculator().Subtract(a, b));
}
```

Input B covers the same behaviors as `Add_WithVariousNumbers_ReturnsCorrectSum` (Theory, four rows),
`Subtract_WithVariousNumbers_ReturnsCorrectDifference` (Theory, three rows) and
`Divide_WithZeroDivisor_ThrowsDivideByZeroException` (Fact), all with AAA comments.

What a full-score version of the Add Theory looks like, with the boundary pair and the `scenario`
column that B is missing:

```csharp
public static TheoryData<int, int, int, string> AddCases => new()
{
    { 2, 2, 4, "two positives" },
    { 2, -2, 0, "opposite signs cancel" },
    { -2, -2, -4, "two negatives" },
    { int.MaxValue, 0, int.MaxValue, "at the upper boundary" },
    { int.MaxValue, 1, int.MinValue, "just above the upper boundary wraps (confirm as intended or reject)" },
};

[Theory]
[MemberData(nameof(AddCases))]
public void Add_TwoIntegers_ReturnsExpectedSum(int a, int b, int expected, string scenario)
{
    // Arrange
    var calculator = new Calculator();

    // Act
    var result = calculator.Add(a, b);

    // Assert
    Assert.True(expected == result, $"{scenario}: expected {expected}, got {result}");
}
```

| Group | A | Deduction (A) | B | Deduction (B) |
|---|---|---|---|---|
| G1 Fundamentals | 0 | R1.2 names `Test1` to `Test3`. R1.3 `Test1` has three Acts. | 2 | |
| G2 Test Design | 0 | Happy values only, no boundary pair, no scenario column. | 1 | R2.2 no boundary pair around the divisor (1 versus 0), no scenario column. |
| G3 Isolation | 2 | Pure logic, correctly nothing mocked. | 2 | |
| G4 Advanced | 0 | R4.1 `Throws<Exception>` accepts anything. | 1 | R4.1 exact type, but the exception is not captured or inspected. |
| G5 Quality | 1 | No AAA, `new Calculator()` repeated in every test. | 2 | |
| G6 Practical | 1 | Runs, no traits. | 1 | Runs, no traits. |
| Total | 4/12 | | 9/12 | |

## Example 3. Mode `review`: Gate 4 catches a flaky suite

```powershell
dotnet test -- --filter-namespace "*.ParallelExecution"
```

Three consecutive runs gave `Failed: 1`, `Failed: 2` and `Failed: 2` out of 9, and the failing test
names differed between runs. Each failure was a `System.IO.IOException: The process cannot access the
file ...`. Gate 4 fails and the status is `BLOCKED`. The review still produces the diagnostic
scorecard, with G5 = 0 under R5.3 because two test classes write the same file from parallel threads
with no shared `[Collection]`, so the team sees what to fix. The fix is the `[Collection("File system")]`
pattern in rulebook Section 3, group G5.

## Example 4. Isolation with recording fakes

`UserService(IUserRepository, IEmailSender)` is tested with an in-memory repository and a recording
email fake. No mocking library is involved. `CreateUserAsync` returns the created user, but the
outcome that matters for this rule is the recorded interaction, which satisfies Gate 3.

```csharp
[Fact]
public async Task CreateUserAsync_ValidInput_SendsExactlyOneWelcomeEmailToThatAddress()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var email = new FakeEmailSender();
    var service = new UserService(repository, email);

    // Act
    await service.CreateUserAsync("Ada", "ada@example.com");

    // Assert
    var sent = Assert.Single(email.Sent);
    Assert.Equal("ada@example.com", sent.To);
    Assert.Equal("Welcome!", sent.Subject);
}
```

Three tests of this shape cover the service: the user is persisted with a generated id (state change),
exactly one welcome email is sent (recorded interaction), and inactive users are filtered (return value).
The fake is asserted on what it recorded, never on how it was called.
