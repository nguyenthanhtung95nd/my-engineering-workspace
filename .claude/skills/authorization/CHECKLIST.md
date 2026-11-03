# Authorization Audit Checklist

Score each layer. A layer is **green only if every box is ticked** — partial credit hides the
gap that matters. Report the stage, the gaps, and the one rule already causing friction.

---

## Layer 0 — Explainability (score this first)

If this layer is red, nothing below it can be trusted.

- [ ] Pick any access decision from last week. Who was allowed, **why**, which rule decided it,
      who owns it, when it last changed — all answerable in **under five minutes**
- [ ] Decision logs record the **determining rule**, not just allow/deny
- [ ] A support engineer can answer "why can this person see this?" without reading code
- [ ] Logs contain identifiers only — **no PII**

---

## Layer 1 — Coverage

The highest-yield layer. Most real breaches live here.

- [ ] Every path reaching protected data is enumerated: HTTP, background services, scheduled
      jobs, queue consumers, service-to-service calls, admin tooling
- [ ] Each path has a **named** enforcement point
- [ ] Non-HTTP paths are enforced — middleware alone does **not** cover them
- [ ] `AllowAnonymous` usages are enumerated and each is justified
- [ ] Every enforcement point builds its question through the **same shared library**
- [ ] UI gating is backed by an identical server-side decision

---

## Layer 2 — The decision

- [ ] Deny by default — absence of a rule denies
- [ ] Denial rules are evaluated first and always win over allowances
- [ ] Actions are **enumerated explicitly** — no wildcard, no "all" permission
- [ ] **No allowance covers every action** — an `owner-full-access` style rule is a wildcard,
      and it grants each operation added to the vocabulary later
- [ ] The most serious guardrail is ordered first, so its rule name is the one recorded when
      several would fire — tenant isolation before device posture, not after
- [ ] Decision logic reads **no clock**: freshness and expiry are settled at the enforcement point
- [ ] Principal and resource types are constrained — new types cannot silently qualify
- [ ] Every outcome carries a **determining rule** identifier
- [ ] Decisions return `Result<T>`/a decision type — never `null`, never a bare `bool`
- [ ] "Can use" is separate from "can grant"
- [ ] Rules reference **governed, durable** data (team, role, relationship) rather than volatile
      facts (title string, department name, a specific manager's id)

---

## Layer 3 — Authority

- [ ] Decisions use the **authority behind the request**, not the caller's own identity
- [ ] No service acts on its own elevated credentials for a user-initiated operation
- [ ] Tenant id, user id, roles, and "on behalf of" never come from headers or query strings
- [ ] Token scope is treated as the outer perimeter, **not** as the decision
- [ ] Cross-tenant access is blocked by a guardrail, not only by a `Where` clause
- [ ] Relationship traversal has a bounded, deliberate depth

---

## Layer 4 — Context and signals

- [ ] Signals required by decisions are inventoried, each with a named owner
- [ ] Freshness is defined **per signal**, not one global TTL
- [ ] Signal provenance is recorded — source and retrieval time
- [ ] Missing or stale required signals **fail closed**
- [ ] Denials caused by degraded signals are distinguishable from denials by rule — and only the
      former alerts
- [ ] A shared vocabulary exists so the same rule means the same thing everywhere

---

## Layer 5 — Tests

- [ ] Every rule has a permit test **and** a matching deny test
- [ ] Tests assert the **determining rule**, not just the verdict
- [ ] A guardrail overriding a would-be permit is tested
- [ ] Cross-tenant access is tested
- [ ] A missing/stale signal is tested
- [ ] An action outside the enumerated list is tested
- [ ] Tests are deterministic — time injected, never `DateTime.Now`

---

## Layer 6 — Governance

- [ ] Each meaningful rule has a decision record stating **why**
- [ ] Each rule has a named owner, accountable **after** merge
- [ ] Reviewer ≠ author, enforced by tooling rather than convention
- [ ] A review cadence exists (quarterly is enough to start)
- [ ] Rules with no traffic are investigated, not ignored
- [ ] Repeated manual exceptions are treated as a **diagnostic**, not a nuisance
- [ ] Audit artefacts are produced by normal work, not assembled for audits

---

## Layer 7 — AI and agents

Skip only if neither applies.

**Using AI on access rules**
- [ ] AI never makes or explains decisions authoritatively
- [ ] AI explanations are compared against real decision traces
- [ ] The five guardrails are attached to every session
- [ ] Humans apply changes manually
- [ ] Access rules never live in a prompt

**Agents acting in the system**
- [ ] The enforcement point sits **between** the agent and its tools, outside the model
- [ ] Denials are returned as structured input the agent can replan on
- [ ] Control-plane actions are denied to agents outright
- [ ] Delegation is structured data evaluated at decision time
- [ ] A sub-agent without delegation has **no** authority — none is inherited
- [ ] Delegation narrows along **all three** dimensions — delegate, action, **and resource scope**
- [ ] Delegation expiry is enforced at the enforcement point with an injected clock
- [ ] What a denial tells the agent is classified — internal rule names are not returned by
      default, because a rule like tenant isolation confirms the resource exists

**Retrieval / RAG**
- [ ] Authorization happens **before** retrieval
- [ ] Search returns identifiers and scores — **never** protected content
- [ ] No reliance on instructions or post-hoc redaction

---

## Scoring

**Layers 0 and 1 are gates, not points.** If either is red, the stage is **Local** no matter how
many other layers are green. A system with thorough tests, governance, and agent controls but no
enforcement coverage is not advanced — it is unprotected with good paperwork.

Only once Layers 0 **and** 1 are green does the count of the remaining six mean anything:

| Layer 0 + 1 | Of Layers 2–7, green | Stage | Next move |
|---|---|---|---|
| Either red | — | **Local** | Make decisions observable, then close the coverage gaps. Nothing else pays off before that |
| Both green | 0–2 | **Local → Coordinated** | Externalise the one rule already causing friction; unify the vocabulary |
| Both green | 3–5 | **Coordinated** | Signal ownership, delegation as a capability, leadership visibility |
| Both green | All 6 | **Strategic** | Maintain the cadence; watch for drift |

> Most systems are **Local**. That is normal and not a failure — it only becomes a problem when
> the decision surface grows faster than the ability to explain it.
