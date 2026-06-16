# Role: SA — Solution Architect

> Responsible for system design, technology evaluation, architectural decisions, and ADRs.
> Works at the beginning of every feature and whenever a major technical decision must be made.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `grill-me` | Surface all constraints and unknowns before committing to a direction |
| `grill-with-docs` | Build shared domain language (CONTEXT.md) + record key decisions as ADRs |
| `write-a-prd` | Document the architectural decision and the options considered |
| `/architect` | Trade-off analysis, right-sized recommendation, ADR generation |
| `/compliance` | Verify security and regulatory requirements upfront |
| `improve-codebase-architecture` | Find deepening opportunities — generates HTML report with before/after diagrams |

---

## Workflow

```
1. grill-me          → Clarify scale, constraints, team capability, timeline
2. write-a-prd       → Document problem statement, options, boundaries
3. /architect        → Trade-off table, recommendation, confidence level
4. Chain-of-Verification → "Apply CoV to your recommendation"
5. ADR               → Commit decision record to repository
```

---

## Real-World Example

**Scenario:** Build a Product CRUD Lambda triggered by API Gateway, MySQL database,
source code on GitLab, infrastructure deployed via AWS CDK.

---

### Step 1 — grill-me: Surface the unknowns

You say:
> "I need to build a CRUD Lambda for products, triggered by API Gateway, using MySQL.
> Source on GitLab, CDK for infra."

Claude asks one question at a time:

**Q1:** Should all four CRUD operations live in a single Lambda function or be split
into separate Lambdas per operation?
*My recommendation: single Lambda with method-based routing via API Gateway proxy
integration — simpler CDK stack, one deployment unit, easier to reason about at
this scale. Split only if operations have very different scaling or memory requirements.*

**Q2:** Does the API require authentication? If so, Cognito User Pool Authorizer
or Lambda Authorizer?
*My recommendation: Cognito User Pool Authorizer via API Gateway — no custom code
to maintain, integrates natively with CDK.*

**Q3:** Should the MySQL connection use RDS Proxy or direct connection?
*My recommendation: RDS Proxy — Lambda cold starts create connection spikes
that overwhelm direct MySQL connections under concurrent invocations.*

**Q4:** Is this a greenfield project or extending an existing CDK stack?
*My recommendation clarification needed — the answer determines whether we create
a new CDK App or add a Stack to the existing one.*

---

### Step 2 — /architect: Trade-off analysis

After grill-me resolves all branches:

```
/architect
"Design the infrastructure architecture for a Product CRUD Lambda:
 - API Gateway (REST) → Lambda (.NET 8)
 - RDS MySQL via RDS Proxy
 - Cognito User Pool Authorizer
 - GitLab CI/CD pipeline
 - CDK deployment
 Constraints: team of 2, 3-week timeline, < 500 requests/day at launch."
```

Claude produces:

**Option A — Single Lambda, REST API Gateway**
```
API Gateway (REST)
  → Cognito Authorizer
  → ProductFunction (Lambda .NET 8)
      → RDS Proxy
          → RDS MySQL (t3.micro)

CDK Stack:
  ProductApiStack
    ├── RestApi + CognitoAuthorizer
    ├── ProductFunction (Lambda)
    ├── RdsProxy
    └── RdsInstance
```
*Fits well when: < 1000 req/day, team < 5, single domain*

**Option B — HTTP API Gateway (lower cost)**
```
API Gateway (HTTP)
  → JWT Authorizer (Cognito)
  → ProductFunction (Lambda .NET 8)
```
*Fits well when: cost sensitivity matters, no need for usage plans or WAF*

**Recommendation: Option B** — HTTP API is 70% cheaper than REST API at this scale,
and JWT Authorizer covers the auth requirement. REST API is only justified when you
need API keys, usage plans, or request/response transformation.

**Chain-of-Verification:**
- Assumption: < 500 req/day → correct for HTTP API; re-evaluate at 10K/day
- Assumption: No WAF required → confirm with security team before launch
- Confidence: 85% — drops if compliance requires REST API for WAF integration

---

### Step 3 — ADR output

```markdown
# ADR-001: API Gateway Type — HTTP API vs REST API

Date: 2026-04-23
Status: Accepted

## Context
Building a Product CRUD Lambda. Need to choose between HTTP API (v2) and REST API (v1).

## Decision
Use HTTP API (v2) with JWT Authorizer backed by Cognito User Pool.

## Options Considered
| Option | Cost | Features | Complexity |
|--------|------|----------|------------|
| HTTP API v2 | ~$1/million | JWT auth, Lambda proxy | Low |
| REST API v1 | ~$3.50/million | Full feature set | Medium |

## Rationale
At < 500 req/day, cost difference is negligible in absolute terms but HTTP API
establishes a cheaper baseline. No WAF, usage plans, or request transforms required.
JWT Authorizer natively supports Cognito without custom code.

## Consequences
Positive: lower cost, simpler CDK construct, faster cold start (smaller payload)
Negative: no request/response transformation, no API keys natively
Risk: if WAF becomes a requirement, migration to REST API is non-trivial

## Review Date
Revisit when monthly requests exceed 1 million or WAF is required.
```

---

## SA Checklist

```
□ grill-me completed — no open design questions
□ Scale, team size, and timeline constraints documented
□ At least 2 options evaluated with trade-offs
□ Chain-of-Verification applied — assumptions and confidence stated
□ ADR committed to repository
□ /compliance run — security requirements verified upfront
```
