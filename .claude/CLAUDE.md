# Claude Code — Master Workspace

## Who I Am
- Senior C#/.NET Backend Developer
- Working on both ASP.NET Core Web API and AWS Lambda (.NET 8) projects
- Using Agile / Scrum — sprint-based, user story driven

## Tech Stack

### ASP.NET Core Web API
- .NET 8 · ASP.NET Core · Entity Framework Core · SQL Server / PostgreSQL
- xUnit · NSubstitute · Testcontainers
- Azure (AKS, Azure SQL, Redis, Service Bus, ACR)

### AWS Lambda
- .NET 8 · AWS Lambda · SQS · SNS · RDS MySQL
- Autofac · Dapper · Polly
- xUnit · Moq · Testcontainers

## How I Work With You

1. Default language is C# unless I say otherwise
2. Follow .NET conventions — PascalCase, async/await, SOLID, primary constructors
3. XML doc comments on all public methods
4. Result<T> for error handling in ASP.NET Core — never return null or throw generic exceptions
5. Production-ready by default — not just code that runs, code that is correct

## Response Format
- Code first, explanation after
- Brief explanation of key decisions
- Call out unhandled edge cases
- Show trade-offs when multiple approaches exist

---

## Feature Development Pipeline

Every feature moves through this pipeline. Enter at whichever stage you already have.

```
Vague idea
    ↓
grill-me          → shared understanding (one question at a time)
    ↓
write-a-prd       → prd/{feature}-prd.md
    ↓
prd-to-plan       → plans/{feature}-plan.md
    ↓
scratchpad        → scratchpad/{feature}-scratchpad.md   (non-trivial changes only; skip for small tasks)
    ↓
do-work           → code + tests + Work Summary
    ↓
ship-feature      → /code-review → /security-review → /test-coverage → /pr-summary
```

---

## Architecture Reference
@.claude/context/architecture.md

## Code Templates
@.claude/context/templates.md

## Testing Guide
@.claude/context/testing.md

---

## Engineering Standards (always active)

@.claude/rules/naming.md
@.claude/rules/structure.md
@.claude/rules/methods-and-classes.md
@.claude/rules/async.md
@.claude/rules/error-handling.md
@.claude/rules/design-principles.md
@.claude/rules/comments.md
@.claude/rules/testing.md
@.claude/rules/safe-modification.md
@.claude/rules/checklist.md

---

## Subagents (invoked automatically)

| Situation | Subagent |
|-----------|----------|
| Any error, test failure, unexpected behavior | **debugger** |
| After writing or modifying any code | **code-reviewer** |
| Any question about a library, framework, or SDK | **docs-explorer** |
| Security review needed | **security-reviewer** |
| Need tests for existing code | **test-generator** |
| Unfamiliar or legacy code | **code-explainer** |
| Slow endpoint or N+1 suspected | **performance-analyzer** |

---

## Skills (pipeline + reusable workflows)

| Skill | Trigger |
|-------|---------|
| `grill-me` | Sharpen a vague idea before writing PRD |
| `grill-with-docs` | Grill + build CONTEXT.md + record ADRs |
| `write-a-prd` | Create a structured PRD |
| `prd-to-plan` | Turn PRD into phased implementation plan |
| `scratchpad` | Persistent working memory for a non-trivial change; also holds Recovery workflow |
| `do-work` | Implement feature or fix — build/test loop |
| `ship-feature` | Pre-PR orchestrator — runs all 4 review steps |
| `diagnose` | Hard or flaky bugs — 6-phase feedback loop |
| `improve-codebase-architecture` | Find deepening opportunities post-feature |
| `build-prototype` | Throwaway prototype to answer a design question — UI (wireframe or multi-variant) or logic/state |
| `resolving-merge-conflicts` | Resolve an in-progress git merge/rebase conflict safely |
| `ba-analysis` | API analysis, data mapping, UAT, SQL |
| `write-ba-docs` | BRD, FRD, User Stories for stakeholder sign-off |
| `write-a-skill` | Create a new skill for this workspace |
| `write-tech-docs` | Write technical docs (Getting Started, Tutorial, How-to, Reference) |
| `self-learning` | Study a course/book/mindset with active recall, teach-back & spaced review (`/self-learning <topic>`) |
| `dotnet-patterns` | Auto-loads on .cs files |
| `security-audit` | Auto-loads on auth/payment/data files |
| `architecture-decision` | Auto-loads on design discussions |

---

## Manual Commands

| Command | When |
|---------|------|
| `/code-review` | Bugs + performance on all changed files |
| `/security-review` | OWASP Top 10 deep dive |
| `/test-coverage` | Coverage gap report for changed files |
| `/pr-summary` | PR description from git diff |
| `/review` | Structured code review with Constitutional Verification (paste code) |
| `/generate` | Scaffold production-ready code |
| `/debug` | Root cause analysis |
| `/refactor` | Three-phase legacy refactoring |
| `/test` | Generate tests for existing code |
| `/migrate` | Database migration safety review |
| `/incident` | Real-time production incident triage |
| `/perf` | Performance profiling analysis |
| `/compliance` | GDPR / SOC2 readiness review |
| `/onboard` | Understand a new codebase |
| `/pipeline` | Automation pipeline design |
| `/agent` | Multi-step agent with guardrails |
| `/devops` | Dockerfile, CI/CD, postmortem |
| `/architect` | Architecture planning + ADRs |
| `/saas` | SaaS product engineering |
| `/workflow` | Daily task planning |
| `/team-standards` | Team norms, onboarding, quality gates |

---

## Working Habits

- **Separate planning from implementation.** Do not mix "decide the approach" and
  "write the code" in one prompt. Use plan mode / `scratchpad` first, then implement.
- **Context hygiene.** `/clear` when switching to a genuinely different task;
  `/compact` when the current task is long. If answers start feeling too broad or
  off-track, suspect context drift: `/clear`, reread rules, reload the scratchpad,
  restate the goal.
- **Living document.** Correct the same mistake twice → encode it once, at the
  project level (this file or memory), instead of repeating it every session.
- **Constraints over wording.** A strong prompt is specific, not long — state what
  must change, what must not, and which trade-offs matter.

## What I Still Own
- Business logic decisions
- Domain model design
- Security architecture sign-off
- Compliance and legal decisions
- Performance trade-offs requiring production data
- Team and process decisions

## Project Context

> Fill this in per project (leave blank in the master template). These are
> project-specific facts Claude cannot reliably infer — they carry the biggest
> weight in the decision hierarchy.

- Project: [name]
- Domain: [business domain]
- Specific conventions: [anything that differs from defaults above]

### Project Boundaries
The single strongest guard against over-engineering. List what the project is —
and, just as importantly, what it is not.

**In-scope:**
- [feature / responsibility this project owns]

**Out-of-scope:**
- [things Claude must NOT add: no new services, no DB, no auth, no CI/CD, etc.]

### Environment Facts
Hard facts about the environment (not preferences). Prevents Claude from assuming
hosted services or config layers that do not exist. Auto-populated by `/onboard`.

| Tool / Service | Value |
|----------------|-------|
| .NET SDK | [version] |
| OS / Shell | [e.g. Windows 11 / PowerShell] |
| Database | [engine + host:port, local vs cloud] |
| Cache / Queue | [Redis / Service Bus / SQS — host or "none"] |
| Container runtime | [Docker version or "none"] |
| Cloud / Region | [Azure/AWS region or "local only"] |
