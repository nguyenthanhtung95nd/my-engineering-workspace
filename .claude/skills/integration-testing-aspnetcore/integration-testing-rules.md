# Integration Testing Rules (ASP.NET Core)

This rulebook covers tests that exercise an ASP.NET Core application through its HTTP boundary with
real infrastructure behind it. It is self-contained. Every rule has an id (for example `R3.2`) so a
review can cite it.

Rules are defaults, not laws. A test may deviate from a rule when it states the reason in one line
next to the deviation, and the review records the reason instead of a deduction. Gates (Section 5.1)
and execution boundaries (Section 6.4) have no exceptions.

Scope. This rulebook is specific to ASP.NET Core applications exercised through HTTP, including the
hosted services such an application runs. Sections 1, 2, 3, 5, 6, 7, 8 and 9 are test-framework
agnostic (xUnit, NUnit, MSTest). Section 4 is reference syntax for `WebApplicationFactory`,
Testcontainers and WireMock on xUnit, with notes for other versions and frameworks. Other platforms
(serverless functions, message-driven workers outside ASP.NET Core) need their own adapter and are
out of scope here.

**Position in the delivery pipeline.** Requirements become a contract (Section 2). The contract
becomes tests before the endpoint exists (Section 3 and Section 4). AI or a human implements until
the tests are green, and every failure on the way is classified before anything is changed (Section
5.4). The review (Section 5) checks the tests, the implementation and every change made to production
code for testability. Verification is three identical runs (Gate 4) plus measured coverage (Section 8).
Observability is part of the contract when the contract names it (R6.6). Trust is assessed from
evidence only, and the work is done only when the definition of done (Section 5.6) is met.

---

## 1. Mindset (five sentences, memorize)

| # | Rule | Practical consequence |
|---|---|---|
| M1 | An integration test is an executable contract of the HTTP boundary: humans define the expected conversation, AI may write the implementation, the test proves the pieces work together through the real pipeline. | No approved contract, no test. Routing, binding, validation, middleware, serialization and the database are real. The test asserts the observable contract, never implementation details. |
| M2 | Red first for new behavior, `?` rows for existing behavior. | A failure caused by Docker, a missing image, a port or a compile error is not red. Characterization never claims red. |
| M3 | Real where you own it, faked where you do not, including the failure modes of what you fake. | The database and the pipeline are real. Third-party APIs are WireMock stubs that also answer 403, 500 and timeouts. |
| M4 | Every test owns its infrastructure and its data: it starts what it needs, seeds what it needs, cleans up on failure, and reports what it could not get. | Containers live in the fixture. Unique data per test. `NOT AVAILABLE` with a reason instead of a silent pass. |
| M5 | Trust is measured, never felt: three identical runs by test identity, rework from change history, coverage from a report file. | Counts alone are not stability. A number without a file behind it is rejected. |

---

## 2. Contract: Given / When / Then for HTTP and state

### 2.1 Format

| Req | Rule | Given (seeded state and request) | When (exactly one HTTP call) | Then (status, body, state, side effects) | Partition |
|---|---|---|---|---|---|
| REQ-12 | R1 | valid request, GitHub user exists in the fake | POST /customers | 201, body equivalent to the request plus an Id, `Location` points to GET /customers/{id}, GET returns the same customer, the fake received one call to `/users/{username}` | valid |
| REQ-12 | R1 | request with email "not-an-email" | POST /customers | 400, `ValidationProblemDetails.Errors["Email"][0]` equals "not-an-email is not a valid email address", nothing persisted, the fake received no call | invalid |
| REQ-14 | R1 | request whose GitHub user the fake does not know | POST /customers | 400, `Errors["GitHubUsername"][0]` names the username | upstream says no |
| REQ-14 | R1 | request whose GitHub user the fake answers 403 for | POST /customers | 500, nothing persisted | upstream failure |
| REQ-20 | R2 | a customer created in this test | GET /customers/{id} | 200, body equivalent to the created customer | valid |
| REQ-20 | R2 | well-formed Guid with no customer | GET /customers/{id} | 404, `ProblemDetails.Title` "Not Found" | not-found |
| REQ-20 | R2 | id "abc" | GET /customers/abc | 404 from the route constraint `?` (to confirm, could be 400 by design) | invalid (malformed key) |
| REQ-31 | R3 | a customer created in this test | DELETE /customers/{id} | 200, GET /customers/{id} then returns 404 | valid |

- `Req` is the requirement or ticket id when one exists, otherwise `none`. It makes every test
  traceable to the requirement it protects and lets a requirement change point at the tests to revisit.
- Given names both the state the test seeds and the request it sends. Seeded state is created through
  the API when possible, so that the test depends only on the public contract.
- When is one interaction with the boundary under test: one HTTP call for an endpoint, or one message
  delivered to a consumer hosted inside the same application. Reading state back after the call
  belongs to Then.
- Then lists the observables the scenario needs: status code, body (deserialized to the contract type),
  headers that carry meaning (`Location`), persisted state (a follow-up GET or a direct query), and
  side effects recorded by a fake (the mock received one call, or none). Not every row needs every
  observable (R2.1).
- One row is one test case. Rows with the same behavior and different data become one `[Theory]`
  with a `scenario` column (R2.9). Method names state the operation, the scenario and the expected
  result, for example `Create_ReturnsValidationError_WhenEmailIsInvalid` (R1.7).

### 2.2 Partitions, as applicable

| Partition | Required when | Typical Then | Otherwise |
|---|---|---|---|
| valid | always | 200 or 201, body, state readable back | none |
| invalid | the endpoint validates input | 400 with `ValidationProblemDetails`, one row per field rule, nothing persisted | `N/A` with a reason |
| not-found | the route takes a key | 404 for a well-formed key with no match. A malformed key is a separate row and may be 404 or 400 | `N/A` with a reason |
| upstream failure | the endpoint calls another service | the status the contract defines (often 500 or 502), the fake set up to fail, nothing persisted | `N/A` with a reason |
| boundary pair | a field rule has a threshold (date not in the future, length limit) | one row at the threshold, one just past it | `N/A` with a reason |
| null, empty, whitespace | a request field is a string | one row each, the expected error key named | separate rows, never folded into "invalid" |
| unauthenticated | the endpoint requires a caller identity | the status the contract defines (usually 401), no body with data, no side effect | `N/A` with a reason |
| forbidden | the endpoint has roles, policies or ownership rules | the status the contract defines (usually 403), no side effect. One row per policy, plus one object-level row (another user's resource) and one tenant-isolation row (another tenant's data) when the model has owners or tenants | `N/A` with a reason |
| sensitive fields | the response type carries fields some callers must not see (password hash, internal ids, other users' data) | the field is absent from the body for that caller | `N/A` with a reason |
| overposting | the request type shares a class with the entity or carries fields the caller must not set (`Id`, `Role`, `CreatedBy`) | the protected field is unchanged after the call | `N/A` with a reason |
| concurrency and idempotency | the contract or the risk level (Section 7) names it: payments, unique constraints, retried commands | two concurrent identical requests produce one record, or a retried request with the same idempotency key produces one side effect | `N/A` with a reason |
| timeout and partial failure | the endpoint calls a dependency that can hang or fail halfway, or writes to more than one table or system | the fake delays past the client timeout or fails on the second call, the API answers the status the contract defines, the transaction rolled back and nothing is half-written | `N/A` with a reason |

Never invent a rule to fill a partition. A row inferred from code rather than from an acceptance
criterion is a hypothesis and is marked `?`. The validator and the middleware show what the API does,
not what it should do. Security rows do not assume one universal answer: the approved contract says
whether an unauthenticated call gets 401, 404 or a redirect.

### 2.3 Deriving a contract from an existing API

1. List the endpoints from the controller or the `Map*` calls: verb, route, request type, response type.
   For each one derive the effective authorization requirement from everything that can impose it:
   `[Authorize]` and `[AllowAnonymous]` attributes, the fallback policy, endpoint conventions
   (`RequireAuthorization`), authorization middleware, and resource-based handlers. A grep for
   `[Authorize]` alone misses the fallback policy and the conventions.
2. For each endpoint list every `return NotFound()`, `return BadRequest()`, thrown exception that a
   middleware turns into a status, validator rule and value-object `Validate()`. Each is a candidate row.
   Note the order of checks: a not-found check that runs before validation changes the answer for
   "unknown id with invalid body" and is a row of its own.
3. For each outbound dependency (database, named `HttpClient`, message bus) list the outcomes the
   code distinguishes (found, missing, 403, timeout). Each is a candidate row in the upstream partition.
4. For each request and response type compare its members with the entity. Shared classes and
   settable server-owned members are candidate overposting and sensitive-field rows.
5. Fill the partitions that apply (Section 2.2). Write `N/A` and the reason for the rest.
6. Mark every row that comes from code rather than from a requirement with `?`.
7. Present the table for approval. No test is written until the table is approved.

Two approvals, not one. **Contract approval** happens before any test: the user approves the rows as
the scenarios to verify, and a `?` row is approved as a hypothesis, not as a requirement. **Hypothesis
confirmation** happens after the characterization run: the user looks at what the API actually
answered for each `?` row and decides whether that behavior is the contract or a defect to fix. A
green run never promotes a `?` row by itself. Until confirmation the row stays `?`, the review lists it
under open risks, and the definition of done is not met.

### 2.4 Traceability

Gate 1 evidence is the contract table with three more columns filled after the run: `Test` (the
method and data row), `Evidence` (the TRX or log that shows it passing) and `Req`. A requirement with
no row, or a row with no test, is a gap the review names. A test with no row is either a missing row
to propose with `?` or a test to remove.

---

## 3. Rules by competency group

Prefer the simplest structure that preserves isolation, readability, traceability and reliable
execution. Never introduce a class, a fixture or an abstraction only to satisfy a naming or layout
convention.

Rules come in three strengths. **Mandatory** rules protect correctness, isolation or safety and have
no exception: R1.1, R2.1, R3.5, R3.7, R3.9, R4.1, R4.2, R5.1, R5.4, R5.8, R6.4. **Context-dependent**
rules apply only when the situation they name exists, and `N/A` with a reason is the normal answer
otherwise: R2.7, R2.8, R2.10, R3.3, R3.8, R4.6, R4.7, R6.6, R6.9. Every other rule is a **convention**:
the default, with an exception path of one line stating the reason next to the code, which the review
records instead of a deduction.

### G1. Host, scope and fundamentals

| # | Rule |
|---|---|
| R1.1 | An integration test runs the application in-process through `WebApplicationFactory<TEntryPoint>` and sends requests with the `HttpClient` from `CreateClient()`. A test that hard-codes `https://localhost:5001` depends on a process it does not control and is not reproducible. |
| R1.2 | `TEntryPoint` is a type the test project can see. With top-level statements add a public marker interface in the API project (`public interface IApiMarker { }`) or `public partial class Program { }`. This is a production change made for testability and is reviewed as one (Section 5.3). |
| R1.3 | The factory is the fixture. `IClassFixture<TFactory>` gives one host (and one container) per class. `ICollectionFixture<TFactory>` gives one host for several classes that then run sequentially. State the trade-off when choosing: isolation versus startup time. |
| R1.4 | One `HttpClient` per test class, created from the factory in the constructor. Do not construct `HttpClient` directly. |
| R1.5 | Default: one test class per endpoint operation, in a folder per controller or feature (`CustomerController/CreateCustomerControllerTests.cs`). Exception: related operations that share the same fixture and the same seeded state may share one class when the file stays readable. Never split a class only to satisfy the convention. |
| R1.6 | Default: AAA with the three comments `// Arrange`, `// Act`, `// Assert`, because AI-written tests are checked by machines before by people. Exception: a test that is one expression per section may omit the comments. Arrange seeds state, Act is the one HTTP call, Assert reads status, body, state and fakes. |
| R1.7 | A test name states the operation, the scenario and the expected result. Default format `Method_Scenario_ExpectedResult`: `Get_ReturnsNotFound_WhenCustomerDoesNotExist`. Another format that states all three is accepted. A name that states neither the scenario nor the outcome, or a suffix like `2`, scores 0. |
| R1.8 | One Act per test and one behavior per test. Use `[Fact]` for a distinct scenario and `[Theory]` for the same behavior with different data. Several asserts are fine when they describe one outcome (status plus body plus header of one call). |
| R1.9 | Pure domain logic (value objects, mappers, calculations) is covered by unit tests. The integration suite covers only what needs the pipeline or the infrastructure. |

```csharp
// R1.1 and R1.4, wrong: a client aimed at a process the test does not start
private readonly HttpClient _httpClient = new() { BaseAddress = new Uri("https://localhost:5001") };

// right: the factory hosts the application in-process
public class GetCustomerControllerTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _client;
    public GetCustomerControllerTests(CustomerApiFactory factory) => _client = factory.CreateClient();
}
```

### G2. Contract and HTTP assertions

The test asserts the observable contract: what a caller can see through HTTP, what is persisted, and
what left the system through a fake. It never asserts how the code got there (which method ran, in
which order, with which internal object).

| # | Rule |
|---|---|
| R2.1 | Every test asserts the status code and the observables its row names, at least one beyond the status. The observables are chosen by the scenario, not by a checklist: a validation failure asserts status, the error key and nothing persisted. A create asserts status, body, `Location` and the read-back. An unauthenticated call asserts status, no data in the body and no side effect. Status alone scores 0 in this group. |
| R2.2 | Read the body into the response contract type with `ReadFromJsonAsync<T>()` and compare it with an equivalence assertion against the request or the created entity. Server-generated members (`Id`, timestamps) are excluded explicitly or by comparing against the expectation type that lacks them, and the test says which. |
| R2.3 | A validation failure is asserted as 400 plus `ValidationProblemDetails` with `Errors["<Field>"][0]` equal to the expected message. One row per field rule. The error key is part of the contract. A correlation field (`traceId`) is asserted only when the error contract names it (R6.6). |
| R2.4 | A well-formed key with no match returns 404 and is a `not-found` row. A malformed key is a separate row. Record whether the route constraint (404) or model binding (400) answers, and ask which the contract wants. |
| R2.5 | A create returns 201 with a `Location` header that resolves to the GET route of the new resource. The test follows it, or issues the GET, to prove persistence. |
| R2.6 | When an outbound dependency fails, the row says what the API returns. The fake is set up to produce the failure (403 with a throttle body, a timeout) and the test asserts the API status and that nothing was persisted, not the dependency's status. |
| R2.7 | Field rules with thresholds get a boundary pair (date of birth today versus tomorrow, username of 39 versus 40 characters). String fields get null, empty and whitespace rows. Never invent a rule. |
| R2.8 | Side effects on fakes are asserted when the contract names them: the mock received exactly one call for a valid request and none for a request rejected by validation. |
| R2.9 | Theory rows carry a `scenario` value so a failure names the case. Data goes in `[InlineData]` for primitives and `TheoryData<...>` for objects, never in `object[]` lists. |
| R2.10 | Security rows (Section 2.2) assert the status the contract defines, that the body carries no data the caller must not see, and that no side effect happened: nothing persisted, no call on the fake. A forbidden row is written per policy and, when the model has owners or tenants, per "another owner's resource". |

```csharp
// R2.1, R2.2, R2.5: status, body, header and state in one row
[Fact]
public async Task Create_ReturnsCreated_WhenDataIsValid()
{
    // Arrange
    var request = _customerGenerator.Generate();

    // Act
    var response = await _client.PostAsJsonAsync("customers", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var created = await response.Content.ReadFromJsonAsync<CustomerResponse>();
    created.Should().BeEquivalentTo(request);                       // Id is not on the request type
    response.Headers.Location.Should().Be($"http://localhost/customers/{created!.Id}");
    var readBack = await _client.GetFromJsonAsync<CustomerResponse>(response.Headers.Location);
    readBack.Should().BeEquivalentTo(created);
}

// R2.3: the error key and message are the contract
var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
problem!.Status.Should().Be(400);
problem.Errors["Email"][0].Should().Be($"{invalidEmail} is not a valid email address");

// R2.10: a forbidden row proves nothing happened
var response = await _clientAsViewer.DeleteAsync($"customers/{created.Id}");
response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
(await _clientAsOwner.GetAsync($"customers/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

// R2.9: scenario column
public static TheoryData<string, string, string> EmptyFields => new()
{
    { "FullName", "", "empty full name" },
    { "FullName", "   ", "whitespace full name" },
    { "Email", "", "empty email" },
};
```

### G3. Dependency replacement

| # | Rule |
|---|---|
| R3.1 | Replace dependencies in `ConfigureWebHost` through `ConfigureTestServices`, which runs after the application's own registrations. Use `RemoveAll<T>()` then add the test registration. Replace the narrowest seam: swap the connection factory and keep the repository, the SQL and the database real. |
| R3.2 | An external HTTP API becomes a WireMock server owned by the factory. Re-register the named `HttpClient` with `BaseAddress` set to the mock's URL. One stub per scenario (known user answers 200, unknown user 404, throttled user 403, slow user delays past the timeout). |
| R3.3 | Hosted services are classified one by one in the factory, never removed wholesale: kept when the row depends on them (an outbox processor the Then reads), replaced by a fake when the row needs their effect without their I/O, disabled when their side effects are outside the test scope. The review lists each one with its class and, for a disabled one, names the test that covers it with its evidence (a run log), or records it as an open risk. |
| R3.4 | Clear logging providers in the factory, or route logs to the test output, so test output stays readable and failures can be traced. |
| R3.5 | Never replace the thing under test. If the row is about persistence, the database is real. An in-memory repository turns the test into a unit test of the controller and the review says so. Replacing more than the row needs is over-mocking and scores 1. |
| R3.6 | Configuration overrides go through `ConfigureAppConfiguration` with an in-memory collection or `UseSetting`, never by editing `appsettings.json` on disk. A value that cannot be overridden (a hard-coded URL) is a testability defect in production code. |
| R3.7 | Authentication is replaced by a test `AuthenticationHandler` registered as the default scheme that issues the principal the row asks for (claims, roles, tenant). Authorization stays real: policies, `[Authorize]` attributes and resource handlers are never bypassed, so a forbidden row proves the policy and not the handler. |
| R3.8 | Time is a dependency. The application takes `TimeProvider` and the factory registers `FakeTimeProvider`. Rules such as "date of birth not in the future" are tested at a fixed instant. |
| R3.9 | Test configuration never carries production credentials, production endpoints or real personal data, and test output never logs a secret or a token. Connection strings come from the container, external URLs from the fake, and generated data from a generator. A test that could reach a production system when a seam is misconfigured is reported as a defect before anything runs. |

```csharp
// R3.1 and R3.2: the factory owns the container and the mock, and swaps the seams
public class CustomerApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public const string ValidGitHubUser = "validuser";
    public const string ThrottledGitHubUser = "throttled";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();
    private readonly GitHubApiServer _gitHub = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<OutboxPublisher>();                      // R3.3: disabled, covered by OutboxTests
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(_ =>
                new NpgsqlConnectionFactory(_db.GetConnectionString()));
            services.AddHttpClient("GitHub", client => client.BaseAddress = new Uri(_gitHub.Url));
        });
    }

    public async Task InitializeAsync()
    {
        _gitHub.Start();
        _gitHub.SetupUser(ValidGitHubUser);
        _gitHub.SetupThrottledUser(ThrottledGitHubUser);
        await _db.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _gitHub.Dispose();
    }
}
```

### G4. Real infrastructure

| # | Rule |
|---|---|
| R4.1 | The container lifecycle belongs to the factory: start in `InitializeAsync` before the host is built, dispose in `DisposeAsync` after the last test of the fixture scope. |
| R4.2 | The connection string comes from the running container (`GetConnectionString()`), never from a fixed host port. A fixed port binding collides with the next fixture, the next agent on the CI box, or a local database. |
| R4.3 | Pin the image tag (`postgres:16-alpine`). `latest` makes the suite change behavior without a commit. |
| R4.4 | The schema is created by the application's own initializer or migrations running against the container at startup. Tests do not duplicate DDL. Exactly one fixture owns schema creation for a given database: with one database per host that is the factory, with a shared database it is the holder that started the container, and the hosts that join later must not run the initializer again. `CREATE TABLE IF NOT EXISTS` on PostgreSQL is not safe from several processes at once. |
| R4.5 | Docker is detected before the suite runs (Section 6). Locally, tests that need it are skipped with a reason and the run is reported as `NOT AVAILABLE`, never failed and never silently passed. On CI, Docker is a prerequisite: the job fails at the environment step, so a missing daemon can never look like a green quality gate. |
| R4.6 | A full-stack compose (application image plus database) is the outermost tier, used for UI tests that need a real browser against a real server. The application in a container reaches a mock on the host through `host.docker.internal` and a fixed port, which is the one place a fixed port is justified. |
| R4.7 | When container startup dominates, use one container per assembly and give each class its own database name, or reset state between tests with a tool such as Respawn. The choice is recorded with the trade-off. |

```csharp
// R4.2, wrong: a fixed host port
new PostgreSqlBuilder().WithPortBinding(5555, 5432).Build();
// right: let Docker assign the port and read the connection string
var db = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
await db.StartAsync();
var connectionString = db.GetConnectionString();
```

### G5. Data isolation and stability

| # | Rule |
|---|---|
| R5.1 | Every test creates the data it needs with unique values from a generator. No test relies on data created by another test or by a seed script shared across tests. |
| R5.2 | Assertions on collections are scoped to the test's own data: `Contain(x => x.Id == created.Id)`. `Single()` or `BeEmpty()` on a list endpoint is valid only when the test class exclusively owns the host and the database, and the test says so. |
| R5.3 | Cleanup is explicit and runs on failure: track created ids and delete them in `DisposeAsync`, or own the database at class scope so disposal removes it. A `// Cleanup` block after the assertions does not run when an assertion fails. Cleanup never depends on the endpoint under test: the class that tests DELETE cleans up by owning its database or by direct SQL on its own rows, not by calling DELETE. |
| R5.4 | Parallel test classes must not share mutable state. Three layouts are valid: each class has its own host and container (`IClassFixture`), or several classes share one host through a collection and run sequentially, or classes share one container but each owns a database of its own inside it (R4.7) and stays parallel. Parallel classes on one shared database is the defect Gate 4 catches. |
| R5.5 | Three identical runs by test identity (M5), with containers recreated each run. Compare names and failure reasons, not counts. Three runs prove repeatability under one set of conditions, not reliability under load, slow dependencies or retries. Those are contract rows (Section 2.2) when the risk calls for them, not extra runs. |
| R5.6 | Generated data satisfies the application's validation rules and the fakes' setups: the generator uses a GitHub username the mock knows, an email the regex accepts, a date in the past. |
| R5.7 | Default: clean up through the API, so cleanup goes through the same contract the tests verify. Direct SQL on the test's own rows is used when the API has no delete, when DELETE is the operation under test (R5.3), or for bulk reset between classes. |
| R5.8 | No `Task.Delay` or `Thread.Sleep` to wait for a result. Wait on the observable: poll the GET with a bounded timeout, await the hosted service's completion signal, or read the fake's log. No `try { } catch { }` that swallows a failure in a test or a fixture. |

```csharp
// R5.3: cleanup that runs even when the assertion fails
public class CreateCustomerControllerTests : IClassFixture<CustomerApiFactory>, IAsyncLifetime
{
    private readonly List<Guid> _createdIds = new();

    [Fact]
    public async Task Create_ReturnsCreated_WhenDataIsValid()
    {
        // ...
        _createdIds.Add(created!.Id);        // registered before the assertions that may fail
        // Assert ...
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        foreach (var id in _createdIds) await _client.DeleteAsync($"customers/{id}");
    }
}

// R5.2, wrong: assumes nobody else wrote to the database
customersResponse!.Customers.Single().Should().BeEquivalentTo(createdCustomer);
// right: scoped to this test's data
customersResponse!.Customers.Should().ContainEquivalentOf(createdCustomer);
```

### G6. Practical, observability and trust

| # | Rule |
|---|---|
| R6.1 | Every integration test class carries `[Trait("Category", "Integration")]` so the inner loop can exclude it: VSTest `dotnet test --filter "Category!=Integration"`, Microsoft Testing Platform `dotnet test -- --filter-not-trait "Category=Integration"`. |
| R6.2 | One command runs the suite. The README or the test output states that Docker is required and which image tags are pulled. |
| R6.3 | CI provides Docker to the test job. Integration tests run in their own stage after unit tests, with `--no-build` after a separate build step, and with a per-test timeout generous enough for container startup. |
| R6.4 | A test that cannot run in the current environment is skipped with a reason that names the missing piece (`Docker daemon not reachable`), never commented out and never left to fail. A skip whose reason does not name a cause ("does not work at the moment") fails Gate 3. |
| R6.5 | Test output is observable: log through `ITestOutputHelper` (xUnit), `TestContext` (NUnit, MSTest), never `Console.WriteLine`. On failure the output shows the request, the status and the body. |
| R6.6 | When the error contract carries an identifier, the test asserts it is present and names which one: a correlation id the caller sent and gets back, the server's trace identifier (`HttpContext.TraceIdentifier`, surfaced as `traceId`), or a W3C trace context (`traceparent`) that links the request to distributed traces. They are different things, and the contract says which one a caller can use to find the request in the logs. |
| R6.7 | Coverage uses the runner's collector (Section 8). The in-process host is instrumented, so controller, middleware and repository code appear in the report. Code running inside a compose container is not instrumented and the review says so. Coverage is a diagnostic, never a gate. |
| R6.8 | The tier is a risk decision (Section 7). Each suite states which tier it is and why: in-process host for API contracts, container for persistence, compose plus browser only for what a user sees. Choosing a higher tier than needed is a cost defect, choosing a lower one is a trust defect. |
| R6.9 | UI tests (Playwright) are the outermost tier: one browser per collection fixture, one page per test, selectors by stable ids, the application started from compose. Browser binaries are an install step that needs approval on a managed machine. |

```csharp
// R6.1 and R6.4
[Trait("Category", "Integration")]
public class CreateCustomerControllerTests : IClassFixture<CustomerApiFactory> { }

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerProbe.IsAvailable) Skip = "Docker daemon not reachable, set DOCKER_HOST or start Docker Desktop";
    }
}

// R6.5: a failure explains itself
_output.WriteLine($"POST /customers -> {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
```

---

## 4. Framework layer

### 4.1 WebApplicationFactory idioms

```csharp
// Marker type when Program is not visible
public interface IApiMarker { }

// Plain factory, no replacement (uses the application's own configuration)
public class GetCustomerControllerTests : IClassFixture<WebApplicationFactory<IApiMarker>>
{
    public GetCustomerControllerTests(WebApplicationFactory<IApiMarker> factory)
        => _client = factory.CreateClient();
}

// Shared host for several classes, run sequentially
[CollectionDefinition("Customer API")]
public class CustomerApiCollection : ICollectionFixture<CustomerApiFactory> { }

[Collection("Customer API")]
public class GetAllCustomerControllerTests { }

// Reading a service from the host for a direct state check
using var scope = factory.Services.CreateScope();
var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

// Overriding configuration without touching appsettings
builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["GitHub:ApiBaseUrl"] = _gitHub.Url,
}));

// R3.7: test authentication, real authorization
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e)
    : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());
        var role = Request.Headers["X-Test-Role"].ToString();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user!), new Claim(ClaimTypes.Role, role)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
// in ConfigureTestServices
services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
// in the test: client.DefaultRequestHeaders.Add("X-Test-User", "ada") and "X-Test-Role", "Viewer"
```

Version notes. `ConfigureTestServices` lives in `Microsoft.AspNetCore.TestHost`. `RemoveAll<T>()`
lives in `Microsoft.Extensions.DependencyInjection.Extensions`. The `Microsoft.AspNetCore.Mvc.Testing`
package major version matches the application's target framework major version, and Detect chooses it.
The factory sets the environment to `Development`, so an unhandled exception answers 500 with the
developer exception page unless the application registers its own handler.

### 4.2 Testcontainers idioms

```csharp
// Current API (Testcontainers.PostgreSql module)
private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("app")
    .WithUsername("app")
    .WithPassword("app")
    .Build();

await _db.StartAsync();
var connectionString = _db.GetConnectionString();
await _db.DisposeAsync();

// Generic container when no module exists
private readonly IContainer _service = new ContainerBuilder()
    .WithImage("vendor/service:1.2")
    .WithPortBinding(8080, assignRandomHostPort: true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/health")))
    .Build();
var mappedPort = _service.GetMappedPublicPort(8080);
```

Version notes. Package versions 1.x and 2.x were published under a different package id with the
`TestcontainersBuilder<PostgreSqlTestcontainer>` and `PostgreSqlTestcontainerConfiguration` API, and
exposed `ConnectionString` as a property. Those versions use a Docker client that current daemons
reject at container start with "cannot hijack chunked or content length stream". That failure is an
environment failure, not red evidence. The module packages (`Testcontainers.PostgreSql`,
`Testcontainers.MsSql`, `Testcontainers.Redis`) exist from 3.0. The resource reaper (`ryuk`) removes
leftover containers when a run is killed. Detect reads the version in the project before writing code.

### 4.3 WireMock idioms

```csharp
public sealed class GitHubApiServer : IDisposable
{
    private WireMockServer? _server;
    public string Url => _server!.Url!;

    public void Start() => _server = WireMockServer.Start();      // random free port

    public void SetupUser(string username) =>
        _server!.Given(Request.Create().WithPath($"/users/{username}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("content-type", "application/json; charset=utf-8")
                    .WithBody($$"""{"login": "{{username}}", "id": 1}"""));

    public void SetupThrottledUser(string username) =>
        _server!.Given(Request.Create().WithPath($"/users/{username}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(403)
                    .WithBody("""{"message": "API rate limit exceeded"}"""));

    // timeout and partial failure rows: delay past the client timeout
    public void SetupSlowUser(string username, TimeSpan delay) =>
        _server!.Given(Request.Create().WithPath($"/users/{username}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithDelay(delay));

    // Recorded interactions for a side-effect assertion (R2.8)
    public int CallsTo(string path) => _server!.LogEntries.Count(e => e.RequestMessage.Path == path);

    public void Dispose() { _server?.Stop(); _server?.Dispose(); }
}
```

A fixed port (`WireMockServer.Start(9850)`) is needed only when the application runs in a container
and must reach the mock through `host.docker.internal`. In-process hosts use the random port.

### 4.4 Test framework lifecycle and mapping

| Concept | xUnit v2 | xUnit v3 | NUnit | MSTest |
|---|---|---|---|---|
| Async fixture setup and teardown | `IAsyncLifetime` returning `Task`, factory declares `public new Task DisposeAsync()` | `IAsyncLifetime` returning `ValueTask` | `[OneTimeSetUp]` and `[OneTimeTearDown]` async | `[ClassInitialize]` and `[ClassCleanup]` async |
| One host per class | `IClassFixture<T>` | `IClassFixture<T>` | static field set in `[OneTimeSetUp]` | static field set in `[ClassInitialize]` |
| One host for several classes | `[Collection]` with `ICollectionFixture<T>` | same | `[SetUpFixture]` | `[AssemblyInitialize]` |
| Data-driven | `[Theory]` with `[InlineData]` or `TheoryData<>` | same, plus `TheoryDataRow` | `[TestCase]` or `[TestCaseSource]` | `[DataTestMethod]` with `[DataRow]` or `[DynamicData]` |
| Skip with reason | `[Fact(Skip = "...")]` or a custom `FactAttribute` | `Assert.Skip(reason)` | `Assert.Ignore(reason)` | `Assert.Inconclusive(reason)` |
| Category | `[Trait("Category","Integration")]` | same | `[Category("Integration")]` | `[TestCategory("Integration")]` |
| Parallel control | `[Collection]`, `maxParallelThreads` | same | `[NonParallelizable]` | `[DoNotParallelize]` |
| Output | `ITestOutputHelper` | `ITestOutputHelper` | `TestContext.Out` | `TestContext.WriteLine` |

The patterns in this rulebook do not depend on the test framework. Detect reads which one the project
uses and maps through this table.

### 4.5 Run, filter, report

| Need | VSTest (`Microsoft.NET.Test.Sdk`) | Microsoft Testing Platform |
|---|---|---|
| Run only integration tests | `dotnet test --filter "Category=Integration"` | `dotnet test -- --filter-trait "Category=Integration"` |
| Exclude them from the inner loop | `dotnet test --filter "Category!=Integration"` | `dotnet test -- --filter-not-trait "Category=Integration"` |
| Keep a report per run | `dotnet test --logger trx --results-directory ./TestResults/run1` | `dotnet test -- --report-xunit-trx --results-directory ./TestResults/run1` |
| Coverage | `dotnet test --collect:"XPlat Code Coverage"` | `dotnet test -- --coverage --coverage-output-format cobertura` |
| Run three times | a loop in the shell, one TRX per run, compared by test name | same |

---

## 5. Review: gates, scorecard, testability changes, failure classification, output template, done

### 5.1 Gates

Gates are numbered Gate 1 to Gate 4. Scorecard groups are G1 to G6. The two sets are never mixed.
A failed gate sets `Status: BLOCKED`. The review continues to the scorecard as a diagnostic. The
scorecard never overrides a gate. Gates have no exceptions.

| Gate | Check | Passes when | Evidence |
|---|---|---|---|
| Gate 1 | Contract coverage | Every approved contract row (Section 2) maps to a test case and to its evidence (Section 2.4). The contract comes from acceptance criteria or a table the user approved. Existing tests are never the contract. Without an approved contract Gate 1 is BLOCKED and a proposed table with `?` rows is attached. In characterization, `?` rows not yet confirmed (Section 2.3) keep the gate at PASS only when the review lists them as open risks and the definition of done is reported as not met. | Contract table with `Test` and `Evidence` columns, and the list of unconfirmed `?` rows. |
| Gate 2 | Red evidence | TDD: the test compiled, reached the Act, and failed at the behavior assertion (for example 404 where 201 was expected) before the endpoint existed. Characterization: `N/A (characterization)`, never a pass. A failure classified as environment or test defect (Section 5.4) is not evidence. | Red-run log with test names and reasons. |
| Gate 3 | Assertions match the contract | Every test asserts the observables its approved row names (R2.1): a row that names status, body and persistence is not covered by a test that asserts status alone, and that test fails the gate. A test that asserts exactly what its row names passes, whatever the count. `Assert.True(true)`, a bare not-null or not-empty check, an assertion unrelated to the Act, or an assertion on implementation details counts as assertion-less. Every skip names its cause (R6.4). | List of tests whose assertions fall short of their row. |
| Gate 4 | Stability | Three consecutive runs with containers recreated produce the same passed, failed and skipped names and the same failure reasons. When the environment cannot run the suite locally, Gate 4 is `NOT RUN` with the reason and the status is BLOCKED. On CI a missing environment fails the job (R4.5). | Three run logs kept with the review. |

### 5.2 Scorecard, six groups scored 0, 1 or 2

| Group | 0 | 1 | 2 |
|---|---|---|---|
| G1 Host, scope and fundamentals | Client aimed at an external URL, or no factory, or names that state neither scenario nor outcome. | Factory present but `HttpClient` constructed by hand, or classes split or merged against R1.5 without a stated reason, or names missing the scenario or the outcome, or several Acts per test. | In-process factory, one client per class from the factory, classes per R1.5 or with a stated reason, AAA, names stating operation, scenario and outcome, Fact and Theory chosen correctly. |
| G2 Contract and HTTP assertions | Status code only, or assertions on implementation details. | Body read but server-generated members compared blindly, or validation asserted without the error key, or no not-found row where a key exists, or Theory data without `scenario`, or security rows missing where the endpoint has an effective authorization requirement (Section 2.3 step 1). | Status plus the observables each row names, validation keyed by field, not-found, upstream, security and reliability rows present where the contract or the risk calls for them, boundary and null rows where rules exist, side effects on fakes asserted where named, `N/A` justified. |
| G3 Dependency replacement | Real external API or real shared database from `appsettings`, or production credentials in test configuration. | Replacement done in `ConfigureServices` instead of `ConfigureTestServices`, or hosted services removed wholesale, or the thing under test replaced, or more replaced than the row needs. | Seams swapped in `ConfigureTestServices` with `RemoveAll`, external HTTP behind WireMock with failure stubs, hosted services classified one by one, authorization kept real, logging routed, no production reachable. |
| G4 Real infrastructure | Fixed ports, `latest` tag, container started outside the fixture, no Docker detection. | Container in the fixture but the schema owned by more than one fixture, or compose used where in-process would do, or image not pinned. | Lifecycle in `IAsyncLifetime`, connection string from the container, pinned image, one schema owner, Docker detected and reported locally, required on CI. |
| G5 Data isolation and stability | Tests depend on each other's data, `Single()` on a shared list, cleanup after the asserts, `Thread.Sleep` or `Task.Delay` as synchronization. | Own data per test but cleanup missing on failure, or cleanup through the endpoint under test, or a shared host with parallel classes. | Unique data per test, assertions scoped to own data, cleanup in `DisposeAsync` independent of the operation under test, fixture tier matches the parallelism, waits on observables, three identical runs. |
| G6 Practical, observability and trust | No trait, cannot run with one command, Docker requirement undocumented, `Console.WriteLine`. | Traits present but no skip-with-reason when Docker is absent, or no report, or no test output on failure, or tier not stated, or coverage neither measured nor explained. | Traits, one command, skip with reason locally and hard requirement on CI, test output on failure, identifier asserted where the contract names it, report produced, coverage measured and read or `NOT MEASURED` with a reason the review accepts (no collector in the project, a tier that cannot be instrumented), tier stated with its risk reason. |

Total is out of 12. Every deduction cites `file:line` and the rule id, for example `R5.2`. A
deviation with a stated reason next to the code is recorded in the "Deducted for" column as
"exception: <reason>" and costs nothing.

### 5.3 Reviewing production changes made for testability

Integration tests often require changes to production code. AI will make them readily. Each one is
listed in the review with the question that decides whether it is safe.

| Change | Legitimate when | Review question |
|---|---|---|
| Marker interface or `public partial class Program` | Always. It exposes nothing. | Is it in the API project and public? |
| A hard-coded URL or connection string moved to configuration | Always. It is a testability and deployment defect. | Does production still read the same value, and is the default unchanged? |
| Error responses gain a field key or an identifier | When the client contract allows the added information. | Is the key stable, documented, and does an existing client depend on the old shape? |
| A hosted service disabled in tests | Only in the test factory, per service, with a reason. | What test covers the disabled service, and does the application still register it? |
| Validation message text changed to match a test | Only with an approved contract change. | Was the contract row updated and approved, or was the message bent to the test? |
| A check reordered (validation before or after not-found) | Only with an approved contract change. | Which order does the contract want, and which rows change? |
| An interface added in front of a dependency | When the seam is the narrowest one that works. | Does the real implementation stay registered in production? |
| An authorization policy relaxed, or a test-only bypass added | Never. | Which forbidden row made the implementation choose this instead of a test principal (R3.7)? |

An edit to production code that exists only to let a weak test pass (returning a fixed value when a
test header is present, skipping validation in `Development`) is rejected as test weakening in disguise.

### 5.4 Failure classification, rework and trust assessment

Every failing test after implementation started is classified before anything is edited. The class
decides what may change.

| Class | Signal | What may change |
|---|---|---|
| Environment failure | Docker unreachable, image missing, port taken, library rejected by the daemon, compile error. | Only the environment. Not the test, not the implementation. Not red evidence. |
| Test defect | The row is right and the test asserts something else: wrong field name, wrong generator, cleanup order. | The test. Logged as a legitimate correction. |
| Implementation defect | The test matches its row and the application answers differently. | The implementation. This is the normal red-to-green path. |
| Contract ambiguity | The test and the implementation each read the row differently and both readings are defensible. | Nothing, until the row is rewritten and approved. Escalated to the contract owner. |

A third attempt at the same failing test, or the same fix applied a third time, without a new
classification in between, is a loop. The loop is classified as contract ambiguity until the contract
owner says otherwise: stop, quote the row and the two readings, and escalate.

Every test edit after implementation started is then logged and classified.

| Class | Meaning | Verdict |
|---|---|---|
| Legitimate correction | The test setup or assertion was wrong. Behavior unchanged. | Accepted after review. |
| Contract change | The requirement was misunderstood, or the test exposed a contract defect (an error without a field key). The contract row is updated. | Accepted after approval of the new row. |
| Test weakening | An assertion was loosened or removed to let a bug pass. | Rejected. |
| Implementation-driven rework | The test was rewritten to follow the implementation. | Counted. This is the number that should trend down. |

Rework equals (implementation-driven rework plus test weakening) divided by total tests. Report all four
counts. Count only from Git history, diffs or a recorded change log. Without such evidence report
`Rework: NOT AVAILABLE`. Never infer past edits from the current test code.

Trust assessment is the last line of the review and is derived, never asserted: the four gates, the
scorecard total, the rework ratio, the coverage status and the open risks, each with its evidence.
A suite with a failed gate has no trust level, only a blocker to remove.

### 5.5 Output template (produce exactly this)

```
## Review: <project>, <date>
Mode: TDD | characterization
Tier: in-process host | container | compose plus browser (risk: Low | Medium | High | Critical, stage: Explore | Expand | Extract | not stated, reason)
Environment: Docker AVAILABLE (Server 28.0.4) | NOT AVAILABLE (reason)
Needs: Docker yes/no, real database yes/no, external services: <names or none>
Runs without Docker: <test classes or none>
Command: dotnet test ...

Gates
Gate 1 PASS  18/18 rows mapped, 18/18 with evidence
Gate 2 FAIL  no red-run evidence            (or: N/A characterization, 6 rows marked ? awaiting confirmation)
Gate 3 PASS
Gate 4 PASS  3 identical runs by test identity (12 passed, 0 skipped, containers recreated, logs attached)
             (or: NOT RUN, Docker daemon not reachable)
Status: BLOCKED | PASS

Scorecard (diagnostic, does not override gates)
| Group | Score | Deducted for |
|---|---|---|
| Group G1 Host, scope and fundamentals | 2 | |
| Group G2 Contract and HTTP assertions | 1 | R2.5 Location header not followed at CreateCustomerControllerTests.cs:40 |
| ... | | |
Total: 10/12

Security rows: <count present / count the contract names, or N/A with reason>
Hypotheses: <n ? rows confirmed by the contract owner, m awaiting confirmation, or N/A for TDD>
Production changes for testability: <list with the Section 5.3 question answered, or none>
Failures classified: environment 0, test defect 1, implementation defect 3, contract ambiguity 1 (escalated: row R4 order of checks)

Rework: legitimate 1, contract change 1 (approved), weakening 0, implementation-driven 2. Ratio 2/21 (9.5%)
        (or: NOT AVAILABLE, no change history for the test project)
Unrequested: <endpoints, parameters, fields or branches in the change with no contract row, or none>

Coverage (diagnostic)
Status: MEASURED | NOT MEASURED (reason: ...)
Runner and collector: VSTest, coverlet.collector
Command: dotnet test --collect:"XPlat Code Coverage"
Report: tests/<project>/TestResults/<guid>/coverage.cobertura.xml
Instrumented: Customers.Api. Not instrumented: Customers.WebApp (compose)
Line 80% (80/100), Branch 60% (30/50), Method: not provided by the report
Uncovered branches:
- CustomerService.UpdateAsync, CustomerService.cs:52: GitHub user invalid on update. Business rule? (ask)
Contract rows without a test: none

Open risks: <rows marked N/A for concurrency or timeout with the reason, disabled hosted services, or none>
Definition of done: <met | not met, items missing>
Trust assessment: BLOCKED by Gate 1 | gates PASS, scorecard 10/12, rework 9.5%, coverage measured, 1 open risk
Next actions (max 3): ...
```

### 5.6 Definition of done

Work is done when every line below is true. `PASS` means these conditions, never that `dotnet test`
returned exit code 0.

- The contract is approved and every row carries a `Req` or `none`.
- Every contract row maps to a test case and to a run log that shows it (Section 2.4).
- Red evidence is recorded for TDD rows, or the mode is characterization and every `?` row has been
  confirmed by the contract owner after the run (Section 2.3).
- Security rows exist for every endpoint with an effective authorization requirement, or the contract
  states `N/A` with a reason.
- The tier, the risk level and the stage (or `not stated`) are stated and match the rows (Section 7).
- Every dependency that is part of a row's Then is exercised in the environment appropriate to it: a
  container for a database that can run in one, an emulator or a dedicated test account otherwise, a
  fake only for what the application does not own. The suite documents what is real, what is
  simulated, and which rows are not covered at this tier. An API without persistence has no such rows.
- No test can reach a production or shared system (R3.9, Section 6.1 step 6).
- Every test owns its data, cleans up on failure, and the fixture tier matches the parallelism.
- Three identical runs by test identity, with the environment the suite needs reset or recreated
  between runs (containers recreated when there are containers).
- Coverage collected and read, or `NOT MEASURED` with a reason the review accepts.
- Every production change for testability is listed with its Section 5.3 answer.
- Every failure since implementation started is classified (Section 5.4).
- Remaining risks and environment limitations are written down: rows marked `N/A`, disabled hosted services, tiers not exercised, tests not run here.

---

## 6. Test environment workflow

### 6.1 Detect before running

1. Read the test project: a reference to a Testcontainers package, a compose file next to the tests,
   a `WebApplicationFactory` subclass that starts containers, or a WireMock server means the suite
   needs infrastructure. Record three facts: needs Docker (yes or no), needs a real database (yes or
   no), external services replaced by fakes (names).
2. Read `TargetFramework` and the versions of the test packages. Any package added must be compatible
   with that target framework and with the Docker daemon in use. Never pin a version from memory.
3. Only when step 1 found a need for Docker, probe the daemon with `docker info` and read the exit
   code. A client that prints its version while the server section errors means the daemon is stopped.
   Record the server version when reachable. When nothing needs Docker, report `Docker: N/A` and skip
   steps 4 and 5.
4. List the images the suite pulls (`WithImage(...)`, compose `image:`, library defaults) and check
   `docker image ls`. A pull is a network download the run will perform. Report it before running on a
   slow or restricted network.
5. Classify every test class: runs without Docker (plain factory with every seam faked), needs Docker,
   needs a browser or another install. Report the three lists.
6. Check that no test configuration can reach a production system (R3.9): connection strings,
   external URLs and credentials in test settings are inspected before the first run.
7. Apply the execution boundaries in Section 6.4 before any action that changes the machine.

### 6.2 Container lifecycle

| Scope | Mechanism | When to choose |
|---|---|---|
| Per class | `IClassFixture<TFactory>` where the factory implements `IAsyncLifetime` | Default. Full isolation, parallel classes, one container each. |
| Per collection | `ICollectionFixture<TFactory>` | Startup dominates. Classes in the collection run sequentially. Assertions must still be scoped to own data (R5.2). |
| Per assembly | A static holder or an assembly fixture plus a database per class | Many classes and a heavy image. Record the choice. The holder owns schema creation, joining hosts do not run it (R4.4). |

Dispose order is the reverse of start order: host, then mock servers, then containers. The resource
reaper removes containers if the process is killed.

### 6.3 Reporting when the environment is missing

```
Environment: Docker NOT AVAILABLE (docker info: open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified)
Needs: Docker yes, real database yes (PostgreSQL), external services: GitHub (WireMock)
Runs without Docker: none in this project
Not run: CreateCustomerControllerTests, GetCustomerControllerTests, ... (12 tests)
Gate 4: NOT RUN
Status: BLOCKED
```

A suite that is skipped is reported as skipped with the counts. A test that fails because the daemon
is missing is an environment failure, not a red run (Gate 2) and not a stability failure (Gate 4).
On CI the same situation fails the job at the environment step and never reaches the test step.

### 6.4 Execution boundaries

What the AI may do on its own, what needs a question first, and what it never does. These
boundaries have no exceptions. On a managed machine, actions in the "ask first" row may also need
approval from the organization's IT or platform team.

Before any action in the "ask first" rows, state the exact command and its expected effect on the
machine (what gets downloaded, started, written or deleted), then wait.

| Action | Boundary |
|---|---|
| Read source, project files, configuration, test results | Allowed |
| Run read-only environment probes (`docker info`, `docker image ls`, `dotnet --info`) | Allowed |
| Build and run tests against containers the test fixture itself starts and owns | Allowed once the user asked for tests to run |
| Create files inside the test project or a scratch directory | Allowed |
| Pull a Docker image, start a stopped Docker Desktop | Ask first, name the image or the action |
| Add a package, install a tool, a runtime or a browser | Ask first, name the exact package and version |
| Change production code for testability | Allowed only for the changes listed in Section 5.3, each one reported |
| Change production code beyond the implementation the user asked for and the Section 5.3 list | Ask first, name the file and the reason |
| Change production code to make a test pass | Only after the failure is classified as an implementation defect (Section 5.4) |
| Edit a test after implementation started | Only after classification, logged (Section 5.4) |
| Run migrations, seed scripts or resets against a database the fixture did not start | Ask first, name the database |
| Delete data, drop a database, remove a container the fixture did not create | Never |
| Call, configure or point a test at a production or shared staging system | Never |
| Store credentials, tokens or real personal data in test files | Never |

---

## 7. Choosing the tier (a risk decision)

| Tier | What is real | What it catches | Cost | Use when |
|---|---|---|---|---|
| Unit | one class, fakes behind interfaces | logic, value objects, mapping | milliseconds, no Docker | always, for domain rules |
| In-process host, seams faked | routing, binding, validation, middleware, serialization, DI graph, authorization policies | contract shape, status codes, error keys, wiring mistakes, 401 and 403 | under a second per test, no Docker when every seam is faked | every endpoint contract row |
| In-process host plus container | everything above plus the real database and the real SQL | persistence, schema, queries, transactions, unique constraints under concurrency | seconds of startup per fixture, Docker required | every row whose Then includes persisted state |
| Compose plus browser | the deployed image, the database, the browser | what the user sees, deployment configuration, container networking | minutes, Docker and browser binaries required | a handful of user journeys, never the full contract |

The tier follows the risk of the endpoint, and the suite says which risk level it assumed.

| Risk | Example | Rows the contract adds | Tier |
|---|---|---|---|
| Low | read-only lookup, no side effect | valid, not-found, unauthenticated when protected | in-process host |
| Medium | create, update, delete with persistence | plus invalid, boundary, forbidden, upstream | host plus container |
| High | payment, authorization decision, money or quota movement | plus concurrency and idempotency, timeout and partial failure, overposting, sensitive fields | host plus container, failure stubs on every dependency |
| Critical | a transaction that spans services | all of the above plus a consumer-driven contract test per boundary and one end-to-end journey | host plus container for this service, compose plus browser for the journey |

The decision is recorded with the suite (the `Tier:` line of the review template, with the risk
level). Speed is chosen when the risk is low and the contract is covered one tier below. Depth is
chosen when the row's Then cannot be observed one tier below. Moving a row up a tier because the lower
tier "felt" insufficient is not a reason. Moving it down because the higher tier is slow is not a
reason either.

The product stage adjusts the decision and is recorded on the same line. The stage answers one
question: which failure would damage trust enough to matter now?

| Stage | What the users are trusting | Adjustment |
|---|---|---|
| Explore | that the team is listening and can change course | Medium risk may sit one tier below when every row is covered there and the dominant failure is building the wrong thing. Rollback is a contract row. No compose plus browser. |
| Expand | momentum under load | The tier from the risk table. Upstream failure, timeout and partial failure rows are required for every dependency, and every row logs enough to diagnose an incident (R6.5). |
| Extract | that it just works | The tier from the risk table, never below. Idempotency and concurrency rows are required from Medium risk upward. Reliability is a precondition, not a feature to schedule. |

A stage is a statement by the contract owner, not an inference from the code. Without one, record
`stage: not stated` and apply the risk table unchanged.

---

## 8. Coverage workflow

Coverage is measured evidence, not an estimate. The AI never reports a coverage number that did not
come from a report file produced in the current session.

### 8.1 Detect the runner, then choose the command

| Runner | How to recognize it | Collector | Command |
|---|---|---|---|
| VSTest | `Microsoft.NET.Test.Sdk` in the test csproj, no platform properties | `coverlet.collector` package | `dotnet test --collect:"XPlat Code Coverage"` |
| Microsoft Testing Platform | `OutputType=Exe` plus the platform runner properties | `Microsoft.Testing.Extensions.CodeCoverage` package | `dotnet test -- --coverage --coverage-output-format cobertura --results-directory ./TestResults` |

If the collector package is missing, report `Coverage status: NOT MEASURED` with the reason and the
exact package to add. Do not add packages or tools without asking (Section 6.4).

### 8.2 Collect and read

1. Run the full suite with collection enabled, once per test project. The coverage run is separate
   from the three stability runs, because instrumentation changes timing.
2. Locate the output. VSTest writes `TestResults/<guid>/coverage.cobertura.xml` under the test
   project. The platform writes `<results-directory>/<guid>.cobertura.xml`.
3. Open the report and list the assemblies it instrumented (the `package` elements). Compare with the
   projects the suite exercises. An assembly missing from the list (a compose-hosted service, a project
   excluded by a filter) is reported as not instrumented before any number is quoted.
4. Keep the file with the review. Quote `line-rate` and `branch-rate` from the XML root element,
   converted to percentages. Report method coverage only when the report provides it.
5. Exclude test code, generated code, migrations and third-party code before reading numbers, and say which test projects produced the report.

### 8.3 Analyze

1. Branch coverage is the one that matters for contract rows. A controller with one `if` can show
   100% line coverage with only one branch executed.
2. List every uncovered branch as `Class.Method, file:line, condition`.
3. Open the source at each location and compare with the approved contract.
4. For each uncovered branch, ask one question: is there a business rule here? If yes and no contract
   row exists, the contract is incomplete. Propose the row with `?`, do not write the test until it
   is approved.
5. A covered line is not a verified line. Gate 3 and coverage are evaluated independently.

---

## 9. Common traps (what a review looks for first)

| Trap | Rule violated | Effect |
|---|---|---|
| `HttpClient` with `BaseAddress = https://localhost:5001` | R1.1 | G1 = 0. The suite depends on a process it does not start. |
| Factory without replacement, `appsettings.json` points at a developer database and the real external API | R3.1, R3.2, R3.9 | G3 = 0, and the suite is throttled or breaks when the developer box changes. |
| Status code asserted, body ignored | R2.1 | G2 = 0. A 201 with a wrong body passes. |
| Assertion on implementation details (a repository method was called, a log line was written) instead of the HTTP observable | M1, Gate 3 | Gate 3 fails. The test breaks on refactoring and passes on wrong behavior. |
| `BeEquivalentTo` between response and request where the request also carries an `Id` the server regenerates | R2.2 | False failures or false passes. State the excluded members. |
| Over-specification: asserting the whole JSON text, header order or property order | R2.2 | Fails on harmless change. Compare the contract type, not the bytes. |
| Under-specification: `response.Should().NotBeNull()` or `body.Should().NotBeEmpty()` as the only assertion beyond the status | R2.1, Gate 3 | Passes on any body. Assert the fields the row names. |
| Asserting on the order of a list the contract does not order, or on a shared seed list | R5.2 | Breaks when another row is inserted. Scope to own data, assert order only when the contract defines it. |
| `Single()` or `BeEmpty()` on a list endpoint while other classes write to the same database | R5.2, R5.4 | Gate 4 fails with a different test name each run. |
| Several hosts share one database and each runs the schema initializer at startup | R4.4, R5.4 | The first test of each class fails at random with a duplicate key error from the catalog. Counts look stable, identities do not. |
| `// Cleanup` after the assertions | R5.3 | Cleanup skipped on failure, next run starts dirty. |
| The DELETE test cleans up by calling DELETE | R5.3 | A broken DELETE hides itself and leaves rows behind. |
| Shared factory through a collection plus assertions that assume an empty database | R5.2 | Order-dependent failures. |
| `Task.Delay(500)` before reading the result of a background step | R5.8 | Passes on a fast machine, fails on CI. Wait on the observable. |
| `try { } catch { }` around a request or a fixture step | R5.8 | A failure becomes a pass. |
| `WithPortBinding(5555, 5432)` or compose with host port 5432 | R4.2 | Collides with a local database or a parallel job. |
| `image: postgres` or `postgres:latest`, or a library default tag nobody pinned | R4.3 | Behavior changes without a commit. |
| Docker stopped, suite reports 12 failed, review calls it red evidence | Gate 2, R4.5 | Not evidence. Report `NOT AVAILABLE`. |
| An old container library fails at start with "cannot hijack" against a current daemon | Section 4.2, R4.5 | Environment failure. Detect reports the version mismatch, never a red run. |
| Repository replaced by an in-memory fake in a test about persistence | R3.5 | The test no longer covers the row. G3 = 1. |
| `RemoveAll<IHostedService>()` with no list of what was removed | R3.3 | A background behavior silently loses its only coverage. G3 = 1. |
| Authorization policy relaxed in `Development`, or a header that bypasses `[Authorize]` | R3.7, Section 5.3 | Rejected. Test principal through the handler, policy stays real. |
| Security rows derived by searching for `[Authorize]` while the application uses a fallback policy or `RequireAuthorization` conventions | Section 2.3 step 1 | Protected endpoints look unprotected. Derive the effective requirement from every source. |
| A `?` row reported as confirmed because the characterization run was green | Section 2.3 | Green describes, it does not approve. The row stays `?` until the contract owner confirms it. |
| `[Theory]` row with `Skip = "does not work at the moment"` and no cause | R6.4 | Gate 3 fails on the skip. |
| No `[Trait("Category","Integration")]` | R6.1 | G6 = 0 or 1. The inner loop cannot exclude the suite. |
| Error message or check order changed in production so a test passes, with no approved contract row | Section 5.3 | Test weakening in disguise. Rejected. |
| A failing test edited before its failure was classified | Section 5.4 | Rejected until classified. Every such edit is counted as rework. |
| Coverage percentage quoted for the UI tier hosted in compose | R6.7, Section 8 | Rejected. Not instrumented. |
| A partition filled with an invented rule (for example a 409 the API never returns) | Section 2.2 | Contract rejected. Remove the row. |
| Three runs compared by counts only | R5.5 | Flakiness hidden. Compare by test name and reason. |
| Three green runs presented as proof of reliability under load or retries | R5.5 | Repeatability only. Reliability rows are contract rows (Section 2.2). |
| An endpoint, parameter, response field or branch in the change that no contract row asks for | M1 | Unrequested functionality. Listed on the `Unrequested:` line. Removed, or a row is proposed and approved first. |
| The same failing test, or the same fix, attempted a third time without a new classification | Section 5.4 | Loop. Stop and escalate as contract ambiguity. |
| Structural changes (rename, extract, move) mixed into the commit that changes behavior | Section 5.4 | Rework cannot be classified and rollback becomes a rewrite. Structural changes go in their own commit with the suite green before and after. |
| Tier chosen from habit, with no risk level or stage on the `Tier:` line | Section 7 | Decision not recorded. The review asks for the risk level and the stage before scoring G4. |
