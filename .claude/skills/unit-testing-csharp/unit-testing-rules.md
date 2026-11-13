# Unit Testing Rules (C#, xUnit v3)

Sections 1, 2, 3, 5 and 6 are framework-agnostic engineering practice. Section 4 is xUnit v3 syntax
with a mapping to NUnit and MSTest. Section 7 lists the traps a review checks first. Every rule has
an id (for example `R4.2`) so a review can cite it.

---

## 1. Mindset (five sentences, memorize)

| # | Rule | Practical consequence |
|---|---|---|
| M1 | Tests are executable contracts, not code coverage. Humans define expected behavior, AI may write the implementation, and tests provide the evidence. | No approved contract, no test. Existing code is never the source of business truth. |
| M2 | Red first, then green. | Red counts only when the test compiles and fails at the behavior assertion. Record names and reasons. |
| M3 | One test covers one behavior and has one sentence as its name. | The name reads as the scenario. Nobody needs to open the body. |
| M4 | A failing test must explain itself in plain language. | Assertions carry messages. Theory data carries a `scenario` column. |
| M5 | Never trust a suite that has not produced the same result, by test identity, three times in a row. | Flaky means not done. |

---

## 2. Contract: Given / When / Then

### 2.1 Format and terminology

| Rule | Given (state, input) | When (exactly one action) | Then (observable result) | Partition |
|---|---|---|---|---|
| R1 | Gold customer, subtotal 500 | DiscountRate | 0.15 | boundary-at |
| R1 | Gold customer, subtotal 499.99 | DiscountRate | 0.10 | boundary-below |
| R2 | item with empty SKU | AddItem | ArgumentException, ParamName "item" | invalid |
| R3 | order id that exists | GetOrderById | the order | valid |
| R3 | well-formed order id with no matching order | GetOrderById | null | not-found |
| R3 | order id null | GetOrderById | ArgumentNullException | invalid |
| R3 | none | GetOrderById | none | boundary: N/A, the rule has no threshold |

- One contract row is one test case (one data row).
- Rows with the same behavior and different data are grouped into one `[Theory]` method.
- Rows with different behaviors get separate test methods.
- A method name is derived from its rows as `Method_Scenario_ExpectedResult`.

```csharp
// three R1-style rows, one behavior, one method
[Theory]
[InlineData(2, 2, 4)] [InlineData(2, -2, 0)] [InlineData(-2, -2, -4)]
public void Add_TwoIntegers_ReturnsExpectedSum(int a, int b, int expected)
{
    // Arrange
    var calculator = new Calculator();

    // Act
    var result = calculator.Add(a, b);

    // Assert
    Assert.Equal(expected, result);
}
```

### 2.2 Partitions, as applicable

| Partition | Required when | Otherwise |
|---|---|---|
| valid | always | none |
| invalid | the rule rejects some input or state | `N/A` with a reason |
| not-found | the operation looks up an entity that may be absent. A well-formed key with no match is a legitimate state, not invalid input. | `N/A` with a reason |
| boundary (a pair: at the threshold and just beside it) | the rule defines a numeric or size threshold | `N/A` with a reason |
| null, empty, whitespace | the input is a string, a collection or a reference | separate rows, one per case, never folded into "invalid" |
| invariant | the class holds state across calls and a property must hold after any sequence of operations (stock never negative, balance equals the sum of entries, a collection stays sorted) | `N/A` with a reason |
| idempotent | an operation can be repeated or retried (apply a command twice, re-run a reconciliation) and the contract says the second call must change nothing | `N/A` with a reason |

An invariant row reads "Given any sequence of <operations>, Then <property> holds" and its test performs
several operations before the assertion. An idempotent row reads "When <operation> is applied twice with
the same input, Then the state equals the state after one application". Both rows name the property, never
an implementation.

Never invent a business rule to fill a partition. A row the AI inferred from code rather than from an
acceptance criterion is a hypothesis and is marked `?`. Code shows what the system does, not what it should do.

### 2.3 Deriving a contract when no acceptance criteria exist

1. List every `if`, `throw`, `switch` and numeric threshold in the public method. Each is a candidate rule.
2. For each rule, fill the partitions that apply (Section 2.2). Write `N/A` and the reason for the rest.
3. Mark every row that comes from code rather than from a requirement with `?`.
4. Present the table for approval. Do not write a test until the table is approved.

---

## 3. Rules by competency group

### G1. Testing Fundamentals

| # | Rule |
|---|---|
| R1.1 | Use AAA with the three comments `// Arrange`, `// Act`, `// Assert`. Keep the comment even when Arrange is empty. |
| R1.2 | Name tests `Method_Scenario_ExpectedResult`. Scenario states the condition and Expected states the outcome. A name like `Test1` scores 0. |
| R1.3 | One Act per test, and one behavior per test. Several Asserts are acceptable when they describe one outcome. Judge by behavior, not by counting Asserts. If the contract treats filtering and ordering as separate rules, they get separate tests. |
| R1.4 | Use `[Fact]` for a distinct business scenario and `[Theory]` for the same behavior with different data. |
| R1.5 | The test tree mirrors the production tree: `tests/X.Tests/Orders/OrderServiceTests.cs` matches `src/X/Orders/OrderService.cs`. |
| R1.6 | Repeated Arrange becomes a helper such as `CreateCustomer(name = "Ada")` or a builder such as `TestData.Product().WithPrice(10m)`. |
| R1.7 | Tests read without comments. Comment only to explain a surprising number, for example `// 100 + 3% fee`. |

```csharp
// R1.3, wrong: three behaviors in one Fact
[Fact] public void Test1() { Assert.Equal(4, calc.Add(2, 2)); Assert.Equal(0, calc.Add(2, -2)); Assert.Equal(-4, calc.Add(-2, -2)); }
// right: see Section 2.1, one Theory with three rows
```

### G2. Test Design

| # | Rule |
|---|---|
| R2.1 | Every business rule gets the partitions that apply to it (Section 2.2). A happy path alone, where invalid or boundary cases exist, scores 0. |
| R2.2 | Every numeric threshold gets a pair of tests: at the threshold and just beside it. For `age` valid in `[0, 150]`, test -1, 0, 150 and 151. |
| R2.3 | Null, empty and whitespace-only are three separate data rows. |
| R2.4 | Theory data ends with a `scenario` column so the failure message is readable. |
| R2.5 | Values produced by `static` counters, `Random` or `DateTime.Now` are never asserted with `Equal`. Inject `TimeProvider` (R3.5). On code the test author cannot change, use `InRange`, `Matches` or another bounded assertion, and record the limitation. No deduction applies when injection is outside the author's control. |
| R2.6 | Apply equivalence partitioning: one representative per class, not ten values from the same class. Thirteen FizzBuzz Facts are one four-row Theory. |

```csharp
// R2.2 and R2.4: boundary pair with a scenario column
public static TheoryData<decimal, decimal, string> GoldDiscount => new()
{
    { 500m,    0.15m, "at threshold: premium rate" },
    { 499.99m, 0.10m, "just below threshold: base rate" },
};
```

### G3. Test Isolation and Mocking

| # | Rule |
|---|---|
| R3.1 | Dependencies go through interfaces and are injected through the constructor, for example `UserService(IUserRepository, IEmailSender)`. |
| R3.2 | Prefer hand-written in-memory fakes that record interactions so the test can assert on them. |
| R3.3 | No real I/O in a unit test. For the file system, create a `Guid` temp directory in the constructor and delete it in `Dispose`. |
| R3.4 | Do not mock what has no external dependency. Pure logic is tested directly. |
| R3.5 | Time and randomness are dependencies. Inject `TimeProvider` (use `FakeTimeProvider` in tests) or a `today` parameter. |
| R3.6 | Use a mocking library (NSubstitute) only when a hand-written fake grows past roughly 20 lines or call order must be verified. This is a heuristic, not a law. |
| R3.7 | Tests touching a real database or HTTP are integration tests. Mark them `[Trait("Category","Integration")]`, give them their own fixture, and make them skippable by environment. |

Terminology. A stub returns canned data. A fake has a simple working implementation such as an in-memory list.
A mock records calls and verifies them. Name each one by what it does.

```csharp
// R3.2: a recording fake
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject)> Sent { get; } = new();
    public Task SendAsync(string to, string subject, string body) { Sent.Add((to, subject)); return Task.CompletedTask; }
}
// in the test
var sent = Assert.Single(emailSender.Sent);
Assert.Equal("ada@example.com", sent.To);
```

### G4. Advanced Testing

| # | Rule |
|---|---|
| R4.1 | Capture the exception with `var ex = Assert.Throws<T>(...)`, then assert `ParamName`, `Message` (with `Contains`) and custom properties. `Throws` requires the exact type. Use `ThrowsAny` to accept subclasses. |
| R4.2 | For async code use `await Assert.ThrowsAsync<T>(() => svc.DoAsync())`. A missing `await` makes the test pass vacuously. |
| R4.3 | For nested exceptions use `Assert.NotNull(ex.InnerException)` and `Assert.IsType<T>(ex.InnerException)`. |
| R4.4 | Data ladder: `[InlineData]` for primitives, `TheoryData<T1,T2,..>` for objects or type safety, `ClassData` when shared across classes. Do not add new `IEnumerable<object[]>` sources. |
| R4.5 | After division, compare `double` or `decimal` with `Assert.Equal(expected, actual, precision: n)`. |
| R4.6 | Async methods that accept a `CancellationToken` receive the test framework's token. A timeout attribute only stops a test whose code honors the token. |
| R4.7 | Test cancellation with `CancellationTokenSource(TimeSpan)` and `ThrowsAnyAsync<OperationCanceledException>`. |
| R4.8 | Every skip has a reason, for example `Assert.SkipUnless(cond, "Set SMTP_HOST to run")`. |
| R4.9 | For collections, `Assert.Single` returns the element for further asserts. Use `Contains(list, pred)`, `All(list, x => Assert...)`, `Empty` and `NotEmpty`. Never write `Assert.True(list.Count() > 0)`. |

```csharp
// R4.1: inspect the exception, do not just catch it
var ex = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100m));
Assert.Equal(50m, ex.AvailableBalance);
Assert.StartsWith("Insufficient funds", ex.Message);
```

### G5. Test Quality and Maintainability

| # | Rule |
|---|---|
| R5.1 | Pick the narrowest fixture tier that works, in this order: constructor and `IDisposable` (per test), `IAsyncLifetime` (per test, async), class fixture (per class), collection fixture (several classes). |
| R5.2 | A shared fixture is read-only. A test that must write uses its own key (own SKU, own email) or a narrower tier. Asserting `initialCount + 1` on shared state is order-dependent. |
| R5.3 | Classes that touch a shared resource (file, port, database) share one `[Collection("name")]`. Never disable parallelization for the whole assembly. |
| R5.4 | No custom test orderer. Order-dependent tests are a design defect. |
| R5.5 | Assertions carry context, for example `Assert.True(cond, $"order {order.Id} should be confirmed, was {order.Status}")`. Log with `ITestOutputHelper`, never with `Console.WriteLine`. |
| R5.6 | A group of asserts repeated three or more times becomes a custom assertion extension such as `order.ShouldBeConfirmed()`. |
| R5.7 | A unit test runs in under one second, and the suite is stable across three consecutive runs by test identity. `Thread.Sleep` in a test is a smell. |
| R5.8 | One responsibility per test class. The class name is the production class name plus `Tests`. |

```csharp
// R5.3, wrong: two classes write the same file in parallel and fail at random
public class FileManagerTests { [Fact] public void Create() => File.WriteAllText("test.txt", "a"); }
public class ArchiveTests     { [Fact] public void Create() => File.WriteAllText("test.txt", "b"); }

// right: the same collection runs them sequentially, and everything else stays parallel
[Collection("File system")] public class FileManagerTests { ... }
[Collection("File system")] public class ArchiveTests     { ... }
```

### G6. Practical Application

| # | Rule |
|---|---|
| R6.1 | Mark tests `[Trait("Category", "Unit")]` or `[Trait("Category", "Integration")]`. Add `Performance=Slow` or `Feature=...` when filtering needs them. |
| R6.2 | One command from the root: `dotnet test`. Filter and report flags depend on the runner. See Section 4.3. |
| R6.3 | Coverage is a diagnostic, not a gate, and it is measured by the workflow in Section 6, never estimated. A review lists uncovered branches by name and asks whether a business rule lives there. A numeric threshold exists only if the project declares one. |
| R6.4 | Minimal CI is restore, build, test, with `--no-build` on the test step. |
| R6.5 | Environment-dependent tests use a custom attribute deriving from `FactAttribute` that sets `Skip` in its constructor. Extract it to a shared library when several projects use it. |
| R6.6 | `xunit.runner.json` declares `culture`, so currency and date tests do not drift between machines, and `maxParallelThreads`. |
| R6.7 | Add `[assembly: CaptureConsole]` when production code writes to the console and that output must appear in test output. |

---

## 4. Framework layer

### 4.1 xUnit v3 idioms

```csharp
// Theory with type-safe data
public static TheoryData<decimal, decimal, decimal, string> Cases => new()
{
    { 100m, 10m, 10m, "10% of 100" },
};
[Theory] [MemberData(nameof(Cases))]
public void CalculatePercentage_Value_ReturnsExpected(decimal v, decimal p, decimal expected, string scenario)
    => Assert.Equal(expected, sut.CalculatePercentage(v, p), precision: 3);

// Exceptions
var ex = Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(-1m));
Assert.Equal("amount", ex.ParamName);
var exAsync = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateUserAsync(email));

// Fixture (per class, async)
public class DbFixture : IAsyncLifetime
{
    public async ValueTask InitializeAsync() { /* expensive, runs once */ }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
public class RepoTests(DbFixture fx) : IClassFixture<DbFixture> { }

// Several classes share a fixture and run sequentially
[CollectionDefinition("Shared db")] public class SharedDb : ICollectionFixture<DbFixture> { }
[Collection("Shared db")] public class ATests { }

// Skip, timeout, token
Assert.SkipUnless(Environment.GetEnvironmentVariable("CI") is null, "Local only");
[Fact(Timeout = 2000)]
public async Task Process_Completes() => await sut.ProcessAsync(TestContext.Current.CancellationToken);

// Time injection
var clock = new FakeTimeProvider(new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero));
var sut = new CouponService(clock);

// Environment attribute
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute() { if (!OperatingSystem.IsWindows()) Skip = "Windows only"; }
}

// Output
public class XTests(ITestOutputHelper output) { /* output.WriteLine(...) */ }
```

xUnit v2 differences. `Assert.Skip*` exists only in v3. The v3 `IAsyncLifetime` returns `ValueTask`.
`TestContext.Current` exists only in v3. A v3 test project is `OutputType=Exe` on Microsoft Testing Platform.

### 4.2 Mapping to NUnit and MSTest

| Concept | xUnit v3 | NUnit | MSTest |
|---|---|---|---|
| Single test | `[Fact]` | `[Test]` | `[TestMethod]` |
| Data-driven | `[Theory]` with `[InlineData]` or `TheoryData<>` | `[TestCase]` or `[TestCaseSource]` | `[DataTestMethod]` with `[DataRow]` or `[DynamicData]` |
| Per-test setup and teardown | constructor and `IDisposable` | `[SetUp]` and `[TearDown]` | `[TestInitialize]` and `[TestCleanup]` |
| Per-class setup | `IClassFixture<T>` | `[OneTimeSetUp]` | `[ClassInitialize]` |
| Shared across classes | `[Collection]` with `ICollectionFixture<T>` | `[SetUpFixture]` | `[AssemblyInitialize]` |
| Expect exception | `Assert.Throws<T>` (returns it) | `Assert.Throws<T>` (returns it) | `Assert.ThrowsException<T>` (returns it) |
| Skip with reason | `Assert.Skip(reason)` or `[Fact(Skip=)]` | `Assert.Ignore(reason)` or `[Ignore]` | `Assert.Inconclusive(reason)` or `[Ignore]` |
| Category | `[Trait("Category","Unit")]` | `[Category("Unit")]` | `[TestCategory("Unit")]` |
| Parallel control | `[Collection]`, `maxParallelThreads` | `[NonParallelizable]`, `[Parallelizable]` | `[DoNotParallelize]` |
| Output | `ITestOutputHelper` | `TestContext.Out` | `TestContext.WriteLine` |

### 4.3 Run, filter, report

| Runner | Filter by category | Report |
|---|---|---|
| xUnit v3 on Microsoft Testing Platform | `dotnet test -- --filter-not-trait "Category=Slow"` | `dotnet test -- --report-xunit-trx` |
| VSTest (xUnit v2, NUnit, MSTest) | `dotnet test --filter "Category!=Slow"` for xUnit v2, `"TestCategory!=Slow"` for NUnit and MSTest | `dotnet test --logger trx` |

---

## 5. Review: gates, scorecard, output template

### 5.1 Gates

Gates are numbered Gate 1 to Gate 4. Scorecard groups are G1 to G6. The two sets are never mixed.

A failed gate sets `Status: BLOCKED`. The review continues to the scorecard as a diagnostic. The
scorecard never overrides a gate.

| Gate | Check | Passes when | Evidence |
|---|---|---|---|
| Gate 1 | Contract coverage | Every approved contract row maps to a test case. | Contract table with a "Test" column. |
| Gate 1 note | Contract source | The contract comes from approved acceptance criteria or a table the user approved. Existing tests are never the contract. Without an approved contract Gate 1 is BLOCKED and a proposed contract with `?` rows is attached for confirmation. | Reference to the approved contract. |
| Gate 2 | Red evidence | TDD: tests compiled and failed at the behavior assertion, or at a `NotImplementedException` thrown by an unimplemented production placeholder, before implementation. Characterization: reported as `N/A (characterization)`, never as a pass. If the suite's origin cannot be established, report the missing evidence. Do not assume TDD. Compile errors and infrastructure errors are not evidence. | Red-run log with test names and failure reasons. |
| Gate 3 | Meaningful assertions | Every test asserts on an observable outcome caused by its Act: a return value, a state change, an exception, an emitted event, or an interaction recorded by a fake. Any assertion style the project uses counts (`Assert.*`, `.Should()`, `.ShouldBe()`). `Assert.True(true)`, or an assertion unrelated to the Act, counts as assertion-less. Every skip has a reason. | List of tests without a meaningful assertion. |
| Gate 4 | Stability | Three consecutive runs produce the same passed, failed and skipped test names and the same failure reasons. Counts alone are not enough. | Three run logs kept with the review. |

### 5.2 Scorecard, six groups scored 0, 1 or 2

The score is a diagnostic of practice quality. It is not a probability that the software is correct,
and it never overrides a failed gate.

| Group | 0 | 1 | 2 |
|---|---|---|---|
| G1 Fundamentals | No AAA, names like `Test1`. | AAA present, but names do not state the scenario, or several Acts per test. | AAA, `Method_Scenario_Expected`, one behavior per test, Fact and Theory chosen correctly. |
| G2 Test Design | Happy path only where invalid or boundary cases exist. | Invalid cases present, boundary pairs missing where thresholds exist. | Every applicable partition covered, null, empty and whitespace separated, data has `scenario`, `N/A` justified. |
| G3 Isolation | Real database, HTTP, clock or file. | Interfaces exist but the fake leaks state, or something without a dependency is mocked. | Fakes behind interfaces, no real I/O, time injected, no needless mocks. |
| G4 Advanced | No exception or async tests. | `Throws` without inspecting the exception, `ThrowsAsync` without `await`, or `object[]` data. | `Throws` with properties, async correct, `TheoryData<>`, `precision` for reals, cancellation token. |
| G5 Quality | Order-dependent, orderer, `Sleep`. | Correct fixture tier but shared mutable state, or context-free asserts. | Narrowest fixture tier, no shared mutable state, assert messages, custom assertions where repeated. |
| G6 Practical | Cannot run with one command. | Runs, but no traits and no report. | Traits for CI filtering, report generated, coverage reported and read. |

Total is out of 12. Every deduction cites `file:line` and the rule id, for example `R5.2`.

### 5.3 Rework and coverage

Every test edit after implementation started is logged and classified.

| Class | Meaning | Verdict |
|---|---|---|
| Legitimate correction | The test setup or assertion was wrong. Behavior unchanged. | Accepted after review. |
| Contract change | The requirement was misunderstood. The contract row is updated. | Accepted after approval of the new row. |
| Test weakening | An assertion was loosened or removed to let a bug pass. | Rejected. |
| Implementation-driven rework | The test was rewritten to follow the implementation. | Counted. This is the number that should trend down. |

Rework equals (implementation-driven rework plus test weakening) divided by total tests. Report all four counts.
Count only from Git history, diffs or a recorded change log. Without such evidence report
`Rework: NOT AVAILABLE`. Never infer past edits from the current test code.

Coverage is collected and analyzed by the workflow in Section 6. The review reports uncovered branch
names with one question per branch (is there a business rule here?), never a bare percentage.

### 5.4 Output template (produce exactly this)

```
## Review: <project>, <date>
Mode: TDD | characterization
Command: dotnet test ...

Gates
Gate 1 PASS  18/18 rows mapped
Gate 2 FAIL  no red-run evidence            (or: N/A characterization, 6 rows marked ? awaiting confirmation)
Gate 3 PASS
Gate 4 PASS  3 identical runs by test identity (20 passed, 1 skipped, run logs attached)
Status: BLOCKED | PASS

Scorecard (diagnostic, does not override gates)
| Group | Score | Deducted for |
|---|---|---|
| Group G1 Fundamentals | 2 | |
| Group G2 Test Design | 1 | R2.2 no boundary pair for threshold=10 at InventoryManagerTests.cs:118 |
| ... | | |
Total: 10/12

Rework: legitimate 1, contract change 1 (approved), weakening 0, implementation-driven 2. Ratio 2/21 (9.5%)
        (or: NOT AVAILABLE, no change history for the test project)
Unrequested: <public members, parameters or branches in the change with no contract row, or none>

Coverage (diagnostic)
Status: MEASURED | NOT MEASURED (reason: ...)
Runner and collector: Microsoft Testing Platform, Microsoft.Testing.Extensions.CodeCoverage
Command: dotnet test -- --coverage --coverage-output-format cobertura --results-directory ./TestResults
Report: tests/<project>/TestResults/<guid>.cobertura.xml (HTML: coveragereport/index.html)
Line 80% (80/100), Branch 60% (30/50), Method: not provided by the report
Uncovered branches:
- PaymentService.ProcessPayment, PaymentService.cs:35: payment declined. Business rule? (ask)
- PaymentService.Refund, PaymentService.cs:52: negative refund amount. Business rule? (ask)
Contract rows without a test: none

Next actions (max 3): ...
```

---

## 6. Coverage workflow

Coverage is measured evidence, not an estimate. The AI never reports a coverage number that did not
come from a report file produced in the current session.

### 6.1 Detect the runner, then choose the command

| Runner | How to recognize it | Collector | Command |
|---|---|---|---|
| Microsoft Testing Platform (xUnit v3 with `UseMicrosoftTestingPlatformRunner`, MSTest or NUnit on MTP) | `OutputType=Exe` plus the MTP properties in the test csproj | `Microsoft.Testing.Extensions.CodeCoverage` package | `dotnet test -- --coverage --coverage-output-format cobertura --results-directory ./TestResults` |
| VSTest (xUnit v2, NUnit, MSTest, xUnit v3 without MTP) | Classic test project with `Microsoft.NET.Test.Sdk` and no MTP properties | `coverlet.collector` package | `dotnet test --collect:"XPlat Code Coverage"` |

If the collector package is missing, report `Coverage status: NOT MEASURED` with the reason and the
exact package to add. Do not add packages or tools without asking. Installing tools on a managed
machine may require approval from the organization's IT or platform team.

### 6.2 Collect

1. Run the full suite with collection enabled, once per test project and target framework.
2. Locate the output. Without `--results-directory`, MTP writes to the build output folder,
   `bin/<Configuration>/<tfm>/TestResults/<guid>.cobertura.xml`. With `--results-directory ./TestResults`
   it writes to the test project's `TestResults/` folder. VSTest writes
   `TestResults/<guid>/coverage.cobertura.xml` under the test project.
3. Keep the file with the review. The file is the evidence. Quote `line-rate` and `branch-rate`
   from the XML root element, converted to percentages. Report method coverage only when the report
   or its generated summary provides it. Never derive or estimate a metric that is absent.

### 6.3 Report

The AI reads the Cobertura XML (or a `TextSummary`) directly. The HTML report is for humans.
Generate it with ReportGenerator when a human will read the result:

```powershell
# project-scoped tool manifest, restorable in CI, preferred over a global install
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
dotnet reportgenerator "-reports:tests/**/*cobertura*.xml" "-targetdir:coveragereport" "-reporttypes:Html;TextSummary"
```

### 6.4 Analyze

1. Read line and branch coverage, and method coverage when provided. Branch coverage is the one that
   matters for business rules. A method with one `if` can show 100% line coverage with only one
   branch executed.
2. List every uncovered branch as `Class.Method, file:line, condition`.
3. Open the source at each location and compare with the approved contract.
4. For each uncovered branch, ask one question: is there a business rule here? If yes and no contract
   row exists, the contract is incomplete. Propose the row, do not write the test until it is approved.
5. Recommend new tests only from approved or human-confirmed rows.

### 6.5 Limits the AI must state

- A suite-level report shows which lines ran during the whole run. It does not show which test
  covered which line. Per-test traceability needs a dedicated tool and is out of scope here.
- A covered line is not a verified line. Gate 3 checks that assertions are meaningful. Coverage and
  Gate 3 are evaluated independently and never substitute for each other.
- Coverage of generated code, test code and third-party code is excluded before reading numbers.

```csharp
// one call with amount = 600 gives 100% line coverage of the first branch and 0% of the second
public decimal CalculateDiscount(decimal amount)
{
    if (amount >= 500)
        return amount * 0.15m;

    return amount * 0.10m;
}
```

---

## 7. Common traps (what a review looks for first)

| Trap | Rule violated | Effect |
|---|---|---|
| `Test1`, `Test2`, `Test3`, several expectations per test, `Throws<Exception>` | R1.2, R1.3, R4.1 | G1 = 0, G4 = 0 |
| Two classes write the same file without a shared `[Collection]` | R5.3 | G5 = 0, and Gate 4 usually fails first |
| A test writes into a shared fixture and asserts `initialCount + 1` | R5.2 | G5 = 1 |
| Custom test orderer plus `Thread.Sleep` | R5.4, R5.7 | G5 = 0 |
| Display name or test name contradicts the body | R1.2 | G1 = 1 |
| Timeout attribute on synchronous blocking code | R4.6 | G4 = 1 |
| Test green on first run with no implementation change, presented as TDD | M2 | Gate 2 fails. It is a characterization test. |
| Red run caused by a compile error or an unreachable database | M2 | Not evidence. Gate 2 stays open. |
| `Assert.True(true)`, or an assertion on a value the Act did not produce | M4 | Gate 3 fails. |
| A missing entity classified as invalid input | Section 2.2 | Partition corrected to not-found. The test name and expectation are rewritten. |
| A coverage percentage stated without a report file from the current run | Section 6 | Rejected. Status becomes NOT MEASURED until a collector runs. |
| A green suite reviewed as TDD with no red-run record | Gate 2 | Evidence reported as missing. No pass. |
| Existing tests used as the contract for Gate 1 | M1 | Gate 1 BLOCKED. A proposed contract with `?` rows is attached for confirmation. |
| A rework ratio reported without change history | Section 5.3 | Replaced by NOT AVAILABLE. |
| Line coverage quoted while branch coverage is ignored | Section 6.4 | Review incomplete. Uncovered branches must be listed by name. |
| Partition filled with an invented rule to make the table complete | M1 | Contract rejected. Remove the row. |
| A public member, parameter or branch in the change that no contract row asks for | M1 | Unrequested functionality. Listed on the `Unrequested:` line. Removed, or a row is proposed and approved first. |
| The same failing test, or the same fix, attempted a third time without a new classification | Section 5.3 | Stop. Treated as a contract ambiguity until the row is rewritten and approved. |
| Structural changes (rename, extract, move) mixed into the commit that changes behavior | Section 5.3 | Rework cannot be classified. Structural changes go in their own commit with the suite green before and after. |
