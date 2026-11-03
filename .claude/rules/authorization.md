# Rules: Authorization

Authentication asks *who are you*. Authorization asks *what can you do* — and that is where
systems actually fail. These rules apply to **every** endpoint, handler, consumer, and job that
touches data belonging to someone.

> **The diagnostic question.** For any access decision: can you say who was allowed, **why**,
> which rule decided it, and who owns that rule? If not, the model has been outgrown.

## Non-negotiables

- **Deny by default.** Absence of a rule is a denial, never an allowance.
- **A denial rule always beats an allowance rule.** Layering is only safe if this holds.
- **Fail closed.** A missing or stale signal denies. Log it distinctly so a degraded signal is
  never mistaken for a normal denial.
- **Never authorize on the caller's identity alone.** Authorize on the *authority behind the
  request*. A service acting for a user is not the same principal as the service.
- **Enumerate actions explicitly.** No wildcard actions, no "all" permission — they silently
  grant every operation added later.
- **"Can use" is not "can grant."** Model them as separate permissions.

```csharp
// Bad — authenticated, but authority never established (IDOR)
[HttpGet("orders/{orderId:guid}")]
public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
{
    var order = await _db.Orders.FindAsync([orderId], ct);
    return Ok(order);
}

// Good — decision made against the resource, denial returns NotFound
[HttpGet("orders/{orderId:guid}")]
public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
{
    var order = await _orders.GetByIdAsync(orderId, ct);
    if (order is null) return NotFound();

    var decision = await _decisions.EvaluateAsync(
        new AccessQuery(CurrentPrincipal, AccessActions.OrderView, context), order, ct);

    return decision.IsAllowed ? Ok(_mapper.ToResponse(order)) : NotFound();
}
```

> Return `NotFound`, not `Forbid`, unless the caller is already entitled to know the resource
> exists — `403` confirms existence.

## Coverage

**Every protected operation needs an enforcement point.** Middleware covers HTTP only. It does
**not** cover background workers, scheduled jobs, message consumers, or service-to-service
calls. A path without an enforcement point is a bug, not a detail.

All enforcement points must build the decision through the **same shared library** — action
naming, resource identification, tenant scoping, error handling, decision logging. Without it
each service grows its own model and recreates the fragmentation the work was meant to remove.

## Decisions carry their reason

- Return a decision type that names the **determining rule** — never a bare `bool`, never `null`.
- Follow `error-handling.md`: `Result<T>` at service boundaries.
- Propagate `CancellationToken` end to end (`async.md`).
- Log every decision with the determining rule. **Identifiers only — no PII** (`checklist.md`).

## Data the rules depend on

Reference **governed, durable** entities — team, role, relationship, ownership. Not volatile
facts: a job-title string, a department name, or a specific manager's id. A rule keyed on
`Department == "Finance"` breaks silently during a reorg.

## Tenancy

A `TenantId` column is a **database convention**, not a trust boundary. It becomes one only
when a missing filter **cannot** return data. Enforce tenant isolation as a guardrail evaluated
before any allowance, not only as a `Where` clause.

## Never trust the client for authority

Tenant id, user id, roles, and "on behalf of" must come from a verified token or server-side
lookup — never from a header, query string, or request body.

Token scope is the **outer perimeter**, not the decision. A valid scope says the client was
delegated a *class* of access; it does not say this principal may touch *this* resource.

## Tests

- Every rule needs a **permit test and a matching deny test**. The dangerous failure is a silent
  allow, which no happy-path test catches.
- Assert the **determining rule**, not just the verdict — the right answer from the wrong rule
  will be the wrong answer somewhere else.
- Always test: a guardrail overriding a would-be permit — **including over the strongest
  allowance you have** · cross-tenant access · a missing signal · an action outside the
  vocabulary.
- Inject time (`IDateTimeProvider`); never `DateTime.Now` (`testing.md`).

## Time

All time-dependent checks — signal freshness, delegation expiry — belong at the enforcement
point, which reads the clock **once** per unit of work. Decision logic reads no clock, so a
decision is reproducible from its inputs alone. Never measure a signal's age against the
timestamp of the context carrying it: that gap is a constant, so a cached or queued context
reports every signal as fresh forever.

## AI and agents

- **Access rules never live in a prompt.** A prompt is not an enforcement point.
- The enforcement point sits **between the agent and its tools**, outside the model. Return
  denials as structured input the agent can replan on — but only the part that is safe to put
  in a model's context.
- **Delegation is data that narrows authority, never widens it**, along all three dimensions:
  who, what, and which resource. A sub-agent without a delegation record has no authority at all.
- **Authorize before retrieval, never after generation.**

## Everything else

Model selection and its named failure modes, enforcement-point placement, signal design,
code generation, adversarial review, and governance live in the `authorization` skill — along
with the audit CHECKLIST and the reference types. This file holds only what must be true on
every change, so that it stays small enough to be always active.
