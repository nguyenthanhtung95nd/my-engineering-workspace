---
name: write-a-prd
description: >
  Guides the user through creating a Product Requirements Document (PRD) by
  interviewing them, exploring the codebase, and saving a structured document
  to prd/ as a local development artifact. Use when the user wants to write,
  create, or draft a PRD, spec, or product requirement for a new feature.
---

# Write a PRD

## Process

Work through these steps in order. Skip any step clearly unnecessary given context.

### 1. Gather the problem statement
Ask the user for:
- A detailed description of the problem they want to solve
- Any early ideas or constraints on the solution

### 2. Explore the codebase
Read relevant files to verify the user's assertions and understand the current
state of the system before designing anything.

### 3. Interview the user
**If `grill-me` was already run**, skip this step — proceed directly to Step 4.

Otherwise, ask targeted questions until you reach a shared, unambiguous
understanding of the full design. Walk down each branch of the decision tree,
resolving dependencies one at a time. Cover:
- Actors and their goals
- Edge cases and failure modes
- Constraints (performance, security, backwards compatibility)
- Dependencies on other systems or teams

### 4. Identify modules
Sketch the major modules to build or modify. For each, define:
- **Interface** — what it exposes (inputs/outputs), not implementation details
- **Boundary** — what it encapsulates and what it delegates

Prefer **deep modules**: maximum encapsulated functionality behind a minimal,
stable interface that can be tested in isolation.

### 5. Write the PRD
Use the template below. Always render the completed PRD as a markdown block
in the chat so the user can read and copy it.

If file tools are available (Claude Code / CLI), also save it locally:
```
prd/{kebab-case-feature-name}-prd.md
```
Do NOT commit or push. PRDs are local development artifacts.

### 6. Offer to generate an implementation plan
After the PRD is complete, always prompt:
> "Would you like me to turn this PRD into a multi-phase implementation plan?"

If the user agrees, invoke the **`prd-to-plan`** skill.

---

## PRD Template

```markdown
## Problem Statement

[The problem the user is facing, described from their perspective.]

## Solution

[The proposed solution, described from the user's perspective.]

## User Stories

A numbered list covering all actors and scenarios. Format:
> As a **{actor}**, I want **{feature}**, so that **{benefit}**.

Be exhaustive — include happy paths, edge cases, and error scenarios.

## Implementation Decisions

Key technical decisions made during the interview:
- Modules to build or modify, and their interfaces
- Architectural decisions and trade-offs
- Schema changes
- API contracts
- Specific interaction flows

Do NOT include file paths or code snippets — these become outdated quickly.

## Out of Scope

Explicit list of things this PRD does not cover.

## Further Notes

Any remaining context, open questions, or follow-up items.
```
