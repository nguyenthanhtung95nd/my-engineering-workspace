# Rules: Safe Modification

Changing existing code safely matters more than changing it fast. The biggest
risk in a mature codebase is not a syntax error — it is **behavioral drift**: a
change that looks correct in isolation while quietly altering how the rest of the
system behaves. These rules apply to **every** change, including small ones.

## Seeing files is not understanding the system

Reading a file is not the same as understanding runtime relationships, call paths,
ownership boundaries, or implicit coupling. Assume Claude can inspect quickly but
must still be guided toward understanding. **Map before modifying.**

For any non-trivial change, first identify:
- Which files own the current behavior
- Which functions/services/helpers are already involved
- Which shared utilities should be reused (never duplicated)
- Which layers must remain untouched
- Where the new logic belongs — and where it does not

## Locate the safest insertion point

Not the most convenient place — the place it belongs architecturally. A safe
insertion point is already responsible for closely related behavior, allows reuse
of existing helpers, minimizes files changed, preserves abstraction boundaries, and
avoids creating a duplicate pathway for the same behavior.

## Minimal-change rules

- Do not refactor unrelated code.
- Reuse existing utilities and services where possible.
- Keep the implementation localized to the mapped area.
- Do not introduce new abstractions unless required by the current ticket (YAGNI).
- Preserve existing structure and naming patterns.
- Implement the simplest correct version first — do not optimize a still-evolving
  feature early.

## Trigger words signal permission, not neutral verbs

In an AI-assisted workflow, these words are read as *permission to redesign the
system*. Never use them without an explicit scope:

- "refactor" · "improve" · "optimize" · "clean up" · "modernize"

> Vague: "Refactor slide generation logic."
> Scoped: "Make a small, localized improvement to slide generation logic **without
> changing its structure**."

## Practices to avoid (debt generators)

- "Just add it quickly." → invites rushed, unreviewed implementation.
- "Refactor while we're here." → opens the door to unrelated structural change.
- "We'll clean it up later." → normalizes unverified debt as acceptable.

## Audit for silent technical debt

After a change, self-review the diff specifically for: duplicated logic, bypassed
shared utilities, unnecessary abstractions, inefficient patterns, and anywhere the
code does more than the ticket required.

## Validate unchanged behavior

Safety must be demonstrated, not hoped for. Before considering a change done, list
the behaviors that must remain unchanged and explain how the implementation
preserves each. If that explanation is vague or incomplete, the change is not safe yet.

## Decision hierarchy (how instructions are prioritized)

When guidance conflicts, higher wins:

1. **System/project rules** — `CLAUDE.md`, these `rules/`, Project Boundaries.
2. **The current prompt.**
3. **Existing code patterns** in the repository.
4. **Inferred general best practices** — the weakest signal; only fills gaps.

A weak prompt falls back to (4), which may not fit this codebase. Constrain the
prompt so (2) is strong, and encode recurring corrections into (1).
