# 02 - Anchor (Playwright core)

The thin end-to-end slice that proves the harness. A deterministic target + accessibility locators +
web-first waits make the agent's self-written tests survive unrelated edits.

## Locators: accessibility hierarchy only
Preference order -- CSS is a banned anti-pattern:
1. `getByRole('button', { name: 'Add item' })`  2. `getByLabel('Title')`  3. `getByPlaceholder('Search...')`
4. `getByText('Saved')`  5. `getByTestId('add-btn')` (third choice, justify in commit)  6. `locator(css)` -- never.

Scope by chaining role locators, not CSS strings:
```ts
const item = page.getByRole('article', { name: /Report A/ });
await item.getByRole('button', { name: 'Rate' }).click();
```
Compose with `.filter({ has, hasText, hasNot })`, `.and()`, `.or(...).first()` -- not `.nth()` (breaks on
DOM reorder). Add an ESLint rule banning `page.locator` under `tests/`. If a locator needs a testid or
CSS, the component often lacks an accessible name -- that is a real bug, not a test problem.

## Waiting: assertions, never timeouts
Ban `waitForTimeout`. Every wait is an auto-retrying assertion on the real end state:
```ts
await page.getByRole('button', { name: 'Add' }).click();
await expect(page.getByText('Saved')).toBeVisible();     // the assertion IS the wait
```
Retrying family: `toBeVisible/Hidden/Enabled`, `toHaveText/Value/Attribute`, `toHaveCount`, `toBeInViewport`.
Wait on network only when the network is the signal -- set the waiter BEFORE the action, match url+method.
Use `fill()` (not `pressSequentially`), `expect.poll()` for a settling value, `toPass({ timeout })` for a
block (its default timeout is 0 -- always set one). `networkidle` and `isVisible()` (no retry) are
discouraged; flakiness means the assertion targets the wrong state, never "wait longer".

## Config: tiny, options in the right place
Runner options go top-level; context options inside `use`. Misplacing a runner option inside `use` is
silently ignored (no error). Prefer build+preview over the dev server for production-like behavior:
```ts
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: 'http://127.0.0.1:4173', trace: 'on-first-retry', screenshot: 'only-on-failure', video: 'retain-on-failure' },
  webServer: { command: 'npm run build && npm run preview -- --port 4173', url: 'http://127.0.0.1:4173', reuseExistingServer: !process.env.CI },
});
```
Do not hide flaky specs in `testIgnore`. `workers: 1` in CI defeats `fullyParallel` -- leave workers unset.

## Projects: a dependency graph, not lucky timing
Model shared prerequisites (login, seed) as a `setup` project other projects depend on -- not `beforeAll`
or `globalSetup` (which loses fixtures, `page`/`request`, and traces):
```ts
projects: [
  { name: 'setup', testMatch: /auth\.setup\.ts/ },
  { name: 'app', testIgnore: /auth\.setup\.ts/, dependencies: ['setup'], use: { storageState: 'playwright/.auth/user.json' } },
]
```
A flaky setup project silently skips the whole suite ("0 tests ran" -> check setup). Projects override
(not merge) top-level `retries`/`timeout`.

## webServer: a contract, not a fixture
`webServer` only launches a process and checks readiness -- never seed or migrate inside it. Most
"works locally, not CI" failures are webServer misconfig. Use the array form for frontend+API (name each);
`url` readiness accepts 2xx/3xx/400-403; always set `use.baseURL` explicitly; `port` is deprecated -> use
`url`. A reused stale server hides code drift -- kill it before debugging.

## UI Mode & Codegen (authoring aids, not deliverables)
On a failing locator, open `--ui` and Pick Locator from evidence instead of guessing. `codegen` produces a
first-draft skeleton **with no assertions** -- it verifies nothing until you add them and rewrite locators
by hand. A recorded flow committed as-is fossilizes a first draft into maintenance debt; `--save-storage`
output holds real credentials -> gitignore it.
