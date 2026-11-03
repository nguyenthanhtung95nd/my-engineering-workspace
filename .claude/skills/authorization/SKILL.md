---
name: authorization
description: >
  Diagnoses, designs, generates, and reviews authorization in .NET systems — web APIs,
  background workers, message consumers, and serverless handlers alike. Answers whether
  the current access-control model still fits, where enforcement belongs so nothing is
  bypassed, and how to express access rules so they can be tested, explained, and
  audited. Use when the user mentions authorization, access control, permissions, roles,
  RBAC, ABAC, ReBAC, policy, multi-tenancy, IDOR, "who can access", delegation, or
  authorizing AI agents — and when writing or reviewing any endpoint, handler, or
  query that returns data belonging to someone.
context: auto
patterns:
  - "**/*Authorization*.cs"
  - "**/*Permission*.cs"
  - "**/*Policy*.cs"
  - "**/*Tenant*.cs"
  - "**/*AccessDecision*.cs"
---

# Authorization

Authentication asks *who are you*. Authorization asks *what can you do* — and that is where
systems actually fail. The goal is **not** to swap one access-control technique for another;
it is to **change where authorization sits in the system** so a decision can be made
consistently, enforced everywhere, and explained afterwards.

**The one diagnostic question.** Pick any access decision this system made. Can you say, in
under five minutes: who was allowed, *why*, which rule decided it, who owns that rule, and when
it last changed? If not, the model has already been outgrown — regardless of whether anything
is broken yet.

## Step 0 — Detect, then pick a mode
Inspect first, and take the project as you find it: its host model, its data access, its DI
container, its mocking library. Then establish how access is decided today (`[Authorize]`, role
strings, ownership checks inside queries, hand-rolled `if`), whether paths exist that no
middleware covers (background workers, message consumers, scheduled jobs, service-to-service
calls), and whether tenancy is a real boundary or just a column.

The code in this skill is **illustration, not a template to transplant**. Match the conventions
already in the repository.

- **Diagnose** (default) → score with [CHECKLIST.md](CHECKLIST.md), name the stage, report gaps.
- **Design** → choose the model and place the enforcement points.
- **Generate** → write the decision code + tests.
- **Review** → audit existing authorization code for bypasses and over-permission.

Show the plan and get confirmation before writing files.

## The spine, in order
Each step must be green before the next buys you anything.

1. **Diagnose** — can you explain *why*? Find where decisions are made today; count how many
   places. See [playbook/01-diagnose.md](playbook/01-diagnose.md).
2. **Model** — ownership check → RBAC → ReBAC → ABAC → policy-based. Each has a named failure
   that tells you it is time to move. See [playbook/02-model-selection.md](playbook/02-model-selection.md).
3. **Enforcement points** — map PEP/PDP/PAP/PIP onto middleware, handlers, services, workers.
   **Coverage is the design principle**: every protected operation needs one. See
   [playbook/03-enforcement-points.md](playbook/03-enforcement-points.md).
4. **Context & signals** — what the decision needs, where it comes from, how fresh it must be,
   and how to prove it was not tampered with. See [playbook/04-context-and-signals.md](playbook/04-context-and-signals.md).
5. **Generate** — requirements, handlers, `Result<T>`, `CancellationToken`, and tests that
   assert **both permit and deny**. See [playbook/05-generate.md](playbook/05-generate.md).
6. **Review** — confused deputy, IDOR, coverage gaps, UI-shaped permissions, silent widening.
   See [playbook/06-review.md](playbook/06-review.md).
7. **Governance** — ownership, ADRs, review cadence, retirement. Policies accumulate technical
   debt exactly like code. See [playbook/07-governance.md](playbook/07-governance.md).
8. **AI & agents** — how to use an AI assistant on access rules safely, and how to authorize
   an agent that plans its own actions. See [playbook/08-ai-and-agents.md](playbook/08-ai-and-agents.md).

## Non-negotiables

They live in [`rules/authorization.md`](../../rules/authorization.md), which is always active —
one copy, so the two cannot drift. Read them there before generating or reviewing anything.

## Output
Either a diagnosis naming the stage with prioritized gaps; or a model decision with its stated
trade-offs and an ADR; or decision code with passing permit **and** deny tests; or a review
with concrete attack paths and corrected code. Always finish by stating what changed and how to
verify it.
