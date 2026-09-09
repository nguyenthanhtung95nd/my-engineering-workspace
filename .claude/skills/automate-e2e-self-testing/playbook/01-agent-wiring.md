# 01 - Agent wiring (foundations)

Before any test, wire the agent to checks it can run itself. Anything the agent cannot mechanically
act on does not belong in the instructions file -- vague directives ("write clean code") are
unverifiable and do not survive the next session.

## Define "what done means"
The single highest-value entry in `CLAUDE.md` / `AGENTS.md` is an exact, ordered definition of green:

```markdown
## What "done" means
A task is not done until all exit zero, in order:
1. npm run typecheck
2. npm run lint
3. npm run test
Do not report complete with any step failing.
```

Add mechanically-checkable content rules too (e.g. "no test scaffolding / testids in rendered UI
copy"). Reminders-to-self belong in a checklist, not the prompt.

## The testing pyramid is a loop hierarchy
Tell the agent which loop a change triggers, with concrete example paths in this repo:

| Loop | Runs | Examples |
|------|------|----------|
| Sub-second | every edit | lint, types, unit |
| Few-second | every meaningful edit | integration + one targeted E2E |
| Minute-ish | before "done" | full E2E, visual, cold typecheck |
| CI-only | never local | cross-browser, full baselines, bundle analysis |

Couple change to test: if `routes/foo/page.tsx` changed, `tests/foo.spec.ts` must exist. Anti-patterns:
overreach (E2E for a pure function), underreach (mock a form, never render it -> miss broken loading states).

## Pick the steering layer by cost of bypass
1. Can lint / types / tests enforce it? Do that -- cheapest, catches humans and agents equally.
2. Must it fire every time? A **hook** (deterministic; the agent cannot skim or forget it).
3. Recurring multi-step workflow? A **skill**.
4. Otherwise a **rule** in the instructions file, revisited periodically.

Rules compound negatively with length -- each extra line lowers the reliability of the rest.

## Fast git hooks (one hook-manager config)
Wire staged-only, fast checks; scope by speed budget (pre-commit < 10s, pre-push < 2 min, slower -> CI):

```yaml
# lefthook.yml
pre-commit:
  parallel: true
  commands:
    lint:    { glob: '*.{ts,tsx}', run: npx eslint --fix --max-warnings=0 {staged_files}, stage_fixed: true }
    format:  { glob: '*.{ts,tsx,json,md,yml}', run: npx prettier --write {staged_files}, stage_fixed: true }
    secrets: { run: npx tsx scripts/run-gitleaks-staged.ts }
pre-push:
  commands:
    checks: { run: npm run pre-push }   # typecheck, dead-code, unit
```

## Runtime policy hooks (when content matters, not just a path)
Register in `.claude/settings.json` with a matcher (`Edit|Write`, `Bash`) and a script that reads the
JSON payload from stdin; to deny, print a `PreToolUse` deny decision and exit 0. Use `$CLAUDE_PROJECT_DIR`.
Uses: block edits to lint/CI config, block editing generated files, auto-attach a failure report after a
test run. Keep hooks fast (no network); `Bash` is a separate path from `Edit`/`Write`; a `cat .env`
subprocess bypasses path-scoped `Read` denies unless sandboxed.

> A guardrail the agent can dodge is a suggestion. See [05-guardrails.md](05-guardrails.md) for making
> checks hard to bypass (deny `--no-verify`/`LEFTHOOK=0`, protect config, mirror in CI).
