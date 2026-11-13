# Examples

## Worked conversion, from ticket clause to case

Ticket clause:

> Default from the browser on first sign-in only: where the stored value is null and the
> browser resolves a zone, write it once. Never overwrite a stored value from the browser
> again, because travelling would otherwise silently redefine what that user's reports mean.

A tester reads two promises in that sentence, so it yields two cases.

| Promise | Case |
|---|---|
| The portal takes my machine's zone the first time, without asking | Set the machine zone, sign in as someone who has never chosen, confirm the portal now shows that zone |
| It never changes that behind my back | Choose a zone, change the machine zone, sign in again, confirm the stored value did not move |

What it does **not** yield is a case about which check runs first, or about what happens when
two internal guards disagree. A tester cannot see either.

## Expected results, written two ways

Ticket clause:

> Reject at write anything the server's zone database cannot resolve, naming the rejected value.

| Wrong | Right |
|---|---|
| The response is 400 and the body reads `{"error":"Time zone 'Mars/Phobos' is not a valid IANA time zone identifier."}` | The request is refused, and the message that comes back names `Mars/Phobos`, the exact value that was sent |

The left column can only be written by someone who has read a source file. The right column
is what the ticket promises, and it still fails the test if the product returns a bare error
code with no value in it.

## An ambiguous requirement becomes an open question, not a guess

Ticket clause:

> Add a time zone control to portal settings: a searchable list of identifiers showing the
> current value.

The ticket never says what the control shows for someone who has never chosen. Three answers
are plausible, which is exactly why you may not pick one.

| Wrong | Right |
|---|---|
| Expected Result: the control reads "not set" | Expected Result covers only what the ticket states, that the control shows the current value when one exists. The unstated case is reported as an open question |

The open question to report:

> The ticket does not say what the control shows before the person has chosen a time zone.
> Candidates are an empty state, the configured business zone, or the browser's zone. Each
> would be correct against a different reading, so no expected result can be written until the
> ticket author decides. Raised with the reporter.

Writing "not set" because you saw that string in a source file is the exact failure this skill
exists to prevent. Writing "the configured business zone" because it felt reasonable is worse,
because it looks like a requirement and nobody will question it.

## Tool based setup, and where setup becomes bypass

A QA tool may put the system into a starting state and may read the outcome. It may never
perform the behaviour the case is testing.

Requirement: the portal records the person's computer time zone the first time they sign in,
and never overwrites a value they chose themselves.

| | Setup, allowed | Bypass, not a test |
|---|---|---|
| Arranging the start | Clear the stored value with a database update, so the account is genuinely in the "has never chosen" state | Nothing to object to here, this is ordinary setup |
| Performing the behaviour | **Sign in through the portal** and let the product decide what to record | Write the value in with an API call or a database update, then report that the value is there |
| Observing the outcome | Read the stored value in the database and read what the control shows | Fine either way, observation is not the behaviour |

The second row is the whole point. The behaviour under test is "the portal decides to record
something at sign in". A case that writes the value itself has removed the product from the
test and proves only that a database accepts an update.

The same line applies to the other direction. To prove a chosen value is never overwritten,
the choice MUST be made through the control, because "a value the person chose" is the
precondition the rule depends on. Arranging it with a direct write tests a different sentence.

## Failure modes this skill was written against

A real run produced a 28 case matrix for a ticket that deserved roughly half that. The faults,
in the order they mattered:

1. **Cases derived from the implementation.** The author had just written the code, so the
   matrix carried a 64 character boundary, a 65 character boundary, a four segment identifier
   and a SQL shaped string. Every one of those numbers came from a regex and a constant. No
   tester reading the ticket would pick them.

2. **A control that cannot accept bad input.** The feature's interface was a searchable list
   of valid identifiers. Eight negative cases described submitting invalid values through it.
   None was reachable.

3. **Duplication across surfaces.** One case checked that the read endpoint returned the
   stored value. Another checked that the control displayed it. Same fact, two rows.

4. **Six columns no executor reads.** Test Group, Test Case Title, Test Technique,
   Traceability, Status and Remarks. Title duplicated Scenario. The rest serve a coverage
   review, which belongs in prose underneath, not in the grid.

5. **A shared preconditions legend.** Rows said "PRE-2, PRE-5" and the tester had to scroll
   back twice per case. Inline and duplicated is correct here.

## Reporting a gap

When a ticket clause cannot become a manual case, write it out rather than dropping it.

> A browser that reports no time zone at all is not reachable by hand, because Chrome and Edge
> always report one and no supported setting changes that. It is covered by the automated test
> for the adoption component.

> The migration file's position in the numbered sequence is a repository fact with no visible
> effect. It is checked at code review and by a build time check that fails on a duplicate number.
