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
| `do-work` | Implement feature or fix — build/test loop |
| `ship-feature` | Pre-PR orchestrator — runs all 4 review steps |
| `diagnose` | Hard or flaky bugs — 6-phase feedback loop |
| `improve-codebase-architecture` | Find deepening opportunities post-feature |
| `ba-analysis` | API analysis, data mapping, UAT, SQL |
| `write-ba-docs` | BRD, FRD, User Stories for stakeholder sign-off |
| `write-a-skill` | Create a new skill for this workspace |
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

## What I Still Own
- Business logic decisions
- Domain model design
- Security architecture sign-off
- Compliance and legal decisions
- Performance trade-offs requiring production data
- Team and process decisions

## Project Context
- Update this section when working on a specific project
- Project: [name]
- Domain: [business domain]
- Specific conventions: [anything that differs from defaults above]
