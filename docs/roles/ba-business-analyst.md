# Role: BA — Business Analyst

> Responsible for clarifying business requirements, writing stakeholder-facing
> documents, and ensuring the business need is fully understood before any
> development begins. Works between the stakeholder and the development team.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `grill-me` | Surface business ambiguities one question at a time before writing anything |
| `write-ba-docs` | Write BRD, FRD, and User Stories for stakeholder review and sign-off |
| `ba-analysis` | API analysis, data mapping, UAT test cases, SQL, validation rules |
| `/impact` | Structured impact & gap analysis when a requirement or rule is changing |

---

## Workflow

```
1. grill-me        → Interview stakeholder, resolve all ambiguities first
2. write-ba-docs   → BRD / FRD / User Stories → stakeholder sign-off
3. ba-analysis     → Deep-dive on API, data, UAT, or validation when needed
4. /impact         → Run if this feature touches an existing system or business rule
5. Hand to DEV     → Documents are complete, unambiguous, signed off
```

---

## The 5-Element Prompt Rule

Before writing any prompt to Claude, confirm all five are present.
Missing any one leads to generic, unusable output.

```
1. Business context      — What domain / process is this feature part of?
2. System context        — Which systems are involved? What integrations exist?
3. Technical expectation — What format and depth is expected in the output?
4. Edge cases            — What can go wrong? Boundary values? Negative scenarios?
5. Downstream impact     — Which systems, reports, or teams are affected?
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

## Real-World Example

**Scenario:** The same Product CRUD Lambda project. Stakeholder says:
> "I want to be able to manage products — create, read, update, delete."

This sounds simple. It is not.

---

### Step 1 — grill-me: One question at a time

**Q1:** What fields does a Product have?
*My recommendation: start minimal — Id, Name, Description, Price, StockCount, IsActive,
CreatedAt, UpdatedAt. Confirm which are required vs optional.*

**Q2:** Who can create, update, and delete products? Any role distinction?
*My recommendation: distinguish at least two roles — Admin (full CRUD) and
Viewer (read-only).*

**Q3:** When a product is "deleted", is it a hard delete or soft delete?
*My recommendation: soft delete — set `IsActive = false`. Hard delete loses
audit history and breaks references in order history.*

**Q4:** Are there validation rules on Price or StockCount?
*My recommendation: Price ≥ 0, StockCount ≥ 0. Clarify whether Price = 0
is valid (free products) or should be rejected.*

**Q5:** Should the list support filtering or pagination?
*My recommendation: yes to pagination (default 20, max 100) from day one —
retrofitting is expensive.*

---

### Step 2 — write-ba-docs: FRD output

```markdown
## Overview
The operations team needs to manage the product catalogue through a system.
Currently products are managed directly in the database, which is error-prone
and inaccessible to non-technical staff.

## Actors
- Admin — full create, update, soft-delete access
- Viewer — read-only access

## Business Rules
- Price must be ≥ 0 (zero is valid for free products)
- StockCount must be ≥ 0
- Deletion is soft only — IsActive set to false, record is never removed
- Deleted products must not appear in any listing

## Functional Requirements

| Req ID | Description | Business Rule | Priority |
|--------|-------------|--------------|----------|
| FR-01 | Admin can create a product | All required fields must be present | Must |
| FR-02 | Admin can update any product field | Price and StockCount ≥ 0 | Must |
| FR-03 | Admin can soft-delete a product | Sets IsActive = false | Must |
| FR-04 | Viewer can retrieve a product by ID | Returns active products only | Must |
| FR-05 | Viewer can list products with pagination | Default 20, max 100 per page | Must |
| FR-06 | System rejects invalid input | Returns field-level error messages | Must |

## Out of Scope
- Product images or media attachments
- Product categories or tagging
- Inventory reservation or stock locking
- Audit log of who changed what
```

---

### Step 3 — /impact: Before handing to DEV

```
/impact
CHANGE:   New product management feature
SYSTEM:   Product API
TRIGGER:  Operations team request
CURRENT:  Products managed directly in database
FUTURE:   Managed via API with role-based access
DATA:     products table, any downstream order or reporting queries
```

---

## BA Checklist

```
□ grill-me completed — all business ambiguities resolved with stakeholder
□ 5-element prompt rule applied to every Claude prompt
□ Documents cover ALL actors and their goals
□ Acceptance criteria are testable — QA can write test cases from them
□ Edge cases documented: invalid input, missing data, boundary values
□ Out-of-scope section is explicit — prevents scope creep
□ /impact run if this touches an existing system, API, or business rule
□ Stakeholder has reviewed and signed off before handing to DEV
```

---

> *"Great BAs ask great questions. AI helps them get extraordinary answers."*
