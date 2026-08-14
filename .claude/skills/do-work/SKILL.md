---
name: do-work
description: >
  Executes a unit of work end-to-end: plans the change, implements it,
  validates via build and test for the detected stack, then produces a
  structured Work Summary. Use when the user asks to implement a feature,
  fix a bug, refactor code, or do any concrete development task.
---

# Do Work

Execute a single, well-scoped unit of development work end-to-end.

## Process

### 1. Plan

**If a plan file already exists** (`plans/{feature}-plan.md` from `prd-to-plan`),
read it and use it. Skip replanning — proceed directly to Step 2.

Otherwise, produce a concise plan before writing any code:
- **Map** — which files own the current behavior, which helpers/services to reuse,
  which layers stay untouched, and the safest insertion point. Seeing files is not
  understanding the system — trace the flow before deciding where code belongs.
  (See `@.claude/rules/safe-modification.md`.)
- **Problem** — what is broken or missing and why it matters
- **Root cause** — the underlying technical reason (if a bug)
- **Approach** — what files/classes will change and how
- **Risks** — anything that could break or needs care

Ask the user to confirm the plan before proceeding.
If scope is ambiguous, ask one clarifying question.

For non-trivial changes, prefer running the `scratchpad` skill first and reading
`scratchpad/{feature}-scratchpad.md` here instead of replanning.

### 2. Detect stack

Scan the project root for indicator files, then use the matching build and test toolchain:

| Indicator file | Stack | Build | Test |
|----------------|-------|-------|------|
| `*.csproj` / `*.sln` | .NET | `dotnet build` | `dotnet test` |
| `package.json` | Node / TypeScript | `npm run build` | `npm test` |
| `go.mod` | Go | `go build ./...` | `go test ./...` |
| `requirements.txt` / `pyproject.toml` | Python | — | `pytest` |
| `Cargo.toml` | Rust | `cargo build` | `cargo test` |

For .NET: scan the project structure to locate the correct `.sln` or `.csproj` path before running.

### 3. Implement

Make only the changes identified in the plan:
- Follow patterns from `@.claude/context/templates.md` and `@.claude/context/architecture.md`
- Follow all rules in `@.claude/rules/`
- No comments unless the WHY is non-obvious
- No extra abstractions or features beyond what the plan specifies

### 4. Build feedback loop

Run the build command for the detected stack. Fix compilation errors before moving on.
Repeat until build is clean.

### 5. Test feedback loop

Run the test command for the detected stack. Fix any failing tests.
Repeat build → test loop until all tests pass.

If integration tests are relevant, run them against the same project path.

### 6. Validate unchanged behavior

Before writing the summary, list the behaviors that must remain unchanged after this
change and explain how the implementation preserves each. Then self-audit the diff
for silent technical debt: duplicated logic, bypassed shared utilities, unnecessary
abstractions, or anywhere the code does more than the plan required. If the
preservation explanation is vague or the audit finds debt, fix it before summarizing.

### 7. Work Summary

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
