---
name: do-work
description: >
  Executes a unit of work end-to-end: plans the change, implements it, validates
  via dotnet build and dotnet test (unit + integration), then produces a structured
  Work Summary. Use when the user asks to implement a feature, fix a bug, refactor
  code, or do any concrete development task. Works for both ASP.NET Core Web API
  and AWS Lambda (.NET 8) projects.
---

# Do Work

Execute a single, well-scoped unit of development work end-to-end.

## Process

### 1. Plan

**If a plan file already exists** (`plans/{feature}-plan.md` from `prd-to-plan`),
read it and use it. Skip replanning — proceed directly to Step 2.

Otherwise, produce a concise plan before writing any code:
- **Problem** — what is broken or missing and why it matters
- **Root cause** — the underlying technical reason (if a bug)
- **Approach** — what files/classes will change and how
- **Risks** — anything that could break or needs care

Ask the user to confirm the plan before proceeding.
If scope is ambiguous, ask one clarifying question.

### 2. Implement

Make the changes identified in the plan:
- Edit only the files required by the plan
- Follow patterns from `@.claude/context/templates.md` and `@.claude/context/architecture.md`
- Follow all rules in `@.claude/rules/`
- No comments unless the WHY is non-obvious
- No extra abstractions or features beyond what the plan specifies

**Stack detection** — read existing files to determine which stack is in use:
- `Function.cs` + `Autofac` → Lambda stack → follow Lambda patterns
- `Program.cs` + `WebApplication.CreateBuilder` → ASP.NET Core → follow Web API patterns

### 3. Build feedback loop

After implementing, run `dotnet build` for the affected project. Fix compilation errors before moving on.

```bash
# ASP.NET Core
dotnet build {ProjectName}.sln -c Release

# Lambda
dotnet build lambda/{LambdaName}/{LambdaName}.sln -c Release
```

Repeat until build is clean.

### 4. Test feedback loop

Run unit tests for the affected project:

```bash
# ASP.NET Core
dotnet test {ProjectName}.Tests/{ProjectName}.Tests.csproj -c Release

# Lambda
dotnet test lambda/{LambdaName}/UnitTests/{ProjectName}.UnitTests.csproj -c Release
```

If integration tests are relevant:
```bash
dotnet test lambda/{LambdaName}/IntegrationTests/{ProjectName}.IntegrationTests.csproj -c Release
```

Fix any failing tests. Repeat build → test loop until all tests pass.

### 5. Work Summary

End every do-work session with this structured summary:

---

## Work Summary

**Problem**
One sentence: what was wrong or missing.

**Root Cause**
The specific technical reason (wrong key, missing null check, incorrect logic, etc.).

**What Changed**
Bullet list of files changed and what was changed in each.

**How It Was Fixed**
Concise description of the fix and why it resolves the root cause.

**How to Improve Further** *(optional)*
Anything left out of scope, follow-up tickets worth raising, or architectural improvements.

**Test Results**
- Build: PASS / FAIL
- Unit tests: X passed, Y failed
- Integration tests: X passed, Y failed (or "not run")
