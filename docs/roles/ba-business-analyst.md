# Role: BA — Business Analyst

> Responsible for clarifying requirements, writing user stories, defining
> acceptance criteria, and ensuring developers can implement without ambiguity.
> Works between the stakeholder and the development team.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `grill-me` | Surface business ambiguities and edge cases before writing the spec |
| `write-a-prd` | Produce a structured PRD with user stories and acceptance criteria |
| `/architect` | Validate technical feasibility of business requirements |
| `/workflow Trigger A` | Identify technical unknowns in a given user story |

---

## Workflow

```
1. grill-me          → Interview stakeholder requirements one question at a time
2. write-a-prd       → Structured PRD: problem, user stories, AC, out of scope
3. /architect        → Technical feasibility check — flag constraints early
4. Hand to DEV       → PRD is complete enough to implement without clarification
```

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
Viewer (read-only). This affects the Cognito User Pool group setup.*

**Q3:** When a product is "deleted", is it a hard delete or soft delete?
*My recommendation: soft delete — set `IsActive = false`. Hard delete loses
audit history and breaks foreign key references in order history.*

**Q4:** Are there validation rules on Price or StockCount?
*My recommendation: Price ≥ 0, StockCount ≥ 0. Clarify whether Price = 0
is valid (free products) or should be rejected.*

**Q5:** Should the list endpoint support filtering or pagination?
*My recommendation: yes to pagination (default 20, max 100) from day one —
retrofitting is expensive. Filtering by IsActive at minimum.*

---

### Step 2 — write-a-prd output

```markdown
## Problem Statement
The operations team needs to manage the product catalogue through an API.
Currently products are managed directly in the database, which is error-prone
and not accessible to non-technical staff.

## Solution
A REST API (API Gateway + Lambda) that exposes full CRUD operations on products,
secured by role-based access via Cognito.

## User Stories

1. As an **Admin**, I want to create a product with name, description, price,
   and stock count, so that new products appear in the catalogue.

2. As an **Admin**, I want to update any field of an existing product,
   so that I can correct mistakes or update pricing.

3. As an **Admin**, I want to soft-delete a product (set IsActive = false),
   so that it is hidden from the catalogue without losing its history.

4. As a **Viewer**, I want to retrieve a single product by ID,
   so that I can inspect its current details.

5. As a **Viewer**, I want to list all active products with pagination,
   so that I can browse the catalogue without loading everything at once.

6. As the **system**, when a create or update request contains invalid data
   (negative price, empty name), I want to return HTTP 422 with field-level
   errors, so that the caller knows exactly what to fix.

## Acceptance Criteria

### Story 1 — Create product
- POST /products with valid body → HTTP 201, returns created product with Id
- POST /products with missing Name → HTTP 422, error identifies the field
- POST /products with Price < 0 → HTTP 422
- POST /products without Admin role → HTTP 403

### Story 3 — Soft delete
- DELETE /products/{id} → sets IsActive = false, returns HTTP 204
- DELETE /products/{id} for non-existent product → HTTP 404
- Deleted product does NOT appear in GET /products list

### Story 5 — List with pagination
- GET /products?page=1&pageSize=20 → returns first 20 active products
- GET /products?pageSize=101 → HTTP 422 (exceeds max)
- Response includes: items[], totalCount, page, pageSize, hasNextPage

## Implementation Decisions
- Soft delete only — IsActive flag, no physical row removal
- Roles: Admin (full CRUD), Viewer (GET only) — enforced via Cognito groups
- Pagination: cursor-based not required at this scale; page/pageSize sufficient
- Price validation: 0 is valid (free products), negative is not

## Out of Scope
- Product images or media attachments
- Product categories or tagging
- Inventory reservation or stock locking
- Audit log of who changed what (tracked in future sprint)

## Further Notes
- "Viewer" group in Cognito must be created as part of CDK stack
- Lambda should return consistent error envelope: { error: string, field?: string }
```

---

### Step 3 — /architect: Feasibility check

```
/architect
"Is the PRD above technically feasible within 3 weeks with 1 developer?
 Flag any requirement that adds unexpected complexity."
```

Claude flags:
- Pagination is trivial with Dapper — no concern
- Cognito group-based role check requires reading group claims from JWT — one-time setup, not complex
- Soft delete with IsActive filter needs a composite index on (IsActive, CreatedAt) — add to schema

---

## BA Checklist

```
□ grill-me completed — all business ambiguities resolved
□ PRD has user stories for ALL actors (Admin, Viewer, System)
□ Acceptance criteria are testable — QA can write test cases directly from them
□ Edge cases documented: invalid input, missing resources, unauthorized access
□ Out-of-scope section prevents scope creep
□ /architect feasibility check done — no technical surprises for DEV
□ PRD saved to prd/{feature}-prd.md
```
