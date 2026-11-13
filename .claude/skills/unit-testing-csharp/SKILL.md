---
name: unit-testing-csharp
description: >
  Writes and reviews C# unit tests from approved Given/When/Then contracts. Supports TDD for new
  behavior and characterization testing for existing behavior. Reviews human- or AI-written test
  suites against four hard gates and a six-group 0/1/2 diagnostic scorecard. Treats code coverage as
  measured diagnostic evidence, not proof of correctness. Use when the user asks to write tests, add
  tests, do TDD, characterize legacy code, review tests, score test quality, check coverage, or verify
  implementation behavior. Uses xUnit v3 as reference syntax, with mappings to NUnit and MSTest.
---

# Unit Testing (C#)

The full rulebook is [unit-testing-rules.md](unit-testing-rules.md). Read it once per session
before either mode. Worked examples are in [EXAMPLES.md](EXAMPLES.md).

**Mindset.** Tests are executable contracts, not code coverage. AI may generate the implementation.
Humans define the expected behavior, tests provide the evidence, and engineering practice establishes trust.

## Detect first (both modes)

1. Find test projects by inspecting solution and project files, package references in `.csproj` and
   `Directory.Packages.props`, test SDK and runner properties, `xunit.runner.json`, `.runsettings` and
   Microsoft Testing Platform settings. Do not rely on direct framework package references alone.
   A project that cannot be classified is reported as unknown.
2. If several solutions or test projects exist, list them and ask which one is in scope.
3. Note the framework and the assertion library (`FluentAssertions`, `Shouldly`, `AwesomeAssertions`).
   Use the rulebook's xUnit v3 idioms, and map through rulebook Section 4.2 for NUnit or MSTest.
4. Find how tests run (`dotnet test` at the solution, runner configuration files).
5. Match the naming and folder layout of the existing tests.

## Mode `write`

Two sub-cases exist. State which one applies before starting.

- **TDD.** The behavior does not exist yet. Red evidence is required (Gate 2).
- **Characterization.** The implementation already exists. Tests pin current behavior, and every row
  derived from code is a hypothesis (`?`) until a human confirms it is intended behavior. Gate 2 is
  reported as `N/A (characterization)`, never as a pass.

Steps:

1. **Contract.** Build the Given/When/Then table (rulebook Section 2). Source order: acceptance
   criteria first, then derivation from the requirement or the code, then questions to the user.
   Existing code is never the source of business truth. It is where hypotheses come from.
   Fill the partitions that apply: valid, invalid, not-found for lookups, a boundary pair when
   a threshold exists, an invariant row when the class holds state across calls, and an idempotent
   row when an operation can be repeated. Null, empty and whitespace inputs are one row each.
   Otherwise write `N/A` with a reason. Never invent a rule to fill a partition.
2. **Stop and show the contract.** No test is written until the user approves the table.
3. **Write tests from rows.** One row is one test case. Rows with the same behavior and different
   data become one `[Theory]`, and every Theory data row carries a `scenario` value (rulebook R2.4).
   Different behaviors get separate methods. Name each method from its rows as
   `Method_Scenario_ExpectedResult`. Apply rulebook Section 3.
4. **Run and expect red (TDD only).** Red counts only when the test compiles and fails at the
   behavior assertion, or at a `NotImplementedException` thrown by an unimplemented production
   placeholder. Compile errors and infrastructure errors are not evidence. Record names and reasons.
5. **Hand off to implementation.** The rule is to make tests green by changing production code.
   Every test edit after this point is logged and classified (rulebook Section 5.3) as a legitimate
   correction, a contract change requiring approval, test weakening (rejected), or
   implementation-driven rework. Implement one contract row, or one Theory, per batch. A structural
   change that keeps the suite green (rename, extract, move) goes in its own commit before the
   behavior change. Stop after the second attempt at the same failing test and escalate the row as
   ambiguous. Every public member, parameter or branch added must map to a row; anything else is
   unrequested functionality and is removed or proposed as a new row first.
6. **Run three times.** Compare the passed, failed and skipped test names and the failure reasons.
   Counts alone are not enough.

## Mode `review`

1. Run the suite. If it does not run with one command, that is the first finding.
2. **Locate the contract.** Find the approved contract or acceptance criteria. If none exists, do not
   treat the existing tests as business truth. Derive a proposed contract, mark code-derived rows `?`,
   and ask for confirmation. Until then Gate 1 is reported as BLOCKED, not PASS.
3. **Determine the mode.** Decide whether the suite was created test-first or characterizes existing
   behavior. TDD requires recorded red-run evidence from before implementation. Characterization
   reports Gate 2 as `N/A`. If the history cannot be established, do not assume TDD and do not claim
   red evidence. Report the missing evidence.
4. **Gates** (rulebook Section 5.1): Gate 1 contract coverage, Gate 2 red evidence, Gate 3 meaningful
   assertions on an observable outcome of the Act, Gate 4 three identical runs by test identity. Any
   failure sets `Status: BLOCKED`. Do not stop. Continue to the scorecard as a diagnostic.
5. **Scorecard.** Six groups G1 to G6 scored 0, 1 or 2 (rulebook Section 5.2), with `file:line` and
   the rule id for each deduction. The score is diagnostic and never overrides a failed gate.
   Gates are numbered Gate 1 to Gate 4 and groups are G1 to G6. Never mix the two in a report.
6. **Rework.** Count only from Git history, diffs or recorded change logs (Section 5.3). Without such
   evidence report `Rework: NOT AVAILABLE`. Never infer past edits from the current test code.
7. **Coverage.** Diagnostic only, never a gate. Follow rulebook Section 6: detect the runner, run the
   collector, read the Cobertura file, report only metrics the report provides, list uncovered
   branches by name with one question each. Without a collector report `NOT MEASURED` with the reason.
8. Output exactly the template in rulebook Section 5.4.

## Do not

- Write tests before the contract is approved, or invent partitions to fill a table.
- Treat a compile error or a broken test environment as red evidence, or assume a green suite was TDD.
- Edit a test to make an implementation pass. Classify and log every post-implementation edit.
- Implement several rows in one batch, mix structural and behavior changes in one commit, retry the
  same fix a third time, or add anything the contract did not ask for.
- Mock a class that has no external dependency. Prefer hand-written fakes behind interfaces.
- Disable parallelization globally. Serialize shared-resource tests with the framework mechanism:
  xUnit `[Collection]`, NUnit `[NonParallelizable]` or a parallel scope, MSTest `[DoNotParallelize]`.
- Assert values from `static` counters, `Random` or `DateTime.Now`. Inject `TimeProvider`.
- State a coverage number, a method coverage figure or a rework count that no report or history supports.
