---
name: ba-analysis
description: >
  BA-specific analysis workflows covering API/integration analysis, data & SQL,
  requirements drafting, UAT test cases, impact analysis, and production support.
  Use when the user is working as a BA on tasks like reading Swagger docs, writing
  user stories, mapping data fields, creating UAT test cases, analyzing change
  impact, or investigating production issues from a business perspective.
---

# BA Analysis

## The 5-Element Prompt Rule

Every BA prompt must include these five elements for high-quality AI output:

```
1. Business context      — What is the business process / domain?
2. System context        — Which systems are involved?
3. Technical expectation — What format / depth is expected?
4. Edge cases            — What negative / boundary scenarios matter?
5. Downstream impact     — Which systems / teams are affected?
```

**Weak prompt:**
> "Write User Stories for the payment feature."

**Strong prompt:**
> "Act as a Technical BA on a Wealth Management platform.
> The feature is Mutual Fund Redemption via API integration with a Fund Accounting system.
> Write User Stories covering: API validations, retry mechanism, downstream impact on
> the portfolio ledger, data quality rules, and negative scenarios (insufficient balance,
> fund suspended, settlement failure).
> Output as a table: Story ID | Actor | Story | Acceptance Criteria | Priority."

---

## Workflows by Task Type

### 1. Requirements Analysis (BRD / FRD / Use Cases)

**Prompt starter:**
```
Act as a Senior BA. Draft functional requirements for [feature] in [system]
considering [business goals, actors, rules, and validations].
Include: happy path, edge cases, error handling, and out-of-scope items.
Output as a structured FRD with sections: Overview, Actors, Business Rules,
Functional Requirements, Non-Functional Requirements, Out of Scope.
```

**Checklist before drafting:**
- [ ] Problem statement is clear
- [ ] All actors identified (user, system, external)
- [ ] Business rules documented
- [ ] At least 3 edge cases listed

---

### 2. API & Integration Analysis

**Prompt starter:**
```
Analyze the following API specification and explain:
- Purpose of each endpoint
- Required vs optional parameters
- Response structure and status codes
- Error codes and their business meaning
- Integration flow and downstream impact
[Paste Swagger JSON or endpoint description here]
```

**When reading a Swagger / OpenAPI spec, ask Claude to:**
- Map each endpoint to a business use case
- Identify which fields are mandatory for your flow
- List all error codes and suggest BA-level handling notes

---

### 3. Data Mapping & SQL

**Prompt starter:**
```
Map the fields between source [System A] and target [System B].
Source fields: [list or paste schema]
Target fields: [list or paste schema]
For each mapping: source field → target field | transformation rule | data type | nullable.
Flag fields with no mapping and suggest default values or business rules.
```

**SQL generation starter:**
```
Write an SQL query to [objective] from [table(s)] with conditions [...].
Use clear aliases. Explain the logic step by step.
Flag any performance concern if the table is large.
```

---

### 4. User Stories & Acceptance Criteria

**Prompt starter:**
```
Create user stories for [feature] with:
- Business value and priority (MoSCoW)
- Acceptance criteria in Given / When / Then format
- API validation rules per field
- Retry and error handling scenarios
- Downstream impact on [systems]
- Negative scenarios (what must NOT happen)
Output as a table: Story ID | Actor | User Story | AC | Priority | Dependencies.
```

---

### 5. UAT Test Cases

**Prompt starter:**
```
Create UAT test cases for [feature] covering:
- Positive scenarios (happy path)
- Negative scenarios (invalid input, missing fields, boundary values)
- Exception scenarios (system unavailable, timeout, partial failure)
For each case: Test ID | Scenario | Precondition | Steps | Expected Result | Priority.
```

---

### 6. Impact & Gap Analysis

**Prompt starter:**
```
Analyze the impact of [change / defect] on [system / process / data].
Cover: upstream triggers, downstream systems affected, data integrity risks,
API contract changes, UI changes, reporting impact, and rollback considerations.
Output as: Impact Area | Current State | Future State | Risk Level | Action Required.
```

See also: `/impact` command for a dedicated impact analysis workflow.

---

### 7. Production Support (Log / Root Cause)

**Prompt starter:**
```
Given the error / log below, identify:
- Root cause (business and technical perspective)
- Affected components and data
- Business impact (which users / transactions / reports)
- Immediate fix and preventive recommendations
[Paste log or error here]
```

---

## Output Format Reference

| Task | Recommended Output |
|------|--------------------|
| Requirements | Structured FRD sections |
| User Stories | Table: ID, Actor, Story, AC, Priority |
| API Analysis | Endpoint table + flow diagram description |
| Data Mapping | Field mapping table with transformation rules |
| UAT Cases | Table: ID, Scenario, Steps, Expected Result |
| Impact Analysis | Impact table: Area, Current, Future, Risk, Action |
| SQL | Query + step-by-step logic explanation |

---

## Common Mistakes to Avoid

- Writing one-line prompts without context
- Forgetting to specify output format
- Not including negative / edge case scenarios
- Pasting large documents without structure — extract key sections first
- Accepting AI output without validation against actual business rules
