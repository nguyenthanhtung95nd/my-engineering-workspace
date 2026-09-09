# Audit scorecard

For an existing project, score each item **present / partial / missing**, note a one-line gap and a
concrete fix, then rank fixes: spine gaps first, then requested add-ons. Details per layer live in
[playbook/](playbook/).

## Spine

### Agent wiring ([01](playbook/01-agent-wiring.md))
- [ ] Instructions file defines "what done means" with exact ordered commands
- [ ] Change->test coupling stated (e.g. touched route requires its spec)
- [ ] Fast git hooks wired (pre-commit < 10s, pre-push < 2 min)
- [ ] Steering layer chosen by cost of bypass (lint/test > hook > skill > rule)

### Anchor ([02](playbook/02-anchor.md))
- [ ] Playwright installed + Chromium; config tiny, options in the right place
- [ ] Locators are accessibility-only (no CSS / testid); `page.locator` banned in tests via lint
- [ ] No `waitForTimeout`; waits are auto-retrying assertions
- [ ] `webServer` owns the deterministic target; `baseURL` set explicitly
- [ ] Shared prerequisites modeled as a `setup` project with `dependencies`

### Determinism ([03](playbook/03-determinism.md))
- [ ] Auth via `storageState` (log in once); `.auth` dir gitignored
- [ ] Fixtures are purposeful (actor / state / environment / diagnostics) with awaited teardown
- [ ] A central seed helper; isolation strategy the DB supports (not `workers: 1`)
- [ ] Network isolated (route/HAR/browser-API mocks); `serviceWorkers: 'block'` when routing
- [ ] State set up via API, behavior asserted via UI (hybrid)

### Evidence ([04](playbook/04-evidence.md))
- [ ] Trace/screenshot/video on failure + HTML + JSON reporters
- [ ] A failure dossier (repro command + console/network forwarded)
- [ ] Flakes triaged by cause, quarantined loudly; `retries` not used to mask bugs

### Guardrails ([05](playbook/05-guardrails.md))
- [ ] One `verify` gate: typecheck -> lint -> unit -> e2e; unit/e2e on separate globs
- [ ] Custom lint rules for recurring mistakes; strict TS extras; escape hatches banned in rules
- [ ] Dead-code gate (knip) and secret scan wired
- [ ] Hard to bypass: skip flags denied, config protected (CODEOWNERS), CI mirrors the hooks

## Add-ons (opt-in) ([06](playbook/06-add-ons.md))
- [ ] Visual regression baselines committed and stabilized
- [ ] Accessibility gate (axe) blocking on violations
- [ ] Performance budgets (bundle size + one runtime number) in VCS
- [ ] Automated review (GATED) tuned + vendor-neutral playbook
- [ ] Runtime probes / custom MCP (GATED)
- [ ] AI test agents (GATED)

## Last resort ([07](playbook/07-ci-and-deploy.md))
- [ ] CI runs the strict superset; artifacts uploaded on failure; branch protection is the hard gate
- [ ] Post-deploy smoke against the deployed URL with pre-written rollback triggers
- [ ] Nightly for slow/broad/drift checks, each failure routed back to work
- [ ] Cross-browser as an opt-in smoke subset, full matrix nightly

> Gating reminder: anything marked GATED (external SaaS review, MCP servers, cloud services) needs org/IT
> approval -- flag it, never auto-install, prefer a local-first or approved alternative.
