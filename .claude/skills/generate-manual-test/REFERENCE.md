# Reference

Read this before writing the matrix. It decides how many cases exist, what order they go in,
which states are testable, and what goes in Test Data.

## 1. Grouping, one case or several

The test is the **expected result**, not the input.

**Group into one case, with several values in Test Data, when all of these hold.**

- The expected result is the same sentence for every value.
- The path through the product is the same, same screen, same control, same request.
- A failure on any one value would be diagnosed the same way.

Example. A ticket says a chosen time zone is saved. Choosing `UTC`, `Europe/London` and
`America/Argentina/Buenos_Aires` all produce "the control shows the chosen value and it
survives a reload". That is one case with three values, not three cases.

**Split into separate cases when any of these hold.**

- The expected results differ, even slightly. Two refusals that return different messages are
  two behaviours.
- The path differs. Choosing through the interface and sending through an endpoint are two
  cases even when the stored outcome matches.
- The preconditions differ. A case needing an account that has never signed in cannot share a
  row with one needing an account that already has a value.
- One is a promise about state and the other about absence of state. "The value is written"
  and "nothing is written" are always separate.

**Never split** because you want the suite to look thorough, or because a validation rule has
several branches. Branches of a validation rule are unit test territory unless the ticket
describes each branch as a distinct user outcome.

## 2. Positive, negative, regression

Classify by **what the case proves**, not by whether the result is an error.

| Group | Proves | Typical shape |
|---|---|---|
| Positive | A promise the ticket makes is kept when the actor does the expected thing | The feature works, the value persists, the right thing is shown |
| Negative | The system refuses, guards or limits correctly when something is wrong, missing, or not permitted | A refusal names what was wrong, an unauthorised actor is stopped, a state the ticket forbids does not occur |
| Regression | Behaviour the ticket did **not** change is still intact | Existing screens, existing data, a neighbouring feature that shares the changed path |

Rules.

- A case where the system correctly does nothing is **negative** when the ticket frames it as
  a prohibition, and **positive** when the ticket frames it as a guarantee. Follow the ticket's
  own wording.
- A case that only confirms the deploy harmed nothing is **regression**, even if it passes
  through the new feature.
- Order the matrix positive, then negative, then regression, and number sequentially so the
  ID carries the group. Do not add a group column.

**Regression scope is bounded by the ticket.** A regression case may only come from existing
behaviour the ticket itself names, or from a requirement that was confirmed. The ticket saying
"this changes no figure and no label", or naming a neighbouring feature the change sits beside,
is what authorises a regression case about it.

You MUST NOT read the source code to discover what else the change touches and then widen the
regression scope from that. Shared files, shared helpers and shared request paths are invisible
to a tester, so a case built on them is not a manual test. If you believe an untouched area is
at risk and the ticket is silent, raise it as an open question for the ticket author instead of
adding a case nobody asked for.

## 3. Reachability

Three tiers. Only the third is a reason not to write a case.

| Tier | Meaning | What to do |
|---|---|---|
| **UI reachable** | A person can reach the state and see the result entirely in the product | Write the case using the interface only |
| **Tool reachable** | The result is observable, but reaching or holding the state needs a QA tool: a database write, an API call, DevTools offline mode or request blocking, an environment setting | Write the case. Name the tool and the exact setup in Preconditions |
| **Unreachable** | No tool available to QA can produce the state, or the state has no observable effect | Do not write a case. Report it, and say where it is covered instead |

**Do not treat "not reachable through the interface" as "not testable".** That mistake drops
real requirements. A state that only a database update can arrange is still a manual test case,
it simply has a longer Preconditions cell.

Before declaring something unreachable, check all of these.

- Can a database write arrange it?
- Can an API call arrange it?
- Can an environment or account setting arrange it?
- Can a browser tool hold it still long enough to observe, for example offline mode stopping a
  write so a transient state persists?
- Can a second account, a second session or a second browser produce it?

If every answer is no, it is unreachable. Say so in plain words and name where it is covered.

## 4. Test data

**Literal when it can be literal.** A value the tester types or selects goes in the cell as
written, ready to copy.

**Dynamic data.** Dates, identifiers and generated values rot the moment they are written. Give
the **rule**, not the value. Write "today's date" or "the identifier of the report created in
step 2", never a fixed date or a pasted id. If the rule needs a calculation the tester cannot do
in their head, put the calculation in the steps.

**Environment dependent data.** URLs, account names, region names, queue names and connection
details differ per environment. Name them by role in Preconditions, for example "an account
holding the Admin role on the environment under test". Do not hardcode one environment's values
into the matrix, and never put a credential in it.

**When the ticket does not supply enough data.** You MUST NOT invent a business constraint to
fill the gap. No made up limits, no assumed formats, no example values that imply a rule the
ticket never stated. Instead, state in Preconditions what the tester must obtain and from whom,
and raise it as an open question. A matrix that says "obtain the approved brand list from the
product owner" is correct. A matrix that quietly uses three brand names you chose is not.

**Account state is data.** "An account that has never signed in" and "an account that already
holds a value" are different data, and they belong in Preconditions in full.
