# 04 - Evidence

A test failure IS the next prompt the agent acts on. Rich evidence lets it reproduce, diagnose, and fix
without a human pasting errors.

## The failure dossier
Config emits forensics on failure only:
```ts
use: { trace: 'retain-on-failure', screenshot: 'only-on-failure', video: 'retain-on-failure' },
reporter: [['html', { open: 'never' }], ['json', { outputFile: 'playwright-report/report.json' }], ['list']],
```
A summarizer script reads `report.json` (walk `suites -> specs -> tests -> results`, keep
`status === 'failed' || 'timedOut'`), pulls `error.message` + attachments, and writes `dossier.md` with a
per-failure section and an exact repro line: `npx playwright test <file> -g "<title>"`. Forward the browser
voice into test output via a fixture:
```ts
page.on('console',  m => (m.type() === 'error' || m.type() === 'warning') && console.error(`[browser ${m.type()}] ${m.text()}`));
page.on('pageerror', e => console.error(`[browser pageerror] ${e.message}`));
page.on('response',  r => r.status() >= 400 && console.error(`[network ${r.status()}] ${r.request().method()} ${r.url()}`));
```
Agent rule: on failure, generate + read the dossier, reproduce in isolation, **never** edit the assertion
to pass, **never** add `console.log` to tests (read the trace instead). `report.json` field names shift
between versions -- validate them.

## Flaky triage: classify, never bump `retries`
Run it 10x first: `for i in {1..10}; do npm test -- --grep "name" || true; done`.
- 0/10 = CI-only (timing or worker isolation) - 1-3/10 = real flake, classify - 4+/10 = a broken test, not
  flaky - 10/10 = setup wrong.

Four buckets -> specific fix:
| Symptom | Fix |
|---------|-----|
| Timing race (assert before network) | `waitForResponse` / auto-retrying assertion -- not a bigger timeout |
| Shared-state leak (test N fails after N-1) | fixture with awaited teardown |
| Locator ambiguity (strict-mode violation) | region-scoped role locator |
| Config/auth mismatch (redirect to /login) | fix project `dependencies` + `storageState` |

Quarantine LOUD, never hide: `test.fixme(...)` plus an issue annotation -- it still runs and traces, and
fails the run if it starts passing (`test.skip` hides it). `retries: process.env.CI ? 2 : 0`, for
environmental flakes only; zero locally is non-negotiable. A 40% failure rate is a broken test.

## The trace: densest evidence, read it before editing
Record settings by intent: `on-first-retry` (CI default, needs retries), `retain-on-failure`,
`retain-on-failure-and-retries` (chasing a flake -- compare passing vs failing attempt). Open with
`npx playwright show-trace path/trace.zip` or `show-report`. The timeline is a filter: double-click the
disproportionately-wide action (it was waiting).

Read four panes in order to extract five dossier fields:
1. **Actions/timeline** -- the widest action is the smoking gun.
2. **DOM snapshots** (before/action/after) -- element missing, obscured, or duplicated.
3. **Network** -- request still pending (timing race), a `302 -> /login` or `401` (auth mismatch), or a
   request that never fired (wrong locator).
4. **Console/Source** -- an error that explains it, or an empty console (itself diagnostic: server-side).

Five fields to record: failing step name, DOM snapshot at failure, relevant network (status/url/method/
timing), console errors (or "empty" -- state it explicitly), and the timestamp delta between action and
assertion. Never delete a trace without extracting these five -- that is the contract with the next agent.
The viewer shows evidence; it does not classify the bug for you.
