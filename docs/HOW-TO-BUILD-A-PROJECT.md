# How to Build a Project - Zero to Production

A hands-on, **language-agnostic** walkthrough for taking a brand-new project from an
empty folder to a shipped, production-ready release using this workspace.

**The pipeline is the constant; the stack is what you plug in.** The same flow -
plan → scope → scaffold → (build → run → test → refine) → integrate → harden → ship -
works whether you build in .NET, Node/TypeScript, Go, Python, or Rust. Everything
stack-specific lives behind the setup table below and in
[README - Adding a New Stack](../README.md#11-adding-a-new-stack).

Running example throughout: **a small Task API** (create / list / complete tasks).
Described at the behaviour level so you can follow it in any language.

---

## Principles that carry the whole build

These are the durable habits. Internalize them; the phases below just apply them.

1. **Plan before you code.** Use Plan Mode (read-only, `Shift+Tab`) for architecture
   talk so the agent reasons before it writes. → [`grill-me`](../.claude/skills/grill-me/SKILL.md)
2. **Constraints beat volume.** State what must change, what must not, and - crucially -
   **what NOT to build yet**. → Project Boundaries + [`safe-modification`](../.claude/rules/safe-modification.md)
3. **Controlled first pass.** Narrow v1 aggressively (no DB/auth/cache/queues unless
   the ticket needs them). Get to a running baseline, then grow.
4. **Iterate, don't one-shot.** generate → run → **test** → refine. Early errors
   (missing deps, env assumptions) are normal, not failure.
5. **Wrap third-party behind an interface.** SDKs (AI providers, payment, storage)
   never called directly from business logic. → [`design-principles`](../.claude/rules/design-principles.md)
6. **Give the agent concrete environment facts** (versions, ports, hosts, model names)
   instead of abstract requirements. → CLAUDE.md `Environment Facts`
7. **Debug systematically:** reproduce → isolate a minimal case → one change at a time →
   fix the root cause, not the symptom. → [`diagnose`](../.claude/skills/diagnose/SKILL.md)
8. **You stay the pilot.** You own goals, architecture, taste, and persistence. The
   agent accelerates; it does not decide.

---

## Per-stack setup (multi-language)

`do-work` auto-detects the stack from a root indicator file. For a new stack not yet
in the workspace, first follow [README §11](../README.md#11-adding-a-new-stack) to add
its rules/context/patterns, then return here.

| Indicator file | Stack | Build / typecheck | Test | Patterns skill |
|----------------|-------|-------------------|------|----------------|
| `*.csproj` / `*.sln` | .NET | `dotnet build` | `dotnet test` | `dotnet-patterns` |
| `package.json` | Node / TS | `npm run build` / `tsc --noEmit` | `npm test` | `{stack}-patterns` |
| `go.mod` | Go | `go build ./...` | `go test ./...` | add per §11 |
| `pyproject.toml` | Python | - | `pytest` | add per §11 |
| `Cargo.toml` | Rust | `cargo build` | `cargo test` | add per §11 |

Where a phase below says "your build command" or "your test command", use this table.

---

## Phase 0 - Environment ready

Confirm the runtime and Claude Code are installed, then open the project **folder** in
your editor so the agent gets correct context from the start.

- Install your stack's runtime/SDK and verify its version.
- Launch Claude Code from inside the project directory.
- Know your permission modes: **default** (asks before edits), **acceptEdits** (fewer
  interruptions once you trust the direction), **plan** (read-only). Cycle with `Shift+Tab`.

## Phase 1 - Point the workspace at the project

1. Copy the `.claude/` folder into the new project root (see [README §10](../README.md#10-setup-for-a-new-or-existing-project)).
2. Fill in `CLAUDE.md` → **Project Context**, and especially **Project Boundaries**
   (`In-scope` / `Out-of-scope`) - this is the single strongest guard against the agent
   over-building.
3. Run `/onboard` to auto-populate **Environment Facts** (SDK, OS/shell, DB, container
   runtime). For a greenfield repo there's little code to map yet; the value is the
   environment table and the boundaries.
4. If this is a non-.NET stack, add its layer first via [README §11](../README.md#11-adding-a-new-stack).

## Phase 2 - Plan first (design before code)

Enter **Plan Mode** and run [`grill-me`](../.claude/skills/grill-me/SKILL.md) (or
[`grill-with-docs`](../.claude/skills/grill-with-docs/SKILL.md) if the domain has new
terminology). Resolve, one question at a time: actors, core entities, the flow, and the
tradeoffs - **and lock what v1 will NOT do**.

Then capture it: [`write-a-prd`](../.claude/skills/write-a-prd/SKILL.md) →
`prd/{feature}-prd.md` (problem, user stories, acceptance criteria, **out-of-scope**).

> Task API v1 out-of-scope example: no auth, no multi-user, no due-date reminders, no
> UI - just a REST API with an in-memory or single-table store.

## Phase 3 - Slice into buildable phases

[`prd-to-plan`](../.claude/skills/prd-to-plan/SKILL.md) → `plans/{feature}-plan.md`.
Each phase is a **vertical tracer bullet** cutting end-to-end (route + logic + test),
not a horizontal layer.

For a non-trivial build, run [`scratchpad`](../.claude/skills/scratchpad/SKILL.md) first:
map the system, list edge cases, note risks, draft ADRs. It survives `/clear` and
interrupted sessions and becomes your recovery anchor.

## Phase 4 - Controlled first pass (scaffold)

Build **phase 1 only** - the narrowest slice that runs. Prefer a skeleton/tracer bullet
over a full feature. If scaffolding from scratch, structure the generation request the
way the book does - a blueprint, not a wish:

```
goal → project overview → stack + key dependencies → folder structure →
security expectations → error-handling expectations → what NOT to include (v1)
```

Then run it immediately and fix the first errors (missing deps, config/env) as they
appear. A narrower first prompt yields a more stable scaffold.

## Phase 5 - The build loop (per phase)

Drive each plan phase through [`do-work`](../.claude/skills/do-work/SKILL.md):

```
map → implement (this phase only) → your build command → your test command → fix → repeat
```

Run and test after **every** meaningful change so a failure maps to one change. When a
bug is hard or flaky, escalate to [`diagnose`](../.claude/skills/diagnose/SKILL.md).
Do not let the agent implement a large multi-part feature in one unreviewed pass.

## Phase 6 - Add features iteratively

Layer features one at a time. Two book lessons matter most here:

- **Wrap external services behind an interface** (an AI provider, a queue, a payment
  SDK). Business logic depends on the abstraction, so you can swap OpenAI ↔ a local
  model, or SQS ↔ an in-memory stub, without touching core code.
- **Feed concrete environment facts** into the request (host:port, model/version,
  region) rather than letting the agent guess - the same facts recorded in CLAUDE.md.

## Phase 7 - Handle regressions & polish

Real product rhythm: a new feature lands and exposes a weakness elsewhere (a layout
breaks when content grows; a query slows under real data). When you change existing
code, apply [`safe-modification`](../.claude/rules/safe-modification.md): map first,
keep the change localized, and **list the behaviours that must stay unchanged** and how
you preserved them.

## Phase 8 - Harden & ship to production

This is where the workspace goes **beyond** the beginner tutorial. Before every PR:

- [`ship-feature`](../.claude/skills/ship-feature/SKILL.md) → runs `/code-review` →
  `/security-review` → `/test-coverage` → `/pr-summary`. Stops at Critical findings.
- `/migrate` before applying any schema change · `/compliance` if it handles personal data.
- `/devops` for Dockerfile / CI-CD · `/perf` on any changed hot path.
- After deploy: watch the first 30 minutes; `/incident` immediately if error rate spikes
  (triage before debugging).

See [README - Full Agile Team Workflow](../README.md#7-full-agile-team-workflow) for
the sprint-level view of pre-release gates and deployment.
