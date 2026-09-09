# 07 - CI and deploy (the loop of last resort)

CI catches the last 5% plus environment-specific bugs -- not the mistakes earlier loops should catch. If CI
is the *first* place you see a lint/type/format error, that is a bug in an earlier loop.

## CI: strict superset, legible output
Put the strict version of every check in CI; keep fast checks in editor/hooks/probes. Group the cheap
static checks into one job; fan out unit and e2e:
```yaml
# static -> { unit, e2e } in parallel
- run: npm ci --ignore-scripts
- uses: actions/cache@v5
  with:
    path: |
      ~/.npm
      ~/.cache/ms-playwright
    key: ${{ runner.os }}-deps-${{ hashFiles('package-lock.json') }}
```
Use `fail-fast: false` (the agent wants all failures + full artifacts in one pass) and `needs:` only for
real build dependencies. Upload traces/screenshots/dossier on failure so the agent can `download` and
recover. Branch protection (required checks) is the hard gate; everything before is soft. Agent rule: never
add `continue-on-error` or relax strictness to "fix" a failure -- propose that as a separate decision. Only
CI reliably catches cross-platform pixels, the cross-browser matrix, clean-slate env, concurrency at scale,
and time-sensitive checks.

## Post-deploy smoke
A green PR proves mergeable, not that the deployment is healthy. Run a tiny smoke (2-3 assertions:
reachable, core route renders, one primary read + one write, auth wiring) against the deployed URL, read
from env so one file runs anywhere:
```ts
test.use({ baseURL: process.env.SMOKE_BASE_URL ?? 'http://127.0.0.1:3000' });
test('home renders and exposes sign in', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByRole('banner').getByRole('link', { name: 'Sign in' })).toBeVisible();
});
```
Write rollback triggers *before* you need them (smoke fail / critical 500 / auth broken / error spike over
threshold in the first N minutes). "If it looks bad, probably revert" is not a policy.

## Nightly (schedule the slow/broad/drift-sensitive checks)
Cron the expensive guardrails that do not belong on every PR -- cross-browser matrix, dependency audit,
upstream-contract/HAR drift, long perf checks -- and route every failure back into daily work (artifact,
PR, or auto-opened issue), or it is just wallpaper. Cron is UTC and must live on the default branch; run at
an odd minute to dodge the scheduler stampede. **Never** run `--update-snapshots` on a cron -- that silently
erases evidence; open a PR with the diff instead.

## Cross-browser without burning the dev loop
Keep Chromium as the fast default; make alternate engines an opt-in smoke subset (tag `@cross-browser`),
full matrix nightly:
```json
"test": "playwright test --project=chromium",
"test:cross-browser": "playwright test --grep @cross-browser --project=chromium --project=firefox-smoke --project=webkit-smoke"
```
Playwright installs only Chromium by default -- run `npx playwright install --with-deps firefox webkit`
before the first cross-browser run (locally and in CI). Cover login, nav, dialogs, high-value forms, and
layout-fragile routes. Treat browser-specific failures as real, not flaky.

## The whole loop, end to end (capstone order)
The honest test of the loop is autonomy: give the agent one feature and the rules file, and see which loops
fire unaided. The layers compose in this order:

1. Read project rules -> 2. write a failing test first -> 3. implement -> 4. static layer (lint + types,
self-caught via hooks) -> 5. unit tests -> 6. E2E (`getByRole`, no `waitForTimeout`) -> 7. runtime probe /
verify tool -> 8. visual-regression diff -> 9. accessibility check -> 10. performance budget -> 11. open PR
-> 12. CI runs all jobs -> 13. on red, pull the failure dossier and fix -> 14. automated review comments,
agent addresses -> 15. CI green + mergeable -> 16. post-deploy smoke; stop-ship/rollback if it fails.

Every error a human had to paste marks a loop that failed to catch it -- that is the tuning target.

## Porting to another stack
Port responsibilities, not filenames -- the loop structure survives a framework swap. Map seven homes:

| Responsibility | React/Vite/Next home |
|---|---|
| Agent rules | `CLAUDE.md` / `AGENTS.md` |
| Deterministic auth | `storageState` or a seeded login route |
| Deterministic data | seed script / test-DB reset / fixture loader |
| Browser proof | Playwright E2E against the app |
| Static layer | ESLint, `tsc --noEmit`, dead-code + secret scan |
| Failure dossier | traces, screenshots, console logs, CI artifacts |
| Post-deploy | smoke path against preview/staging |

Do not copy commands blindly if the stack has better primitives. The rule stays: cheap loops early, strict
loops later.
