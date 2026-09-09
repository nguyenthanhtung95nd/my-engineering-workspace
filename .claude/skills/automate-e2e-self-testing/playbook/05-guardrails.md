# 05 - Guardrails (verify gate + static layer)

The static layer is the cheapest loop and helps agents disproportionately: a lint error 30s after an edit
is nearly free (context still hot); a test failure 5 min later is expensive. An agent is text-in/text-out,
so every lint/type/dead-code error is a future prompt -- write the messages like fix prompts.

## The one verify gate
Chain the checks into a single script the agent runs before declaring done; keep unit (`.test`) and e2e
(`.spec`) on separate globs so runners never collide:
```json
"scripts": { "verify": "tsc -b && eslint . && vitest run && playwright test" }
```
Each static tool follows the same setup pattern: install -> configure aggressive-but-not-noisy -> add a
named script -> add the script to "what done means" -> run on save/commit/push/CI (same layers, only
strictness changes) -> tune weekly. Steps 3-4 are the most skipped: "we have ESLint" means nothing if the
agent is not told to run it.

## Lint + types as guardrails
ESLint defaults are a floor; the rules that help agents encode *your* recurring mechanical mistakes.
`no-restricted-syntax` is the swiss-army knife (AST selector, no plugin), with a prompt-shaped message:
```js
{ files: ['app/api/**/*.ts'], rules: { 'no-restricted-syntax': ['error', {
  selector: "MemberExpression[object.property.name='body'][property.name='userId']",
  message: 'Read userId from the session, not the request body. Trusting the client on identity is a permission bug.',
}]}}
```
Non-negotiable rules: `@typescript-eslint/no-floating-promises`, `no-misused-promises`,
`strict-boolean-expressions`, `no-console` (allow `error`/`warn`). `tsconfig`: `strict: true` plus
`noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, `noUnusedLocals/Parameters`, `noImplicitReturns`.
In agent rules, ban the escape hatches: no `eslint-disable`, no rule downgrades, no `any`, no
`@ts-expect-error` -- fix the code. Tighten gradually on legacy code (`warn` -> `error` in new-file
overrides), pin `--max-warnings=<n>` in CI, and refuse increases.

## Dead-code detection (a gate, not a chore)
Agents accrete orphans because additive edits are cheap and subtractive edits only touch what they were
told to. Run `knip` on every CI pass so it points the agent at the orphan it just created:
```jsonc
// knip.json
{ "entry": ["src/app/**/{page,layout,route}.tsx", "tests/**/*.{test,spec}.ts", "scripts/*.ts"],
  "project": ["src/**/*.{ts,tsx}", "tests/**/*.ts"] }
```
Three responses per finding: delete / wire up / ignore-with-written-justification. (`ts-prune` is
unmaintained -- use `knip`.) Fix false positives by tightening `entry`, never by a silent mute.

## Secret scanning
Wire a staged scan into pre-commit and a full scan in CI (the net for a bypassed/uninstalled hook). Use a
wrapper that copies the git index to a temp dir and runs `gitleaks dir` there:
```toml
# .gitleaks.toml
[extend]
useDefault = true
[allowlist]
paths = ['''tests/data/.*''']
```
Agent rule: use unambiguously fake placeholders (`your_key_here`) -- values that pattern-match real formats
trip the scanner. If a real secret ever lands, rotate it, scrub history, document the incident.

## Make it hard to cheat the guardrails
A fast agent optimizing for "command succeeded" will find `--no-verify`, `LEFTHOOK=0`, an editable hook
file, or "just weaken the config." Layer the defenses:
- **Instructions:** never use skip flags/env vars; never weaken hook/CI/ruleset config to pass; infra
  changes get the same review standard as app code.
- **Pre-execution denial** (a `PreToolUse` hook) denies the whole class:
  `/(^|\s)(--no-verify|LEFTHOOK=0|LEFTHOOK_EXCLUDE=|HUSKY=0)/`.
- **CI mirror** is the layer that matters most: CI is the authoritative superset from a clean environment.
- **Protect the protection:** CODEOWNERS + required review on `lefthook.yml`, `.github/workflows/**`,
  `.claude/**`, policy files. Stable, obvious check names (`typecheck`, `unit`, `e2e`, `secret-scan`) are
  harder to game than one opaque `verify`. The subtle bypass is weakening a check then technically obeying
  it -- watch for a removed CI job, a renamed required check, or a narrowed file glob.

## Structured CLI output as pipeline glue
When a step needs a judgment (flake vs real bug, root cause), constrain LLM output to a checked-in JSON
schema so scripts can branch on it -- no free-text parsing. Run hermetically (skip project config/hooks/MCP
so output is identical on laptop and CI); put repo context in an appended system-prompt file; feed input on
stdin. Stop at classification -- never auto-apply fixes on model-reported confidence; route those to a
gated fix loop. Not for pre-commit (no latency budget).
