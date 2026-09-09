---
name: automate-e2e-self-testing
description: Sets up or audits a self-testing E2E feedback loop for any frontend project (Playwright + local guardrails) so the agent verifies its own UI work before human review. Use when adding E2E/UI tests, hardening a frontend verify pipeline, wiring accessibility-anchored Playwright tests, or auditing an existing test setup against best practices.
---

# Automate E2E Self-Testing

Make UI verification **cheap and automatic**. The constraint on agent-driven frontend work is not
model intelligence -- it is a human having to be the feedback loop. Every layer here moves a check
earlier and turns a soft prompt ("don't use fixed waits") into a hard gate (a lint rule that fails).
A failing check keeps enforcing until deleted; a prompt decays the next session.

## Step 0 -- Detect, then pick a mode
Inspect first: framework (Vite / Next / other), package manager, existing `e2e/` + Playwright config,
`test`/`verify` scripts, git hooks, and whether a deterministic build exists (mock/preview).

- **Bootstrap** (no E2E) -> install the SPINE end to end.
- **Audit** (E2E exists) -> score each layer with [CHECKLIST.md](CHECKLIST.md), report gaps, apply what is missing.

Always show the plan and get confirmation before writing files or installing anything.

## The loop, in order
Build the SPINE first (each layer green before the next); add ADD-ONS on request.

**SPINE**
0. **Agent wiring** -- define "what done means" (exact ordered commands), pick rules/skills/hooks per cost of bypass, wire fast git hooks. See [playbook/01-agent-wiring.md](playbook/01-agent-wiring.md).
1. **Anchor** -- Playwright against a deterministic target; accessibility-only locators; web-first waits; `webServer`; Chromium first. See [playbook/02-anchor.md](playbook/02-anchor.md).
2. **Determinism** -- `storageState` auth, purposeful fixtures, seeded/isolated state, network isolation (route/HAR/browser-API mocks), API+UI hybrid. See [playbook/03-determinism.md](playbook/03-determinism.md).
3. **Evidence** -- trace/screenshot/video + a readable failure dossier; triage flakes by cause, never by `retries`. See [playbook/04-evidence.md](playbook/04-evidence.md).
4. **Guardrails** -- one `verify` gate (typecheck -> lint -> unit -> e2e); static layer (custom lint, dead-code, secret scan); make it hard to bypass. See [playbook/05-guardrails.md](playbook/05-guardrails.md).

**ADD-ONS** (opt-in -- offer, install only on request) -- visual regression, accessibility gate, performance budgets, automated review, runtime probes/MCP, AI test agents. See [playbook/06-add-ons.md](playbook/06-add-ons.md).

**LAST RESORT** -- CI, post-deploy smoke, nightly, cross-browser, plus the end-to-end capstone order and how to port to another stack. See [playbook/07-ci-and-deploy.md](playbook/07-ci-and-deploy.md).

## Tooling policy (enterprise-safe)
- **Local-first (recommend freely):** Playwright, ESLint, tsc, knip, Gitleaks, a hook manager, axe -- run on the machine, nothing leaves it.
- **GATED -- needs org/IT approval, NEVER auto-install:** any external SaaS review bot, MCP servers (runtime-probe / AI test agents), cloud review or cross-browser services. Flag them, prefer an approved or local-first alternative, and do not stand up shadow IT.

## Output
Either config + fixtures + at least one **passing** spec + a `verify` gate + a hook; or an audit
report with prioritized gaps. Finish by stating what changed and the exact command to run.

Templates to clone: [templates/playwright.config.ts](templates/playwright.config.ts), [templates/fixtures.ts](templates/fixtures.ts).
