---
name: grill-me
description: >
  Pre-processing step that sharpens a vague idea into a shared understanding
  before writing a PRD. Interviews the user relentlessly one question at a time,
  provides a recommended answer for each question, and explores the codebase
  instead of asking when the answer is already there. Use when the user has a
  vague idea they want to harden, wants to stress-test a design, or mentions
  "grill me". Sits before write-a-prd in the pipeline:
  grill-me → write-a-prd → prd-to-plan → do-work.
---

# Grill Me

Interview the user relentlessly about every aspect of their plan or design
until reaching a shared understanding. Walk down each branch of the decision
tree, resolving dependencies between decisions one at a time.

## Rules

- Ask **one question at a time** — never batch multiple questions together.
- For each question, **provide your own recommended answer** before the user
  responds. This forces a concrete position and makes the conversation productive.
- If a question can be answered by **exploring the codebase**, do that instead
  of asking. Read relevant files first.
- Do not move to the next branch until the current decision is resolved.
- Continue until all branches are exhausted and there are no open questions.

## Example Conversation

> **You:** I want to add rate limiting to the API so tenants can't abuse it.
>
> **Claude:** First question — where should the limit be enforced?
> My recommendation: as ASP.NET Core middleware using `Microsoft.AspNetCore.RateLimiting`
> with a sliding window partitioned by TenantId, since you already have TenantContext
> in the pipeline. Does that fit, or do you prefer a Redis-backed solution for
> multi-instance resilience?
>
> **You:** Redis-backed — we run 3 pods on AKS.
>
> **Claude:** Next — should limits be uniform across all tenants, or configurable per tenant?
> My recommendation: configurable via a `TenantRateLimitSettings` table, with a sensible
> default in `appsettings.json` for tenants without an override. Agree?

When all branches are resolved, offer to run `/write-a-prd`.
