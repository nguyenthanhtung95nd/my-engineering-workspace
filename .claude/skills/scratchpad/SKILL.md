---
name: scratchpad
description: >
  Creates and maintains a persistent per-feature working-memory file
  (scratchpad/{feature}-scratchpad.md) that survives /clear and interrupted
  sessions. Maps the existing system, catalogs edge cases and performance-
  sensitive paths, records risks/assumptions, holds draft ADRs, and tracks
  progress. Use at the start of any non-trivial change, before do-work.
  Also holds the Recovery workflow for resuming an interrupted session.
---

# Scratchpad

The **thinking layer** of the workspace. If `CLAUDE.md` controls how Claude
behaves and `plans/` says which phase to build, the scratchpad is the working
memory for the current change — the one artifact that survives `/clear` and a
killed terminal.

## When to use

- **Use** for any non-trivial change: touches more than 2–3 files, adds a new
  service/endpoint, changes a data flow, or has unresolved architectural options.
- **Skip** for small clear tasks (single-file fix, add a column, tighten a guard).
  Use `do-work`'s inline plan instead — do not create a scratchpad for these.

Sits in the pipeline as: `grill-me → write-a-prd → prd-to-plan → scratchpad → do-work`.
The scratchpad is optional and complementary — it never replaces the PRD or the plan.

## Rules

- **Plan, do not code.** This skill produces/updates a document only. Do not modify
  application code while building the scratchpad. Map first — seeing files is not
  understanding the system.
- **One file per feature:** `scratchpad/{feature}-scratchpad.md`. Never one giant
  shared file — stale planning docs are worse than none.
- **Living document:** update it when scope changes, a new constraint appears, an
  assumption is corrected, or a decision turns out wrong. Always bump *Last updated*.

## Process

### 1. Map the existing system (before proposing anything)
Trace the current flow end-to-end. Identify which files own the behavior, which
services/helpers are already involved, which layers must stay untouched, and where
new logic actually belongs. Read `CLAUDE.md` and any `CONTEXT.md` first so the map
respects existing rules and domain language.

### 2. Write the scratchpad
Create `scratchpad/{feature}-scratchpad.md` with these sections:

```markdown
# {Feature} — Scratchpad
Working document. Tracks analysis, decisions, and progress. Not a spec.

## 1. Flow / Map Analysis
[Request lifecycle, files involved, safest insertion point]

## 2. Edge Cases
| # | Edge case | Current behavior | Potential issue |

## 3. Performance-Sensitive Paths
| Priority | Path | Concern | Current mitigation |   (rank P0–P3)

## 4. Risks & Assumptions
| # | Risk | Severity | Likelihood | Detail |
| # | Assumption | Basis | What breaks if wrong |

## 5. Architecture Decision Log (draft)
### ADR-00X: {title} — Pending | Decided
**Context** · **Options** · **Decision** · **Constraints** · **Status**

## 6. Progress Tracker
| Task | Status | Notes |
### Backlog
| Task | Priority | Depends on |

---
*Last updated: YYYY-MM-DD*
```

### 3. Review before coding
Read the scratchpad back. If the map is wrong, a constraint is missed, or a risky
direction is proposed, fix it now — before a single line of code changes.

### 4. Resolve open decisions under explicit constraints
For each `Pending` ADR, state the hard constraints and let Claude converge on the
simplest option that fits them. Constraints prevent over-engineering — always give
them (e.g. "no new dependencies", "Node/framework built-ins only", "must persist").

### 5. Hand off to engineering mode
Only once the map is stable and ADRs are decided, tell Claude to move from planning
into implementation (→ `do-work`). Keep planning and coding in separate prompts.

### 6. Draft ADR → official ADR
When an ADR moves to **Decided** and it is hard-to-reverse + surprising + a real
trade-off, promote it from the scratchpad to `docs/adr/` via `grill-with-docs`.
The scratchpad keeps a one-line pointer to the promoted ADR.

## Keeping it current after implementation
When `do-work` finishes, update the scratchpad: mark ADRs resolved, move items to
the completed section of the Progress Tracker, and record what was actually built.
An unmaintained scratchpad creates false confidence — treat drift as a bug.

## Recovery — resuming an interrupted session

A killed terminal or an interrupted session does not lose the code on disk — it
loses **context**. Recovery is context restoration, not re-implementation. The
Progress Tracker in this file is the anchor.

Write a recovery prompt with four parts, in order:

1. **State what was completed and where it stopped.** Point at the scratchpad
   Progress Tracker.
2. **Review the current codebase first.** Have Claude inspect the actual files
   before acting, so it rebuilds understanding from the repo, not assumptions.
3. **Assign specific, non-destructive follow-up tasks only** (e.g. finish docs,
   update the tracker).
4. **Set firm boundaries on what NOT to repeat** — especially the operation that
   caused the interruption (on Windows/PowerShell, forcibly killing a Node process
   with `Stop-Process`/`taskkill` can close the session itself; those commands are
   denied in `settings.json`, stop servers with Ctrl+C by hand instead).

Do not re-implement work that already exists — that creates duplicate edits and
conflicting docs. Inspect first, continue second.
