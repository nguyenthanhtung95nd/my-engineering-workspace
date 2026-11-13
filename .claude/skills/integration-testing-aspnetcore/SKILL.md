---
name: integration-testing-aspnetcore
description: >
  Writes and reviews ASP.NET Core integration tests from approved Given/When/Then contracts that
  describe one HTTP call, its status, body, persisted state and side effects on fakes. Hosts the
  application in-process with WebApplicationFactory, runs real databases with Testcontainers, replaces
  external HTTP APIs with WireMock, keeps authorization real behind a test authentication handler, and
  detects Docker only when the suite needs it. Supports TDD for new endpoints and characterization for
  existing APIs with two-step approval of hypotheses. Reviews suites against four hard gates, a
  six-group 0/1/2 scorecard, a checklist for production changes made for testability, and a definition
  of done. Use when the user asks to write, add, review or score integration tests, API tests, endpoint
  tests, WebApplicationFactory or Testcontainers setups, test an ASP.NET Core API end to end, pick a
  test tier, check why an integration suite is flaky, or decide what needs Docker. Scope is ASP.NET
  Core over HTTP.
---

# Integration Testing (ASP.NET Core)

The rulebook is [integration-testing-rules.md](integration-testing-rules.md). Read it once per session
before either mode. Examples: [EXAMPLES.md](EXAMPLES.md). Scope: ASP.NET Core over HTTP and its hosted services. Other platforms need their own adapter.

**Mindset.** An integration test is an executable contract of the HTTP boundary. Humans define the
conversation, AI may implement, the test proves the pieces work together through the real pipeline.
Real where you own it, faked where you do not. The test owns its infrastructure and data. Trust is measured.

## Execution boundaries (rulebook Section 6.4, no exceptions)

Allowed: read code and configuration, read-only probes, build and run tests against containers the
fixtures start, create files in the test project or a scratch directory. Ask first, naming the item:
pull an image, start Docker Desktop, add a package, install anything, touch a database a fixture did
not start. Never: delete what you did not create, reach production or staging, store secrets or real personal data. Production code changes only within what the user asked for and Section 5.3.

## Detect first (both modes)

1. Find the test projects. Classify the runner (VSTest or Microsoft Testing Platform) and the framework.
   Read `TargetFramework` and the package versions. Anything you add must be compatible with both and with the Docker daemon.
2. Say what the suite needs in three lines: **Docker**, **a real database**, **external services**
   replaced by fakes (names). Check that no test setting can reach a production system (R3.9).
3. Only when Docker is needed, probe `docker info`, record exit code and server version, and list the
   images the suite pulls. Otherwise report `Docker: N/A`. State which classes **run without Docker**.
4. Find the entry point the factory uses, how the application registers its seams, the effective
   authorization requirement of each endpoint (attributes, fallback policy, conventions, handlers),
   the tier the suite sits in (Section 7), and the naming and layout.

## Mode `write`

- **TDD.** The endpoint or behavior does not exist yet. Red evidence is required (Gate 2).
- **Characterization.** The API exists. Tests pin current behavior, every row derived from code is a
  hypothesis (`?`) that stays `?` until the contract owner confirms it after the run (Section 2.3).

1. **Contract.** Build the Given/When/Then table (rulebook Section 2) with a `Req` column. Source
   order: acceptance criteria, then derivation from requirement or code (Section 2.3), then questions.
   Partitions as applicable: valid, invalid, not-found, upstream failure, boundary pair, null, empty,
   whitespace, unauthenticated, forbidden, sensitive fields, overposting, and the reliability rows the
   risk level calls for (Section 7). Else `N/A` with a reason. Never invent a rule.
2. **Stop and show the contract.** No test is written until the user approves the table. A `?` row is
   approved as a hypothesis to verify, not as a requirement.
3. **Choose the tier and build the factory.** State the risk level, the product stage as the contract
   owner stated it (Explore, Expand, Extract, or not stated) and the tier (Section 7). Typically
   one `WebApplicationFactory` subclass owns the container, the mocks, the test authentication handler
   and the seams (G3, G4). Classify hosted services one by one (R3.3). List production changes (Section 5.3).
4. **Write tests from rows.** One row is one test case. Same behavior with different data becomes one
   `[Theory]` with a `scenario` column. Each test asserts the observables its row names (R2.1), owns
   its data and cleanup (G5), carries `[Trait("Category","Integration")]`, logs on failure (R6.5).
5. **Run.** TDD: red counts only when the test compiled, reached the HTTP call, and failed at the
   behavior assertion. Classify every failure first (Section 5.4), only an implementation defect is red.
   Characterization: report what each `?` row observed and ask the owner to confirm or reject it.
6. **Hand off to implementation** only when the user asked for it, otherwise stop with the red evidence.
   When implementing, classify each failure before editing, escalate ambiguity, log every test edit (5.4).
   Implement one contract row, or one Theory, per batch. A structural change that keeps the suite green
   goes in its own commit before the behavior change. Stop after the second attempt at the same failing
   test and escalate the row as ambiguous (5.4). Every endpoint, parameter, field or branch added must
   map to a row; anything else is unrequested functionality and is removed or proposed as a row first.
7. **Run three times** with the environment recreated. Compare names and failure reasons, not counts.
8. **Check the definition of done** (Section 5.6) and report what is not met, unconfirmed `?` rows included.

## Mode `review`

1. Run Detect. If the suite cannot run here, report `Environment: NOT AVAILABLE` with the reason, list
   what does run, and continue the review on the code. Gate 4 is then `NOT RUN` and Status is BLOCKED.
2. **Locate the contract.** Without an approved contract, derive one with `?` rows (Section 2.3) and
   report Gate 1 as BLOCKED until it is confirmed. Existing tests are never the contract.
3. **Determine the mode.** TDD needs recorded red evidence from before the endpoint existed.
   Characterization reports Gate 2 as `N/A`. Unknown origin is reported as missing evidence.
4. **Gates** (Section 5.1): Gate 1 contract coverage with evidence, Gate 2 red evidence, Gate 3 every
   test asserts what its row names and every skip names its cause, Gate 4 three identical runs by identity.
5. **Scorecard.** G1 host and fundamentals, G2 contract and HTTP assertions, G3 dependency replacement,
   G4 real infrastructure, G5 data isolation, G6 practical, observability and trust, each 0, 1 or 2
   (Section 5.2) with `file:line` and the rule id. A stated exception costs nothing. Never mix gates and groups.
6. **Security and production changes.** Count security rows against the endpoints with an effective
   authorization requirement. List each production change and answer its Section 5.3 question.
7. **Failures, rework and coverage.** Classify failures (Section 5.4). Rework only from change history, else `NOT AVAILABLE`. Coverage only from a report of this session, else `NOT MEASURED` with a reason.
8. Output exactly the template in Section 5.5: hypotheses, open risks, definition of done, trust assessment.

## Do not

- Write a test before the contract is approved, invent a partition row, or promote a `?` row to
  confirmed because the run was green. Only the contract owner confirms.
- Aim an `HttpClient` at a URL of a process the test does not start, or at a production system.
- Replace the thing under test, remove hosted services wholesale, or bypass an authorization policy.
- Assert implementation details, `Single()` on a shared list, wait with `Task.Delay`, or edit a failing
  test or production code before the failure is classified (Section 5.4).
- Treat a Docker failure, a library version mismatch or a compile error as red evidence or flakiness,
  or declare done on exit code 0. Done is Section 5.6.
- Implement several rows in one batch, mix structural and behavior changes in one commit, retry the
  same fix a third time, add anything the contract did not ask for, or choose a tier without a stated
  risk level and stage.
