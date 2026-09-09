# 03 - Determinism

Two invariants: every test starts in a known state, and no two tests can see each other's state. A
non-deterministic suite fails intermittently, and the agent "fixes" it by disabling parallelism -- hiding
the leak instead of removing it.

## Auth via storageState (log in once)
Log in once in a `setup` project, save browser state to JSON, reuse it everywhere:
```ts
// e2e/auth.setup.ts
setup('authenticate', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email').fill('alice@example.com');
  await page.getByLabel('Password').fill('password123');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL('/dashboard');
  await page.context().storageState({ path: 'playwright/.auth/user.json' });
});
```
`sessionStorage` is NOT persisted (use `addInitScript`); SDK tokens in IndexedDB need
`storageState({ indexedDB: true })`. Gitignore the `.auth` dir. Stale cookies -> fix the setup, never add
login to a test.

## Third-party auth (OAuth/SAML): test the boundary you own
Do not drive the provider's UI in every test. Split three questions: (1) normal suite uses an app-owned
bootstrap endpoint that mints the **real session shape** (test/staging only, never in prod); (2) capture
browser-storage tokens inside a context; (3) one tiny real-flow smoke that asserts the redirect *starts*,
in an isolated scheduled lane -- not the PR gate.

## Fixtures: object-with-teardown, purposeful only
Test-scope by default; worker-scope only read-only AND expensive. A fixture must answer one of four
questions -- actor identity, starting state, environment, or diagnostics -- otherwise it is a helper.
```ts
export const test = base.extend<{ seeded: void }>({
  seeded: async ({ request }, use) => {
    await resetContent();
    await use();
    await resetContent();   // teardown MUST be awaited, or it races the next test's setup
  },
});
```
Name a fixture for what it *provides* (`seededReader`), not what it does. `addInitScript`/permissions must
run before navigation. Worker-scoping mutable state leaks between tests -- `serial` mode won't fix it.

## Second actor / browser-free setup: APIRequestContext
`storageState` is one setup-time user. For a second actor or fast API setup, build an `APIRequestContext`
and dispose it:
```ts
adminRequest: async ({ playwright }, use) => {
  const ctx = await playwright.request.newContext({ storageState: 'playwright/.auth/admin.json' });
  await use(ctx);
  await ctx.dispose();
},
```
It is for setup/verification only -- never replace the UI-under-test with API calls.

## Seed + isolation
Centralize a seed helper every spec calls. `resetContent` keeps users (deleting them invalidates stored
sessions); only the auth setup calls `seedFreshDatabase`. Isolation strategies: per-test transaction
(fast, needs a shared connection); **per-worker database keyed on `TEST_WORKER_INDEX`** (the SQLite sweet
spot); per-test namespace (heaviest, most portable). `workers: 1` is a temporary brace, not the design.

## Network isolation (make external traffic reproducible)
Set `serviceWorkers: 'block'` whenever relying on routing (a PWA/MSW worker bypasses it). Install every
mock BEFORE `goto`.

- **HAR record/replay** -- freeze whole flows as a committed artifact:
  ```ts
  await page.routeFromHAR('e2e/fixtures/catalog.har', { url: '**/api.example.com/**', update: !!process.env.UPDATE_HARS });
  ```
  Record with `UPDATE_HARS=1`, replay otherwise. Scrub credentials with a JSON-parsing script; prefer a
  throwaway test user (fixtures dirs are often skipped by secret scanners). Never re-record to silence red.
- **Route interception** -- surgical, inline, readable:
  ```ts
  await page.route('**/api/items', r => r.fulfill({ status: 200, body: JSON.stringify({ items: [] }) })); // fake
  await page.route('**/api/items', r => r.abort());                                                        // error
  await page.route('**/*.{png,jpg,webp}', r => r.abort());                                                 // block
  ```
  You cannot override `Cookie` via `continue()` -- use `storageState`. Enabling routing disables the HTTP cache.
- **Browser APIs** -- prefer first-class knobs (`page.clock.install/pauseAt/fastForward`, context
  `geolocation`/`permissions`/`locale`/`timezoneId`/`colorScheme`). When hand-mocking, preserve the
  contract (a value-only mock passes first paint but breaks framework code calling `addEventListener`).

## Hybrid API + UI
Set up state via the `request` fixture (fast, deterministic); act and assert through the browser (the real
subject). `request` inherits `baseURL`, headers, and `storageState` auth. API setup bypasses UI validation
-- keep server-side validation equal to the form, and never short-circuit the action under test.

## Say what a test does when it fails
Wrap each top-level action in `test.step`; tag by intent; annotate to link issues; use `expect.soft` for a
post-action checklist (hard `expect` for preconditions):
```ts
test('user can rate an item', { tag: ['@critical', '@authenticated'] }, async ({ page }) => {
  await test.step('open the list', async () => { await page.goto('/list'); });
  await test.step('submit 4 stars', async () => { /* ... */ });
});
```
Cap nesting at two levels; keep a small canonical tag vocabulary; run subsets with `--grep @critical`.
