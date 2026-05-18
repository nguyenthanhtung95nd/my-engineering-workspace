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

### 4. Apply the BA 5-Element Check

Before writing anything, confirm all five elements are present.
If any element is missing, ask for it — do not proceed without it.

```
Element 1 — Business context
  What is the business process or domain this feature belongs to?
  Example: "Mutual Fund Redemption on a Wealth Management platform"

Element 2 — System context
  Which systems are involved? What integrations exist?
  Example: "Trade API → Fund Accounting system → Portfolio Ledger"

Element 3 — Technical expectations
  What depth and format does the output need?
  Example: "FRD with API contract, validation table, error handling"

Element 4 — Edge cases
  What negative / boundary scenarios must be covered?
  Example: "Insufficient balance, fund suspended, T+2 settlement cutoff"

Element 5 — Downstream impact
  Which systems, reports, or teams are affected by this feature?
  Example: "Settlement report, portfolio valuation batch, compliance feed"
```

**Checklist: do not proceed to Step 5 until all five are confirmed.**

### 5. Identify modules
Sketch the major modules to build or modify. For each, define:
- **Interface** — what it exposes (inputs/outputs), not implementation details
- **Boundary** — what it encapsulates and what it delegates

Prefer **deep modules**: maximum encapsulated functionality behind a minimal,
stable interface that can be tested in isolation.

### 6. Write the PRD
Use the template below. Always render the completed PRD as a markdown block
in the chat so the user can read and copy it.

If file tools are available (Claude Code / CLI), also save it locally:
```
prd/{kebab-case-feature-name}-prd.md
```
Do NOT commit or push. PRDs are local development artifacts.

### 7. Offer to generate an implementation plan
After the PRD is complete, always prompt:
> "Would you like me to turn this PRD into a multi-phase implementation plan?"

If the user agrees, invoke the **`prd-to-plan`** skill.

---

## PRD Template

```markdown
## Problem Statement

[The problem the user is facing, described from their perspective.]

## Business Context

[The business domain, process, and goals this feature serves.]
[Reference: BA 5-Element — Element 1]

## Systems Involved

[List of systems, integrations, and data flows relevant to this feature.]
[Reference: BA 5-Element — Element 2]

## Solution

[The proposed solution, described from the user's perspective.]

## User Stories

A numbered list covering all actors and scenarios. Format:
> As a **{actor}**, I want **{feature}**, so that **{benefit}**.

Be exhaustive — include happy paths, edge cases, and negative scenarios.

### Acceptance Criteria

For each story, write AC in Given / When / Then format:
> **Given** [precondition], **When** [action], **Then** [expected result].

### Validation Rules

| Field | Rule | Error Message | Severity |
|-------|------|--------------|----------|

### Edge Cases & Negative Scenarios

[Reference: BA 5-Element — Element 4]
List at minimum: invalid input, boundary values, system unavailable, partial failure.

## Implementation Decisions

Key technical decisions made during the interview:
- Modules to build or modify, and their interfaces
- Architectural decisions and trade-offs
- Schema changes
- API contracts
- Specific interaction flows

Do NOT include file paths or code snippets — these become outdated quickly.

## Downstream Impact

[Reference: BA 5-Element — Element 5]
[List systems, reports, or teams that are affected by this feature.]
[Flag any API contract changes, data format changes, or SLA impacts.]

## Out of Scope

Explicit list of things this PRD does not cover.

## Further Notes

Any remaining context, open questions, or follow-up items.
```

---

## BA Prompt Patterns (for complex features)

Use these as the opening line when starting a PRD session for API-heavy or
integration-heavy features:

**API integration feature:**
```
Act as a Technical BA on [platform]. The feature is [name].
It involves API integration with [system]. Write a PRD covering:
business rules, API validations, error handling, retry logic,
downstream impact on [systems], and negative scenarios.
```

**Data migration / schema change:**
```
Act as a Senior BA. We are changing [field/table/rule] in [system].
Write a PRD covering: current state, future state, migration logic,
data integrity risks, downstream consumers affected, and rollback plan.
```

**New reporting requirement:**
```
Act as a BA on [domain]. We need a new report showing [objective].
Data sources: [tables/APIs]. Write a PRD covering: report structure,
filters, business rules for each metric, edge cases for null/zero values,
and access control requirements.
```
