---
name: write-ba-docs
description: >
  Produces business-facing BA documents: BRD, FRD, and User Stories with
  Acceptance Criteria. Output is written for stakeholders and business sign-off,
  not for developers to implement. Use when the user wants to write a BRD,
  FRD, user stories, acceptance criteria, or any requirement document that
  a business stakeholder or product owner needs to review and approve.
---

# Write BA Docs

## Step 1 — Identify which documents to produce

**If the user passed a parameter**, use it directly:
- `write-ba-docs brd` → BRD only
- `write-ba-docs frd` → FRD only
- `write-ba-docs stories` → User Stories only
- `write-ba-docs brd frd` → BRD + FRD
- `write-ba-docs all` → all three documents

**If no parameter was given**, ask before doing anything:

> Which document(s) would you like me to write?
>
> 1. **BRD** — Business Requirements Document (for sponsor / business owner)
> 2. **FRD** — Functional Requirements Document (for business + dev team)
> 3. **User Stories** — Stories + Acceptance Criteria (for product owner / scrum team)
> 4. **All three**
>
> Reply with a number, a name, or a combination (e.g. "1 and 2", "BRD", "all").

Wait for the user's answer before proceeding.

---

## Step 2 — Gather context (if not already provided)

Before writing any document, confirm the 5 elements are present.
If any is missing, ask for it — one question at a time.

```
1. Business context      — What domain / process is this feature part of?
2. System context        — Which systems are involved?
3. Edge cases            — Key negative / boundary scenarios?
4. Downstream impact     — Which systems, reports, or teams are affected?
5. Output expectation    — Any specific format or depth required?
```

If `grill-me` was already run, skip this step — context is already established.

---

## Step 3 — Write the selected document(s)

### BRD — Business Requirements Document

Audience: sponsor, business owner, steering committee.
Answers: why this project exists and what the business needs to achieve.

**Prompt:**
```
Act as a Senior BA. Write a BRD for [project / feature].
Business goal: [what outcome the business wants]
Stakeholders: [who is involved or affected]
Current problem: [pain point or gap today]
Out of scope: [what this project will not address]
Output as structured sections: Executive Summary, Business Objectives,
Stakeholders, Current State, Desired State, High-Level Requirements,
Assumptions, Constraints, Out of Scope.
```

---

### FRD — Functional Requirements Document

Audience: business analyst, developer, QA.
Answers: what the system must do, field by field, rule by rule.

**Prompt:**
```
Act as a Senior BA. Write an FRD for [feature] in [system].
Actors: [who interacts with the system]
Business rules: [key rules governing this feature]
Integrations: [external systems involved]
Edge cases: [invalid input, boundary values, error scenarios]
Downstream impact: [systems or reports affected]
Output as structured sections: Overview, Actors, Business Rules,
Functional Requirements (table: Req ID | Description | Rule | Priority),
Non-Functional Requirements, Out of Scope.
```

---

### User Stories + Acceptance Criteria

Audience: product owner, scrum team, QA.
Answers: who needs what, and what does "done" look like.

**Prompt:**
```
Act as a Technical BA on [platform].
Write User Stories for [feature] covering:
- All actors (user roles, system, external systems)
- Happy path and negative scenarios
- API validation rules per field
- Downstream impact on [systems]
- Error handling and retry scenarios
Output as a table:
Story ID | Actor | User Story | Acceptance Criteria (Given/When/Then) | Priority | Dependencies
```

---

## Step 4 — Save output

Save each completed document to `ba/` at the workspace root.
Use kebab-case naming with document type as suffix:

```
ba/{feature-name}-brd.md
ba/{feature-name}-frd.md
ba/{feature-name}-user-stories.md
```

Always render the document in chat first so the user can review before saving.
Do NOT commit or push. BA documents are local working artifacts
until stakeholder sign-off is complete.

---

## Document reference

| Document | Audience | Key sections |
|----------|----------|-------------|
| BRD | Sponsor, business owner | Executive Summary, Objectives, Current vs Desired State, Requirements, Constraints |
| FRD | Business + dev team | Overview, Actors, Business Rules, Functional Requirements table, Out of Scope |
| User Stories | Product owner, scrum team | Story ID, Actor, Story, AC (Given/When/Then), Priority, Dependencies |

---

## See also

- `grill-me` — run first when requirements are still vague
- `ba-analysis` — for API analysis, data mapping, UAT, SQL tasks
- `/impact` — when the feature changes an existing system or business rule
