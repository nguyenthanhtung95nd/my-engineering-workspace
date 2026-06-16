# Claude Code — Master Workspace

This workspace was built through hands-on engineering experience across real production
projects and refined over time into a structured AI-native development system.
It is designed to serve as a single workspace for both ASP.NET Core Web API and
AWS Lambda (.NET 8) projects, covering the full lifecycle from requirement to deployment.

---

## Table of Contents

1. [What is this workspace?](#1-what-is-this-workspace)
2. [Full directory structure](#2-full-directory-structure)
3. [How it works — 4 layers](#3-how-it-works--4-layers)
4. [Feature development pipeline](#4-feature-development-pipeline)
5. [Five engineering roles](#5-five-engineering-roles)
6. [Real-world walkthrough](#6-real-world-walkthrough)
7. [Full Agile team workflow](#7-full-agile-team-workflow)
8. [Daily workflow with Scrum](#8-daily-workflow-with-scrum)
9. [Quick reference](#9-quick-reference)
10. [Setup for a new project](#10-setup-for-a-new-project)
11. [Adding a new stack](#11-adding-a-new-stack)

---

## 1. What is this workspace?

This is a **master AI engineering workspace** that consolidates everything needed
to work on both ASP.NET Core Web API and AWS Lambda (.NET 8) projects.

It combines:
- A **full feature development pipeline** — from vague idea to merged PR
- **Always-on engineering rules** loaded automatically every session
- **Specialized subagents** that self-delegate for debugging, review, and docs lookup
- **19 manual commands** for deep, targeted work
- **Context and templates** for both stacks

### How this differs from generic AI chat

| Generic AI chat | This workspace |
|----------------|----------------|
| Re-explain your stack every session | `CLAUDE.md` + `rules/` load context automatically |
| Generic C# output | Output follows your conventions — `Result<T>`, primary constructors, NSubstitute |
| You must remember to ask for security review | `code-reviewer` agent runs automatically after code changes |
| One prompt → one answer | Full pipeline: interview → spec → plan → implement → verify → deploy |
| Training data for library APIs | `docs-explorer` fetches live docs via Context7 MCP |

---

## 2. Full Directory Structure

```
claude-mastery/
├── README.md                            ← You are here
├── docs/
│   └── roles/
│       ├── sa-solution-architect.md     ← SA role: workflow + real example
│       ├── ba-business-analyst.md       ← BA role: workflow + real example
│       ├── dev-developer.md             ← DEV role: workflow + real example
│       ├── qa-tester.md                 ← QA role: workflow + real example
│       └── devops-engineer.md           ← DevOps role: workflow + real example
│
└── .claude/
    ├── CLAUDE.md                        ← Session context, loads rules + context via @
    ├── settings.json                    ← Permissions: allow dotnet/git, deny push/rm -rf
    │
    ├── rules/                           ← Always-on standards, loaded every session
    │   ├── naming.md                    ← PascalCase, _camelCase, Async suffix
    │   ├── structure.md                 ← SRP, method < 20 lines, guard clauses
    │   ├── methods-and-classes.md       ← Max 3 params, no bool args, DI lifetimes
    │   ├── async.md                     ← No .Result/.Wait(), CancellationToken end-to-end
    │   ├── error-handling.md            ← Result<T> (Web API), Lambda batch failures
    │   ├── design-principles.md         ← SOLID, YAGNI, wrap third-party libs
    │   ├── comments.md                  ← Why not what, XML doc on public APIs
    │   ├── testing.md                   ← Testing pyramid, DAMP over DRY, no DB mocks
    │   └── checklist.md                 ← Pre-PR checklist
    │
    ├── context/                         ← Reference docs loaded via @ in CLAUDE.md
    │   ├── architecture.md              ← Layer flows for both stacks
    │   ├── templates.md                 ← Copy-paste boilerplate for both stacks
    │   └── testing.md                   ← xUnit + NSubstitute / Moq patterns
    │
    ├── skills/                          ← Pipeline + auto-invoked workflows
    │   ├── grill-me/                    ← Interview: one question at a time
    │   ├── grill-with-docs/             ← Grill + shared language (CONTEXT.md) + ADRs
    │   ├── diagnose/                    ← Hard/flaky bugs: feedback loop → hypothesise → fix
    │   ├── improve-codebase-architecture/ ← Deepening opportunities → HTML report
    │   ├── write-a-prd/                 ← Structured PRD → prd/{feature}-prd.md
    │   ├── prd-to-plan/                 ← Vertical slices → plans/{feature}-plan.md
    │   ├── do-work/                     ← Implement + build/test loop + Work Summary
    │   ├── write-a-skill/               ← Create new skills for this workspace
    │   ├── dotnet-patterns/             ← Auto-loads on .cs/.csproj files
    │   ├── security-audit/              ← Auto-loads on auth/payment/data files
    │   └── architecture-decision/       ← Auto-loads on design discussions
    │
    ├── agents/                          ← Specialized subagents, self-delegate
    │   ├── debugger.md                  ← Root cause analysis + build/test loop
    │   ├── code-reviewer.md             ← Critical/Warning/Suggestion with file:line refs
    │   ├── docs-explorer.md             ← Live docs via Context7 MCP, never training data
    │   ├── security-reviewer.md         ← OWASP expert, attacker mindset
    │   ├── test-generator.md            ← xUnit coverage: happy/validation/error paths
    │   ├── code-explainer.md            ← Legacy code understanding
    │   └── performance-analyzer.md      ← N+1, slow queries, memory issues
    │
    └── commands/                        ← 19 manual triggers
        ├── code-review.md               ← /code-review [BUGS|SECURITY|PERFORMANCE]
        ├── review.md                    ← /review
        ├── security-review.md           ← /security-review
        ├── generate.md                  ← /generate
        ├── debug.md                     ← /debug
        ├── refactor.md                  ← /refactor
        ├── test.md                      ← /test
        ├── migrate.md                   ← /migrate
        ├── incident.md                  ← /incident
        ├── perf.md                      ← /perf
        ├── compliance.md                ← /compliance
        ├── onboard.md                   ← /onboard
        ├── pipeline.md                  ← /pipeline
        ├── agent.md                     ← /agent
        ├── devops.md                    ← /devops
        ├── architect.md                 ← /architect
        ├── saas.md                      ← /saas
        ├── workflow.md                  ← /workflow
        └── team-standards.md            ← /team-standards
```

---

## 3. How It Works — 4 Layers

```
Layer 1 — Always On
  rules/ loaded via @ in CLAUDE.md every session.
  Naming, structure, async, error handling, and design principles
  apply automatically. You never need to say "follow .NET conventions."

Layer 2 — Context Aware
  skills/ auto-load by file type or topic.
  Open a Controller → security-audit skill loads.
  Ask about caching → architecture-decision skill loads.
  Write any .cs code → dotnet-patterns skill loads.

Layer 3 — Self-Delegating
  agents/ trigger automatically by situation.
  Any error or failing test    → debugger invoked.
  Code written or modified     → code-reviewer invoked.
  Library or SDK question      → docs-explorer fetches live docs.

Layer 4 — Manual Power
  commands/ for deep, targeted work you explicitly request.
  /code-review BUGS,SECURITY   → before every PR.
  /incident                    → when production is down.
  /architect                   → for major design decisions.
```

---

## 4. Feature Development Pipeline

Every feature moves through this pipeline.
**Enter at whichever stage you already have.**

```
Vague idea
    ↓
grill-me           Interview: one question at a time.
                   Claude gives a recommended answer first.
                   Explores the codebase instead of asking what is already there.
grill-with-docs    Same as grill-me + builds shared language (CONTEXT.md) + records ADRs.
                   Use when the project needs consistent domain terminology.
    ↓
write-a-prd    Structured PRD saved to prd/{feature}-prd.md.
               Problem statement, user stories, acceptance criteria, out of scope.
    ↓
prd-to-plan    Vertical slices saved to plans/{feature}-plan.md.
               Each phase cuts end-to-end: schema + logic + tests.
    ↓
do-work        Implement phase by phase.
               Loop: implement → dotnet build → dotnet test → fix → repeat.
               Ends with a structured Work Summary.
    ↓
/code-review   Run /code-review BUGS,SECURITY before raising the PR.
```

### When to enter at each stage

| You have | Start here |
|----------|------------|
| A vague idea | `grill-me` |
| A clear problem statement | `write-a-prd` |
| A finished PRD | `prd-to-plan` |
| A plan or a small clear task | `do-work` |
| Finished code, need review | `/code-review BUGS,SECURITY` |

### Quick tasks — no pipeline needed

For small bugs or clear single-file changes, skip all pipeline stages.
Just describe the task — Claude treats it as `do-work` without a plan file.

```
"Fix the null reference in ProductService when description is missing."
"Add a LastSyncedAt column to the sync_status table."
"Refactor ProcessBatchAsync — it is over 40 lines."
```

### Solo developer — playing all roles (SA + BA + DEV + QA)

When you own the full ticket end-to-end, `grill-me` and `grill-with-docs` run
in sequence — not as alternatives. Each covers a different hat.

**New feature or complex ticket:**

```
New ticket
    ↓
grill-me           Put on the BA + SA hat.
                   Clarify scope, constraints, actors, edge cases.
                   One question at a time until nothing is ambiguous.
    ↓
grill-with-docs    Lock down domain language → update CONTEXT.md.
                   Record architectural decisions as ADRs.
                   Skip if the ticket introduces no new domain concepts.
    ↓
write-a-prd        Document requirements before writing any code.
    ↓
prd-to-plan        Slice into vertical phases. Put on the DEV hat.
    ↓
do-work            Implement phase by phase — build/test loop.
    ↓
/code-review BUGS,SECURITY → Final gate before PR. Put on the QA hat.
```

**Small ticket — implementation on known ground:**

```
grill-me   → Quick alignment, no file creation
    ↓
do-work    → Implement + regression test
    ↓
/code-review BUGS,SECURITY → Before PR
```

**The rule:** if the ticket introduces a concept not yet in `CONTEXT.md`, always
run `grill-with-docs`. If it is purely implementation on known ground, `grill-me`
alone is enough.

---

## 5. Five Engineering Roles

Each role has its own dedicated guide with workflow steps,
primary tools, and a complete real-world example.

| Role | Guide | Responsibility |
|------|-------|---------------|
| **SA** | [sa-solution-architect.md](docs/roles/sa-solution-architect.md) | System design, ADRs, technology decisions |
| **BA** | [ba-business-analyst.md](docs/roles/ba-business-analyst.md) | Requirements, user stories, acceptance criteria |
| **DEV** | [dev-developer.md](docs/roles/dev-developer.md) | Feature implementation, bug fixes, PRs |
| **QA** | [qa-tester.md](docs/roles/qa-tester.md) | Test planning, automated tests, security scenarios |
| **DevOps** | [devops-engineer.md](docs/roles/devops-engineer.md) | CI/CD, infrastructure, incident response |

Switch roles depending on the work at hand.
You can be SA in the morning and DEV in the afternoon — the workspace adapts.

---

## 6. Real-World Walkthrough

**Project:** Product CRUD Lambda triggered by API Gateway, MySQL database,
source code on GitLab, deployed via AWS CDK.

This single project illustrates every role in sequence.

```
SA     → grill-me: HTTP API vs REST API, RDS Proxy, CDK stack design
         /architect: trade-off table, recommendation, ADR-001 committed
         improve-codebase-architecture: deepening review after implementation
         See: docs/roles/sa-solution-architect.md

BA     → grill-me: What defines a product? Roles? Soft delete?
         write-a-prd: user stories, AC, out of scope
         See: docs/roles/ba-business-analyst.md

DEV    → prd-to-plan: 6 phases (skeleton → repository → service → routing → auth → tests)
         do-work: implement phase by phase, build/test loop each phase
         /migrate: schema safety review before applying to RDS
         /code-review BUGS,SECURITY: Critical — GET by ID missing is_active filter
         diagnose: for hard or flaky bugs that cannot be reproduced with /debug alone
         See: docs/roles/dev-developer.md

QA     → /test: xUnit suite — happy path, validation, soft delete, pagination
         Integration tests: Testcontainers MySQL, soft delete exclusion verified
         /security-review: IDOR and Viewer role enforcement test scenarios
         /perf: EXPLAIN confirms index seek on list query
         See: docs/roles/qa-tester.md

DevOps → GitLab CI: build → test → package → deploy-staging → deploy-production (manual)
         CDK deploy: ProductApiStack with OIDC role assumption, no stored credentials
         /incident: RDS Proxy connection limit incident — P1, resolved in 17 minutes
         Postmortem: CloudWatch alarm added, runbook updated
         See: docs/roles/devops-engineer.md
```

Each role guide shows the exact commands, outputs, and code produced at that stage.

---

## 7. Full Agile Team Workflow

This section describes the ideal workflow when all five roles collaborate across
a full Agile sprint. Each ceremony and work phase maps directly to the workspace tools.

The example project used throughout: **Product CRUD Lambda — API Gateway, MySQL, GitLab, CDK.**

---

### Sprint 0 — Foundation (before the first sprint)

This happens once per project. It establishes the technical and requirements foundation
that all subsequent sprints build on.

```
SA   → grill-me on the full system
       → /architect: choose stack, cloud services, CDK structure, auth approach
       → ADR-001 committed: HTTP API vs REST API decision documented

BA   → grill-me on business requirements with stakeholders
       → write-a-prd: top-level product spec, actor definitions, out-of-scope list

DevOps → Set up GitLab repository, branch strategy, CI/CD skeleton
         → CDK bootstrap: create staging and production environments
         → /compliance: verify security requirements before any code is written

Output:
  ADR-001 (architecture decision)
  product-prd.md (top-level requirements)
  GitLab repo with CI/CD pipeline skeleton
  CDK staging + production stacks provisioned
```

---

### Sprint Planning — Day 1 of each sprint

The goal is to leave planning with stories that have zero technical ambiguity.

```
BA   → Present user stories from the backlog
       Each story should already have draft acceptance criteria

SA   → For each story involving a design decision:
       /architect or grill-me to resolve approach before estimation
       Flag stories that need an ADR before implementation begins

DEV  → For each story:
       grill-me → identify technical ambiguities
       If story cannot be clarified → move to next sprint or spike first

QA   → For each story:
       Review AC for testability
       Flag missing edge cases or unclear error scenarios

DevOps → Flag any story that requires infrastructure changes
         (new AWS service, schema migration, new environment variable)
         These need /migrate or CDK review before the sprint ends

Output:
  Sprint backlog with stories that have:
  - Clear AC testable by QA
  - No open technical questions
  - Infrastructure changes identified upfront
  - Realistic estimates based on actual clarity
```

---

### During Sprint — Day-by-day execution

Each story moves through the same flow regardless of complexity.

```
Day N — Story starts

  BA   → write-a-prd for the story (if not already done in planning)
         Saved to prd/{story-name}-prd.md

  SA   → If design decision required: /architect → ADR
         If no decision needed: SA reviews BA PRD for technical correctness

  DEV  → prd-to-plan → plans/{story-name}-plan.md
         Vertical phases, each cutting end-to-end
         do-work phase by phase:
           implement → dotnet build → dotnet test → fix → Work Summary
         /migrate before applying any schema change to shared DB

  QA   → While DEV is implementing:
         /test on each completed class → build test suite in parallel
         /security-review on auth/data endpoints
         Raise issues early — not after the full story is "done"

  DevOps → Monitor CI pipeline on each push
           If new infra needed: CDK stack update in staging first
           /compliance check before any new endpoint goes to staging

Day N+1 or N+2 — Story ready for review

  DEV  → /code-review BUGS,SECURITY → fix all Critical issues → raise PR
  QA   → Review PR: run tests locally, verify AC point by point
  SA   → Architecture review on PR if story touched core design
  DevOps → Verify CI passes: build + unit test + integration test all green
           Confirm staging deployment succeeded
           Verify CloudWatch shows no anomalies post-deploy
```

---

### Pre-Release — Last 2 days of sprint

Before promoting to production, every role has a gate to pass.

```
QA   → Full regression on staging:
       dotnet test (all suites)
       /security-review on all changed endpoints
       /perf on any endpoint that changed a DB query

DEV  → All PRs merged to main
       No open Critical findings from /code-review
       Work Summary written for every story

SA   → Architecture review: do merged changes match ADRs?
       Flag any drift from agreed design

DevOps → /migrate: review all schema changes applied this sprint
         CDK diff: confirm production stack change is expected
         Pre-deployment checklist:
           □ All tests pass in CI
           □ Staging deployment healthy for 24h
           □ Rollback plan documented
           □ On-call engineer aware of deployment window

Output:
  Go / No-go decision for production deployment
  Rollback plan documented before deploy begins
```

---

### Production Deployment

```
DevOps → GitLab CI: trigger manual deploy-production stage
         Monitor CloudWatch during and after deploy:
           Error rate, Lambda duration, RDS Proxy connections
         Keep staging available for 1h post-deploy as rollback target

All roles → Watch for alerts in the first 30 minutes
            If error rate spikes: /incident immediately (do not debug first)
```

---

### Sprint Retrospective — Last day of sprint

```
All roles → Review what slowed the sprint down:
            - Stories that had ambiguity not caught in planning?
              → Improve grill-me prompts
            - PRs that had Critical issues from /code-review?
              → Add rule to rules/ or update checklist.md
            - Incidents in staging that reached production?
              → Update /incident triage patterns
            - Tests that were written too late?
              → Reinforce: QA starts /test while DEV is mid-implementation

DevOps → Update runbook if any new failure mode was discovered
SA     → Update ADR if any architectural decision was revised
DEV    → Update CLAUDE.md Project Context if new patterns emerged
```

---

### Ideal Sprint Summary

```
Sprint Planning    SA + BA + DEV + QA + DevOps   Zero ambiguity before estimation
Story start        BA → SA → DEV                 PRD → plan → implement
Parallel           QA + DevOps                   Tests + infra in parallel with DEV
Story done         DEV → QA → SA → DevOps        PR → review → staging → verify
Pre-release        All roles                      Regression + compliance + CDK diff
Deploy             DevOps                         Manual gate → monitor → rollback ready
Retro              All roles                      Improve workspace based on friction
```

The key principle: **no role waits for the previous role to fully finish.**
BA writes the PRD while SA is finishing the ADR.
QA starts writing tests while DEV is mid-implementation.
DevOps prepares the CDK diff while QA runs regression.
Parallelism is what makes the sprint velocity sustainable.

---

## 8. Daily Workflow with Scrum

### Sprint Planning

```
For each user story:
  grill-me          → surface technical ambiguities before estimating
  /architect        → if a design decision is involved

Output: stories with technical clarity, more accurate estimates
```

### Daily Development Loop

```
Morning — start the task:
  grill-me
  → clarify approach before writing any code

During the day — build:
  rules/ apply automatically to every interaction
  agents/ self-delegate as needed
  you focus on business logic and domain decisions

Hard or flaky bug:
  diagnose    ← structured 6-phase loop: build feedback loop → reproduce → hypothesise → fix

End of day — pre-commit:
  /code-review BUGS,SECURITY    ← always
  /migrate                      ← if schema changed
  do-work Work Summary          ← document what changed and why
```

### Before Raising a PR

```
/code-review BUGS,SECURITY      ← mandatory, no exceptions
/security-review                ← if auth, payment, or data access changed
dotnet test                     ← all tests pass locally
```

---

## 9. Quick Reference

### By situation

| Situation | Action |
|-----------|--------|
| Vague idea | `grill-me` |
| Need a spec | `write-a-prd` |
| Need a phased plan | `prd-to-plan` |
| Ready to implement | `do-work` |
| Before raising a PR | `/code-review BUGS,SECURITY` |
| Production error | `/incident` → `/debug` |
| Need tests for existing code | `/test` |
| Schema change | `/migrate` |
| Library or SDK question | `docs-explorer` auto-invokes |
| Slow endpoint | `/perf` |
| Design decision | `/architect` |
| GDPR or compliance audit | `/compliance` |
| Joining a new codebase | `/onboard` |
| Build shared domain language + ADRs | `grill-with-docs` |
| Hard or flaky bug | `diagnose` |
| Find architecture improvement opportunities | `improve-codebase-architecture` |

### Five rules to never break

```
1. grill-me BEFORE coding anything non-trivial — prevent building the wrong thing
2. /code-review BUGS,SECURITY before EVERY PR — no exceptions
3. /incident before debugging production — saves significant time
4. /migrate before applying any schema change — prevents data loss
5. do-work Work Summary at end of every session — institutional memory
```

---

## 10. Setup for a New Project

When starting a new project, open this workspace and update three things.

### Step 1 — Update CLAUDE.md Project Context

```markdown
## Project Context
- Project: [name]
- Domain: [business domain]
- Stack: ASP.NET Core Web API / AWS Lambda (.NET 8)
- Specific conventions: [anything that differs from workspace defaults]
```

### Step 2 — Add project-specific examples to dotnet-patterns skill

Add to `.claude/skills/dotnet-patterns/SKILL.md`:

```markdown
## Project-Specific Patterns
[Paste 2–3 representative code examples from this codebase]
[Include: DI module name, base classes, custom middleware, DB access pattern]
```

### Step 3 — Run /onboard

```
/onboard
[describe or paste the codebase structure and main components]
```

Claude builds a mental model of the project, identifies conventions,
and flags high-risk areas to approach carefully.

### New project checklist

```
□ CLAUDE.md Project Context section updated
□ dotnet-patterns skill includes project-specific code examples
□ /onboard completed
□ /security-review run on existing auth endpoints
□ /migrate run on all existing schema files
□ /compliance run if the project handles personal data
```

---

## 11. Adding a New Stack

This workspace was built with .NET as the primary stack, but its core —
the pipeline, roles, rules about code structure and design principles, all agents,
and all skills — is language-agnostic. When you need to work in a new stack
(NestJS, React, Blazor, or anything else), you add a thin layer on top rather
than rebuilding from scratch.

---

### What is stack-agnostic vs stack-specific

Understanding this distinction is the key to extending the workspace correctly.

**Stack-agnostic — keep as-is, never duplicate:**

```
Pipeline skills      grill-me, write-a-prd, prd-to-plan, do-work, write-a-skill
All agents           debugger, code-reviewer, docs-explorer, security-reviewer,
                     test-generator, code-explainer, performance-analyzer
Role guides          docs/roles/ — all five roles, all workflow steps
Core rules           structure.md, design-principles.md, comments.md
Universal commands   architect, agent, pipeline, compliance, onboard,
                     incident, workflow, team-standards
```

**Stack-specific — add new files per stack, never overwrite existing ones:**

```
rules/               naming, async, error-handling, methods-and-classes, testing
context/             architecture, templates, testing
skills/              {stack}-patterns/ (auto-loads on relevant file types)
commands/            generate, review, security-review, debug, refactor,
                     test, migrate, perf, devops, saas
```

---

### Step-by-step: adding a new stack

The process is the same regardless of the stack. NestJS is used as the example.

**Step 1 — Add stack-specific rules**

Create new files in `rules/`. Do not modify existing `.md` files.

```
.claude/rules/
├── naming.md                  ← existing, keep
├── structure.md               ← existing, keep
├── design-principles.md       ← existing, keep
├── comments.md                ← existing, keep
├── async.md                   ← existing (.NET), keep
├── async-nestjs.md            ← NEW: Promise patterns, no .then() chains
├── error-handling.md          ← existing (.NET), keep
├── error-handling-nestjs.md   ← NEW: HttpException, global exception filter
├── naming-dotnet.md           ← optional: extract .NET-specific parts here
├── naming-typescript.md       ← NEW: camelCase files, interface naming, decorators
├── testing.md                 ← existing (.NET), keep
└── testing-nestjs.md          ← NEW: Jest, Supertest, test module setup
```

**Step 2 — Add stack-specific context**

Create new files in `context/`. Do not modify existing ones.

```
.claude/context/
├── architecture.md            ← existing (.NET), keep
├── architecture-nestjs.md     ← NEW: Module → Controller → Service → Repository
├── templates.md               ← existing (.NET), keep
├── templates-nestjs.md        ← NEW: NestJS boilerplate, Prisma, decorators
├── testing.md                 ← existing (.NET), keep
└── testing-nestjs.md          ← NEW: Jest patterns, e2e test setup
```

**Step 3 — Add a stack-specific skill**

Create a new skill directory. This skill will auto-load on relevant file types.

```
.claude/skills/nestjs-patterns/SKILL.md
```

In the skill frontmatter, set the file patterns that trigger it:

```yaml
---
name: nestjs-patterns
description: >
  Auto-loads when working with NestJS projects.
  Use when working with .ts files, nest-cli.json, or when user mentions
  NestJS, Prisma, TypeORM, or TypeScript backend.
context: auto
patterns:
  - "**/*.ts"
  - "**/nest-cli.json"
  - "**/prisma/schema.prisma"
---
```

Content should mirror the structure of `dotnet-patterns/SKILL.md`:
DI patterns, error handling conventions, naming, and 2–3 representative
code examples from a real project.

**Step 4 — Add stack-specific commands**

Create new command files alongside the existing ones. Do not modify existing commands.

```
.claude/commands/
├── generate.md                ← existing (.NET), keep
├── generate-nestjs.md         ← NEW: NestJS module/service/controller scaffold
├── test.md                    ← existing (xUnit), keep
├── test-nestjs.md             ← NEW: Jest test suite generation
├── migrate.md                 ← existing (EF Core / SQL), keep
├── migrate-nestjs.md          ← NEW: Prisma migrate, schema.prisma safety review
├── devops.md                  ← existing (Docker .NET, Azure), keep
└── devops-nestjs.md           ← NEW: Docker Node.js, npm ci patterns
```

For commands where the structure is the same but examples differ
(review, security-review, debug, refactor, perf, saas), you can
either create a new file or extend the existing one with a clearly
labelled stack section at the bottom.

**Step 5 — Update CLAUDE.md Project Context**

Switch which rules and context files are loaded via `@` directives.
Comment out the .NET block and uncomment the NestJS block.

```markdown
## Stack context — uncomment the active stack

<!-- .NET / ASP.NET Core / Lambda
@.claude/rules/async.md
@.claude/rules/error-handling.md
@.claude/rules/naming.md
@.claude/rules/testing.md
@.claude/context/architecture.md
@.claude/context/templates.md
@.claude/context/testing.md
-->

<!-- NestJS / TypeScript
@.claude/rules/async-nestjs.md
@.claude/rules/error-handling-nestjs.md
@.claude/rules/naming-typescript.md
@.claude/rules/testing-nestjs.md
@.claude/context/architecture-nestjs.md
@.claude/context/templates-nestjs.md
@.claude/context/testing-nestjs.md
-->
```

The rules that are always loaded regardless of stack (no comment needed):

```markdown
@.claude/rules/structure.md
@.claude/rules/design-principles.md
@.claude/rules/comments.md
@.claude/rules/checklist.md
@.claude/rules/methods-and-classes.md
```

---

### Stack addition checklist

```
□ rules/{concern}-{stack}.md created for: async, error-handling, naming, testing
□ context/architecture-{stack}.md written: layer flow diagram + responsibilities
□ context/templates-{stack}.md written: at least 3 boilerplate examples
□ context/testing-{stack}.md written: unit + integration test patterns
□ skills/{stack}-patterns/SKILL.md created with correct file patterns in frontmatter
□ commands/generate-{stack}.md created
□ commands/test-{stack}.md created
□ commands/migrate-{stack}.md created (or noted as N/A if no DB migrations)
□ CLAUDE.md updated: new @ directives commented in, old ones commented out
□ /onboard run on a real project using the new stack to validate the setup
```

---

## Notes

**This workspace is a living document.**

- Update `CLAUDE.md` Project Context when switching projects.
- Add new files to `rules/` when you identify a recurring mistake.
- Add new skills to `skills/` when you identify a repeatable workflow.
- Refine `grill-me` and `do-work` as you learn what works in practice.

**Claude is the copilot. You are the pilot.**

What remains yours:
- Business logic decisions
- Domain model design
- Security architecture sign-off
- Compliance and legal decisions
- Performance trade-offs requiring production data
