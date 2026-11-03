# 06 — Review

Review authorization adversarially. For every finding, give a **concrete attack path** — who
does what, in what order, to get access they should not have. A warning without an attack path
is not a finding.

## Order of attack

Work outside in. The first two find most real issues.

### 1. Coverage — what is not protected at all

The highest-yield check, and the one most often skipped. Enumerate every path that reaches
protected data:

```bash
rg -n 'BackgroundService|IHostedService|Consumer|Worker|Scheduled|Timer' --type cs
rg -n 'HttpClient|RestClient|InvokeAsync' --type cs      # service-to-service
rg -n 'AllowAnonymous' --type cs
```

For each, name its enforcement point. **No enforcement point is the finding.**

> *Attack path template:* "A message published to queue X is processed by consumer Y, which
> reads records for any tenant. Any producer with queue access reads the whole table."

### 2. IDOR — identity checked, authority not

```csharp
// ❌ Authenticated, but not authorized — any logged-in user reads any order
[HttpGet("orders/{orderId:guid}")]
public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
{
    var order = await _db.Orders.FindAsync([orderId], ct);
    return Ok(order);
}
```

Attack path: authenticate as any user, increment or guess `orderId`, read other customers'
orders.

```csharp
// ✅ Authority established before the resource is returned
[HttpGet("orders/{orderId:guid}")]
public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
{
    var order = await _orders.GetByIdAsync(orderId, ct);
    if (order is null) return NotFound();

    var decision = await _decisions.EvaluateAsync(
        new AccessQuery(CurrentPrincipal, AccessActions.OrderView, context), order, ct);

    if (!decision.IsAllowed)
    {
        _logger.LogInformation("Denied order:view on {OrderId} — rule {Rule}",
            orderId, decision.DeterminingRule);
        return NotFound();   // do not confirm existence
    }

    return Ok(_mapper.ToResponse(order));
}
```

### 3. Confused deputy — the system's own authority used on a caller's behalf

```csharp
// ❌ The job runs with elevated rights and passes the caller's input straight through
public async Task ExportAsync(string departmentId, CancellationToken ct)
{
    var rows = await _hr.GetEmployeesAsync(departmentId, ct);   // service credentials
    await _storage.WriteAsync(rows, ct);
}
```

Attack path: a low-privileged user triggers the export for a department they cannot read; the
service's own credentials fetch it; the result lands somewhere they can read.

Fix: carry the **initiating principal** through and authorize on *their* authority, with the
service recorded as context.

### 4. Silent widening

| Pattern | Why it is dangerous |
|---|---|
| Wildcard or "all" actions | Every operation added later is granted retroactively |
| Unconstrained principal or resource types | New types silently become eligible |
| A denial rule used to patch a too-broad allowance | The allowance is still wrong; the patch will be forgotten |
| Permission names mirroring UI controls | The interface shape becomes the security boundary |
| Relationship traversal with no depth limit | A long chain grants access nobody modelled |

### 5. Over-trusted input

```bash
# Values taken from the client and used in decisions
rg -n 'Request\.(Headers|Query)\[' --type cs
rg -n 'FromHeader|FromQuery' --type cs
```

Tenant id, role, user id, or "on behalf of" taken from a header or query string and trusted is
a critical finding. These must come from a verified token or a server-side lookup.

### 6. Token scope mistaken for authorization

A valid token with the right scope says the client was **delegated** a class of access. It does
not say this principal may touch **this** resource. Scopes are the outer perimeter; the decision
is still required.

```csharp
// ❌ Scope treated as the decision — and an unbounded read of every tenant's orders
if (User.HasClaim("scope", "orders.read")) return Ok(await _db.Orders.ToListAsync(ct));

// ✅ Scope narrows the perimeter; the decision still runs on the resources
if (!HasScope(User, "orders.read")) return Forbid();

var page = await _orders.GetPageAsync(request.Page, request.Size, ct);
var permitted = await _decisions.FilterAsync(
    new AccessQuery(CurrentPrincipal, AccessActions.OrderView, context), page, ct);

return Ok(permitted.Select(_mapper.ToResponse));
```

Two traps inside that fix:

```csharp
// ❌ A JWT usually carries ONE space-delimited "scope" claim, so an exact-value match fails
//    for a perfectly valid token of "orders.read orders.write".
User.HasClaim("scope", "orders.read")

// ✅ Split it, or use the scope-aware policy your auth library already provides.
private static bool HasScope(ClaimsPrincipal user, string scope) =>
    user.FindAll("scope")
        .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(scope, StringComparer.Ordinal);
```

> **On `Forbid()` here versus `NotFound()` elsewhere:** a scope denial is about the *client's*
> delegated authority, not about any particular resource, so `403` discloses nothing — the caller
> never named a resource. A **resource** denial is different: `403` there confirms the thing
> exists. Scope → `403`. Resource → `404`.

> ⚠️ **A list endpoint is the most commonly missed authorization surface.** Per-row
> `EvaluateAsync` in a loop is an N+1 against the information point on your busiest route — use
> `FilterAsync`, and page the query before authorizing rather than after.

### 7. Explainability

For each decision point, ask: if a customer disputes this tomorrow, can we show **what** was
decided and **why**? If the log records only `403`, the answer is no.

## Review checklist

**Intent**
- [ ] Each rule is explainable in one or two plain sentences
- [ ] It does exactly what was intended and nothing more
- [ ] Another reviewer would reach the same reading

**Scope**
- [ ] The smallest set of principals, actions, and resources
- [ ] No new condition or relationship grants unintended access
- [ ] No surprising interaction with existing rules

**Structure**
- [ ] Guardrail vs. narrowed-allowance vs. carve-out was a deliberate choice
- [ ] Reads clearly to someone who was not involved

**Behaviour**
- [ ] Tests exist for **both** permit and deny
- [ ] Tests assert the **determining rule**, not only the verdict
- [ ] The result matches what you would say during an audit

**Accountability**
- [ ] The rule has a named owner
- [ ] A decision record explains why it exists
- [ ] **If you cannot confidently defend it, do not approve it**

## Output format

```
Authorization Review — <scope>
Stage: <Local | Coordinated | Strategic>

🔴 CRITICAL
   Finding:     <specific weakness>
   Attack path: <ordered steps an attacker takes>
   Rule:        <which rule should have decided this, and why it did not>
   Fix:         <corrected code>

🟠 HIGH / 🟡 MEDIUM …

Coverage gaps: <paths with no enforcement point>
Unexplainable decisions: <points that cannot answer "why">
```

→ Next: [07-governance.md](07-governance.md)
