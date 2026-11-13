---
name: generate-manual-test
description: Generates an execution-ready manual test case matrix from a ticket, written from a manual tester's seat rather than from the implementation. Use when the user asks for manual test cases, a test case matrix, QA test cases, or UAT cases for a ticket, story or feature.
---

# Generate Manual Test

Goal: every case covers a stated requirement, contains no assumption, and can be executed
independently by a manual tester.

## The governing rule

A manual tester may use **any tool** their team gives QA: the database, the AWS console,
browser DevTools, an API client. What they do **not** have is the source code.

The boundary is knowledge, not tooling.

| Allowed | Forbidden |
|---|---|
| Read a database column, call an endpoint, use any QA tool | Use a limit, regex, cap or constant that only exists in code |
| Arrange a state with a tool, then observe the result in the product | Quote an error string you read in a source file |
| Any environment access the team already grants QA | Design a case around which internal guard fires first |

**If you had to read the implementation to know a value, the case does not belong here.** Having
read the code, use that knowledge for one purpose only: confirming a case is reachable.

## Requirement sources

Read the **whole** ticket. Every one of these generates candidate cases when present.

- Description and background, for the behaviour the change promises
- Scope or requirements
- Acceptance criteria
- Business rules
- QA or test guidance
- Clarifications that were **confirmed**, and only those. Record where each was confirmed.
  An unconfirmed opinion in a chat thread is not a requirement.

Implementation guidelines are context, not a source of cases. They say how to build, not what
to verify. A section missing from the ticket is simply absent, never a reason to invent one.

## Never infer

When the ticket does not state a business rule, a validation rule, a default value or an
expected result, you MUST NOT supply one. Two options, both reported.

1. Write the case for the part that **is** stated, leaving the unstated part out of the
   expected result.
2. Record it as an open question for the ticket author.

Never let a guess reach the Expected Result column. It fails for the wrong reason.

## Workflow

1. Read the ticket in full and list every candidate requirement from the sources above.
2. Convert each into a person. Ask "what does someone do, and what do they see". A clause
   with no observable action and no observable result is not a manual case. Say so.
3. Classify reachability and decide grouping. **Read REFERENCE.md before this step.**
4. Write the matrix.
5. Account for every requirement. Each one MUST end in exactly one of three states, and all
   three are reported. **Test Case**, naming the IDs that cover it. **Open Question**, where
   the ticket is too ambiguous to state an expected result. **Uncovered Requirement**, where
   nothing a tester can do would verify it, naming where it is covered instead. A requirement
   that appears in none of the three has been dropped silently, which is a defect in the suite.

## Columns

Exactly these, in this order. Nothing else.

| Test Case ID | Test Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result |

- **Test Case ID**: sequential from `TC-001`. Order positive, then negative, then regression,
  so ID order carries the grouping. No group column.
- **Test Scenario**: one sentence naming the behaviour under test.
- **Preconditions**: written out in full in every row, including any tool access the case
  needs. Never reference a shared legend. Duplication across rows is correct. A QA tool may
  **arrange the starting state** and **observe the outcome**. It MUST NOT perform the behaviour
  under test. Writing a value straight into the database and then checking the database proves
  nothing about the product.
- **Test Steps**: numbered, each one action a person performs, in a single cell.
- **Test Data**: literal values, or a rule for deriving them. See REFERENCE.md.
- **Expected Result**: observable on screen, in a response, or in a datastore. Phrase it in
  the ticket's language, never in the product's internal constants.
- **Actual Result**: left blank for the tester.

## Formatting

No em dashes, no semicolons, no `<br>` tags. Steps run inline as `1. Do this. 2. Do that.`

## Anti-patterns

- Unit test boundaries leaking in: a maximum length, a character class, a segment count.
- Expected results quoting code constants instead of the ticket's promise.
- Coverage audit columns. Report coverage in prose underneath, not in the grid.
- Cases that submit invalid input through an interface that only offers valid choices.
- Padding. A short suite a tester finishes beats a long one they abandon.

## Output

Print the matrix in chat. Save a file only when asked. Follow it with the three-state
accounting from step 5, in prose.

See REFERENCE.md for grouping, classification, reachability and test data.
See EXAMPLES.md for worked examples and the failure modes this skill was written against.
