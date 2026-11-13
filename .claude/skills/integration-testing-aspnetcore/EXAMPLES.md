# Worked examples

Every number and every log line below comes from a run executed while these examples were written.
The run logs (TRX files and the Cobertura report) live with the validation projects that produced
them, not in this folder, so the figures here are quotations, not artifacts a reader can re-open from
the skill alone.
The application under test is a small Customers API: `POST /customers`, `GET /customers/{id}`,
`GET /customers`, `PUT /customers/{id}`, `DELETE /customers/{id}`, Dapper over PostgreSQL, value
objects that throw `ValidationException` into a middleware that answers `ValidationProblemDetails`,
and a named `HttpClient` that asks GitHub whether a username exists.

## Example 1. Mode `review`, calibration: a weak suite and a strong suite

Two pre-existing suites for the same API were reviewed with the same rulebook.

**Suite A** has one class with a hand-built `HttpClient` aimed at `https://localhost:5001`, a `[Fact]`
that expects 404 for a random Guid, and a `[Theory]` whose first row is `Skip = "This doesn't work
atm sorry"`. **Suite B** has a `WebApplicationFactory<IApiMarker>` subclass that starts a PostgreSQL
container and a WireMock server, swaps `IDbConnectionFactory` and the named client in
`ConfigureTestServices`, removes every `IHostedService`, and holds 13 tests in five classes, one
per endpoint operation.

Detect (both suites): runner VSTest, xUnit v2, `TargetFramework` net6.0. Suite A needs a running
process and a PostgreSQL on port 5432 that the tests do not start. Suite B needs Docker and pulls the
image its container library defaults to. Docker: Server 28.0.4 reachable.

Run results:

```
Suite A   Failed: 3, Passed: 0, Skipped: 1, Total: 4, Duration: 12 s
          every failure: System.Net.Http.HttpRequestException : No connection could be made because
          the target machine actively refused it. (localhost:5001)

Suite B   first attempt, container library 1.5.0 against Docker 28:
          Failed: 13, Passed: 0, Total: 13, Duration: 195 ms
          every failure: System.InvalidOperationException : cannot hijack chunked or content length stream
          (at Docker.DotNet.ContainerOperations.AttachContainerAsync, called from StartAsync)
          the library also pulled postgres:11.5 (428 MB) before failing

Suite B   factory moved to the current container API (PostgreSqlBuilder, GetConnectionString(),
          image pinned to postgres:16-alpine), test files untouched, three runs:
          run 1  Passed: 13   run 2  Passed: 13   run 3  Passed: 13
          IDENTICAL BY IDENTITY: YES
```

Both suites are characterization suites written against an existing API, so Gate 2 is `N/A`.
Neither has an approved contract, so Gate 1 is BLOCKED for both and a proposed table with `?` rows
was attached (it became the contract of Example 3).

| | Suite A | Suite B |
|---|---|---|
| Gate 1 | BLOCKED, no contract | BLOCKED, a list of test names is not a contract |
| Gate 2 | N/A (characterization) | N/A (characterization) |
| Gate 3 | FAIL, the skip reason does not name a cause (R6.4) | PASS |
| Gate 4 | NOT RUN, environment failure (connection refused) | PASS after the container API update, NOT RUN with the original library (environment failure, Section 4.2) |
| Status | BLOCKED | BLOCKED (Gate 1) |
| G1 Host, scope and fundamentals | 0, R1.1 client aimed at a process the test does not start, CustomerControllerTests.cs:9 | 2 |
| G2 Contract and HTTP assertions | 0, R2.1 status only, R2.9 Theory rows without `scenario` | 1, R2.5 `Location` asserted but never followed, R2.1 Delete and Get-404 tests assert status only, R2.4 no not-found row for Update, R2.7 no boundary pair, R2.9 no `scenario` |
| G3 Dependency replacement | 0, R3.1 R3.2 R3.9 real database and real GitHub behind the external process | 1, R3.3 `RemoveAll(typeof(IHostedService))` with no list of what was removed, CustomerApiFactory.cs:43 |
| G4 Real infrastructure | 0, R4.5 no detection, R4.3 compose file uses `image: postgres` | 1, R4.3 image tag left to the library default, R4.5 no Docker detection and no skip |
| G5 Data isolation and stability | 1, no data written so nothing collides, but the suite cannot run three times (R5.5) | 1, R5.2 `Single()` and `BeEmpty()` without stating that the class owns the database, R5.3 `// Cleanup` after the assertions in GetAll |
| G6 Practical, observability and trust | 0, R6.1 no trait, R6.4 skip without a cause, the process must be started by hand | 1, R6.1 no trait, R6.4 no skip when Docker is absent, R6.5 no test output, R6.8 tier not stated |
| Total | 1/12 | 7/12 |

Production changes for testability found in Suite B (Section 5.3): a public marker interface
(legitimate), the validation error key changed from `Customer` to `GitHubUsername` so the test can
read `Errors["GitHubUsername"]` (legitimate contract change, a client-visible improvement, needs the
row approved), and `RemoveAll(IHostedService)` in the test factory (per service classification
missing, R3.3).

Lesson. The "cannot hijack" failure looked like 13 red tests. It was a library that no longer speaks
the daemon's protocol. Detect reads package versions before the first run for exactly this reason.
The fix touched only the factory, the 13 test files stayed byte-identical, and that diff is the
evidence that the tests were sound.

## Example 2. Mode `review`: Gate 4 catches a shared database

Suite B's five classes were run against one shared PostgreSQL container instead of one per class,
with the test files unchanged. Each class still had its own host, and classes ran in parallel, which
is the layout of a suite whose factories all read the same connection string from configuration.

```
run 1  Failed: 4, Passed: 9      run 2  Failed: 4, Passed: 9      run 3  Failed: 4, Passed: 9
```

| Test | Run 1 | Run 2 | Run 3 |
|---|---|---|---|
| Create_ReturnsValidationError_WhenGitHubUserDoestNotExist | Failed | Failed | Failed |
| Delete_ReturnsNotFound_WhenCustomerDoesNotExist | Failed | Failed | Failed |
| GetAll_ReturnsAllCustomers_WhenCustomersExist | Failed | Failed | Failed |
| Get_ReturnsCustomer_WhenCustomerExists | **Failed** | Passed | **Failed** |
| Update_UpdatesUser_WhenDataIsValid | Passed | **Failed** | Passed |
| the other eight | Passed | Passed | Passed |

Every failure: `Npgsql.PostgresException : 23505: duplicate key value violates unique constraint
"pg_type_typname_nsp_index"`. Five hosts ran the application's `CREATE TABLE IF NOT EXISTS` at the
same time on one database, which PostgreSQL does not serialize. The first test of each class paid
for it, the next test rebuilt the host and passed. The counts say "4 failed" three times. The
identities say the suite is unstable. Gate 4 FAIL, Status BLOCKED, G4 = 1 under R4.4 (more than one
fixture owned schema creation) and G5 = 1 under R5.4.

Lesson. The expected failure was `Single()` on a list that another class had written to (R5.2). It
did not show in three runs. The schema race showed instead. The review reports what was measured,
not what was predicted, and the rulebook gained the schema-owner sentence in R4.4 from this run.

## Example 3. Mode `write`, characterization: contract, 35 tests, three runs, review

The API existed, so the mode is characterization. Every row was derived from the controller, the
validators, the value objects and the service (Section 2.3) and marked `?`. The table was approved
before any test was written. Tier: in-process host plus container, risk Medium (create, update and
delete with persistence), stage not stated by the owner so the risk table applies unchanged. `Req` is
`none` throughout, the API has no ticket ids. Security partitions
are `N/A`: no endpoint has an effective authorization requirement (no attributes, no fallback policy,
no conventions). Concurrency and timeout rows are `N/A` at this risk level.

| Req | Rule | Given | When | Then | Partition | Test |
|---|---|---|---|---|---|---|
| none | R1 | valid request, username known to the fake | POST /customers | 201, body equivalent, `Location`, GET read-back equal, fake called once | valid | `Create_ReturnsCreatedWithLocationAndPersists_WhenDataIsValid` |
| none | R1 | email "not-an-email" | POST | 400, `Errors["Email"][0]`, not persisted, fake not called | invalid | `Create_ReturnsValidationErrorAndDoesNotCallGitHub_WhenEmailIsInvalid` |
| none | R1 | full name "R2D2 Unit" `?` | POST | 400, `Errors["FullName"][0]` = "{v} is not a valid full name" | invalid | `Create_ReturnsValidationError_WhenFullNameHasDigits` |
| none | R1 | username "bad--name" `?` | POST | 400, `Errors["GitHubUsername"][0]` = "{v} is not a valid username", fake not called | invalid | `Create_ReturnsValidationErrorAndDoesNotCallGitHub_WhenUsernameIsMalformed` |
| none | R1 | username of 39 and of 40 characters `?` | POST | 201, then 400 keyed by GitHubUsername | boundary pair | `Create_UsernameLengthBoundary_IsAcceptedAtLimitAndRejectedPastIt` (Theory, 2 rows) |
| none | R1 | date of birth today and tomorrow `?` | POST | 201, then 400 "Your date of birth cannot be in the future" | boundary pair | `Create_DateOfBirthBoundary_IsAcceptedTodayAndRejectedTomorrow` (Theory, 2 rows) |
| none | R1 | FullName, Email, GitHubUsername each null, "", "   " `?` | POST | 400 keyed by the field, "'<Display Name>' must not be empty." | null, empty, whitespace | `Create_ReturnsValidationError_WhenRequiredStringFieldIsMissing` (Theory, 9 rows) |
| none | R1 | username the fake does not know | POST | 400, `Errors["GitHubUsername"][0]` names the username, not persisted | upstream says no | `Create_ReturnsValidationError_WhenGitHubUserDoesNotExist` |
| none | R1 | username the fake answers 403 for | POST | 500, not persisted | upstream failure | `Create_ReturnsInternalServerErrorAndPersistsNothing_WhenGitHubIsThrottled` |
| none | R1 | empty JSON body `?` | POST | 400 | invalid (no body) | `Create_ReturnsBadRequest_WhenBodyIsEmpty` |
| none | R2 | customer created by the test | GET /customers/{id} | 200, body equivalent | valid | `Get_ReturnsCustomer_WhenCustomerExists` |
| none | R2 | random Guid `?` | GET | 404, `ProblemDetails.Title` "Not Found" | not-found | `Get_ReturnsNotFoundProblem_WhenCustomerDoesNotExist` |
| none | R2 | id "abc" `?` | GET /customers/abc | 404, empty body (route constraint) | invalid (malformed key) | `Get_ReturnsNotFoundWithEmptyBody_WhenIdIsNotAGuid` |
| none | R3 | two customers created by the test `?` | GET /customers | 200, contains both by equivalence | valid | `GetAll_ContainsEveryCustomerCreatedByThisTest_WhenCustomersExist` |
| none | R3 | nothing created, class owns the database | GET /customers | 200, empty list | valid (empty) | `GetAll_ReturnsEmptyList_WhenNoCustomerExists` |
| none | R4 | existing customer, valid body `?` | PUT /customers/{id} | 200, body equivalent, GET shows new values and the same Id, fake called once | valid | `Update_ReturnsOkAndPersistsNewValues_WhenDataIsValid` |
| none | R4 | random Guid, valid body `?` | PUT | 404 | not-found | `Update_ReturnsNotFound_WhenCustomerDoesNotExist` |
| none | R4 | random Guid, invalid email `?` | PUT | 404, not 400 | not-found before validation | `Update_ReturnsNotFoundBeforeValidating_WhenCustomerDoesNotExistAndEmailIsInvalid` |
| none | R4 | existing customer, invalid email | PUT | 400, `Errors["Email"][0]` | invalid | `Update_ReturnsValidationError_WhenEmailIsInvalid` |
| none | R4 | existing customer, unknown username | PUT | 400, `Errors["GitHubUsername"][0]` | upstream says no | `Update_ReturnsValidationError_WhenGitHubUserDoesNotExist` |
| none | R4 | existing customer, date of birth tomorrow `?` | PUT | 400 keyed by DateOfBirth | boundary-above | `Update_ReturnsValidationError_WhenDateOfBirthIsTomorrow` |
| none | R4 | existing customer, throttled username `?` | PUT | 500, GET still returns the old values | upstream failure | `Update_ReturnsInternalServerErrorAndKeepsOldValues_WhenGitHubIsThrottled` |
| none | R5 | customer created by the test `?` | DELETE /customers/{id} | 200, GET then 404 | valid | `Delete_ReturnsOkAndCustomerIsGone_WhenCustomerExists` |
| none | R5 | random Guid | DELETE | 404 | not-found | `Delete_ReturnsNotFound_WhenCustomerDoesNotExist` |
| none | R5 | id "abc" `?` | DELETE /customers/abc | 404 | invalid (malformed key) | `Delete_ReturnsNotFound_WhenIdIsNotAGuid` |

25 methods, 35 test cases. Evidence for every row: `char-run1.trx`, `char-run2.trx`, `char-run3.trx`.

The factory (one per class, `IClassFixture`) starts a pinned `postgres:16-alpine` container and a
WireMock server, swaps `IDbConnectionFactory` and the named client, and registers three stubs: a
valid user, a 39-character user and a throttled user that answers 403. Hosted services: the
application registers none, nothing is removed. Production changes: none. Every class carries
`[Trait("Category","Integration")]`, uses `DockerFact` and `DockerTheory` attributes that skip with
a reason when `docker info` fails, logs every response through `ITestOutputHelper`, and deletes what
it created in `DisposeAsync`. The DELETE class does not clean up through DELETE: it owns its database.

```
run 1  Passed: 35, Failed: 0, Skipped: 0, Duration: 1 s
run 2  Passed: 35   run 3  Passed: 35          IDENTICAL BY IDENTITY: YES
dotnet test --filter "Category=Integration"    Passed: 35
dotnet test --filter "Category!=Integration"   No test matches the given testcase filter
```

Every `?` row was observed by the first run: the API answered what the hypothesis said. That is the
first of the two approvals (Section 2.3). The second, the contract owner confirming that each observed
behavior is wanted, had not happened when this example was written, so the 18 rows stay `?` and the
definition of done is reported as not met. Four observations were worth the confirmation step:

- An unknown id with an invalid email on PUT answers 404, not 400. The not-found check runs before
  the value objects validate. This is now a documented row, and a question for the contract owner.
- The NotEmpty message for `GitHubUsername` is `'Git Hub Username' must not be empty.`, produced by
  splitting the property name. A client sees "Git Hub". The row records the exact text so a change
  is noticed, and the review asks whether the display name should be configured.
- An empty JSON body answers 400 with a `ProblemDetails`, a malformed id answers 404 with no body.
  Two different shapes for two different kinds of "bad request".
- The fake receives exactly one call for a valid create and none for a request rejected by the
  value objects. The side-effect assertion proves the validation order without reading the code.

What the pre-existing 13-test suite missed, now covered: the read-back after create, update and
delete (persistence), the `Location` follow, the side effects on the fake, both boundary pairs, nine
null, empty and whitespace rows, the malformed key on GET and DELETE, the not-found row and the
not-found-before-validation row on PUT, the throttled row on PUT with old values kept, and "nothing
persisted" after every rejection.

Review of the new suite, in the Section 5.5 template:

```
## Review: Customers.Api.Tests.Integration (characterization), 2026-11-13
Mode: characterization
Tier: in-process host plus container (risk: Medium, stage: not stated, create, update, delete with persistence)
Environment: Docker AVAILABLE (Server 28.0.4)
Needs: Docker yes, real database yes (PostgreSQL in a container), external services: GitHub (WireMock)
Runs without Docker: none, every class needs the container
Command: dotnet test (VSTest, xUnit 2.9, net8.0 test project referencing the net6.0 API)

Gates
Gate 1 PASS  27/27 rows mapped, 35/35 cases with evidence (char-run1.trx), 18 ? rows listed under open risks
Gate 2 N/A   characterization
Gate 3 PASS
Gate 4 PASS  3 identical runs by test identity (35 passed, 0 skipped, containers recreated, 3 TRX attached)
Status: PASS

Scorecard (diagnostic, does not override gates)
| Group | Score | Deducted for |
|---|---|---|
| Group G1 Host, scope and fundamentals | 2 | |
| Group G2 Contract and HTTP assertions | 2 | |
| Group G3 Dependency replacement | 2 | |
| Group G4 Real infrastructure | 2 | |
| Group G5 Data isolation and stability | 2 | exception: GetAll_ReturnsEmptyList asserts BeEmpty, reason stated in the test (class owns the database) |
| Group G6 Practical, observability and trust | 2 | |
Total: 12/12

Security rows: N/A, no endpoint has an effective authorization requirement (no attributes, no fallback policy, no conventions)
Hypotheses: 0 confirmed by the contract owner, 18 awaiting confirmation
Production changes for testability: none
Failures classified: environment 0, test defect 0, implementation defect 0, contract ambiguity 0

Rework: legitimate 0, contract change 0, weakening 0, implementation-driven 0. Ratio 0/35 (session change log, no edit after the first run)
Unrequested: none (characterization, no production change in this session)

Coverage (diagnostic)
Status: MEASURED
Runner and collector: VSTest, coverlet.collector 6.0.2
Command: dotnet test --no-build --collect:"XPlat Code Coverage"
Report: TestResults/d9f361dc-8728-4499-a51a-a4039a7bcba6/coverage.cobertura.xml
Instrumented: Customers.Api. Not instrumented: none (no compose tier in this suite)
Line 98.6% (363/368), Branch 93.3% (28/30), Method: not provided by the report
Uncovered branches:
- CustomerService.CreateAsync, CustomerService.cs:24-26: "a user with this id already exists". Unreachable through HTTP, the id is generated by the mapper. Business rule? (ask: dead code at the boundary, or a unit test)
- CustomerId.Validate, CustomerId.cs:10-11: empty Guid rejected. Unreachable through HTTP, the not-found check answers first. Business rule? (ask)
Contract rows without a test: none

Open risks: 18 ? rows awaiting confirmation, concurrency and timeout rows N/A at risk Medium, no security rows, the 500 body is the developer exception page (Development environment of the factory)
Definition of done: not met, 18 hypothesis rows await confirmation by the contract owner
Trust assessment: gates PASS, scorecard 12/12, rework 0/35, coverage measured, 2 uncovered branches both unreachable through HTTP, done blocked on hypothesis confirmation
Next actions (max 3): ask the owner whether PUT should validate before the not-found check, decide whether the duplicate-id branch is dead code, configure the display name for GitHubUsername or accept "Git Hub Username" as the contract
```

A 12/12 scorecard with a PASS status is not a statement that the API is correct. It states that
every approved row has a test with evidence, and that the practice meets the rulebook. Correctness
is decided when the contract owner confirms the 18 observed behaviors, and the three questions in
"Next actions" are where that decision is hardest.

## Example 4. Environment reporting: the same suite without Docker

The suite was run once more with the Docker endpoint pointed at a closed port, in one process only.

```
DOCKER_HOST=tcp://127.0.0.1:1 dotnet test --no-build
Skipped! - Failed: 0, Passed: 0, Skipped: 25, Total: 25, Duration: 249 ms
every entry: NotExecuted (Docker daemon not reachable (docker info failed), start Docker Desktop or set DOCKER_HOST)
```

The report for that run reads:

```
Environment: Docker NOT AVAILABLE (docker info failed with DOCKER_HOST=tcp://127.0.0.1:1)
Needs: Docker yes, real database yes (PostgreSQL), external services: GitHub (WireMock)
Runs without Docker: none in this project
Not run: 25 methods (35 cases), skipped with the reason above
Gate 4: NOT RUN
Status: BLOCKED
```

Two details matter. A skipped `[Theory]` counts once in the runner's totals (25 methods, not 35
cases), so the report states both numbers. And nothing failed: a missing daemon is an environment
failure (Section 5.4), it is not red evidence and it is not flakiness. On a CI job the same
condition fails the environment step before the test step runs (R4.5).

## Example 5. Mode `write` without Docker: the in-process tier for rows that do not touch persistence

The same API, the same contract rows whose Then names no persisted state: validation answers,
upstream answers, routing answers. Tier: in-process host with every seam faked, risk Low for these
rows, stage not stated. The factory swaps the connection factory for an in-memory SQLite connection that only the
startup initializer uses, swaps the repository for an in-memory fake, and points the named client at
WireMock. R3.5 is honored by scope, not by exception: no row in this suite claims persistence, and
the suite says so in the factory's header comment.

```
DOCKER_HOST=tcp://127.0.0.1:1 dotnet test        (the daemon is unreachable on purpose)
run 1  Passed: 9      run 2  Passed: 9      run 3  Passed: 9        IDENTICAL BY IDENTITY: YES
```

Nine cases in one class: invalid email (400, fake not called), date of birth tomorrow (400), full
name null, empty and whitespace (Theory, three rows with `scenario`), unknown GitHub user (400 naming
the user), throttled user (500), empty body (400), malformed id on GET (404, empty body). The same
rows passed in Example 3 against the container, which is the evidence that they never depended on
the database.

Detect for this suite reads: Docker N/A (no container reference, no compose file, no container in the
factory), real database no, external services GitHub (WireMock). Runs without Docker: every class.
The review's open risks name what this tier cannot say: nothing about persistence, nothing about the
real SQL, and the repository fake would hide a mapping defect between the DTO and the table. Those
rows belong to Example 3's tier. One suite per tier, each honest about its reach, is the layout the
rulebook asks for, and it is cheaper than running everything in containers.
