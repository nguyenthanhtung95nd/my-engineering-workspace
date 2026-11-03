# 03 — Enforcement points

The design question is **where the decision is enforced**, not whether rules exist. Rules that
look centralised on paper are routinely bypassed by unprotected routes, background jobs,
message consumers, and service-to-service calls.

## The four roles

| Role | What it does | In .NET |
|---|---|---|
| **Enforcement point** | Intercepts the request, builds the question, enforces the answer | Middleware · `IAuthorizationHandler` · a guard at the top of a service method |
| **Decision point** | Evaluates rules, returns allow/deny | An injected `IAccessDecisionService<TResource>` |
| **Administration point** | Where rules are authored and versioned | The repo + CI |
| **Information point** | Supplies attributes the decision needs | Repositories, HR/CRM APIs, caches, token claims |

The invariant, regardless of deployment:

```
intercept → enrich with context → evaluate → enforce
```

## The four inputs

Every decision is built from four things. Name them explicitly in code — vague parameter bags
are where bugs hide.

- **Principal** — who (or what) is acting
- **Action** — the operation being attempted
- **Resource** — what is being acted upon
- **Context** — everything true at request time: device, time, tenant, risk, delegation

## The enforcement point is the weak link

> **Well-crafted rules are worthless if the question asked does not match the operation performed.**

Three guarantees every enforcement point must meet:

| Guarantee | Meaning |
|---|---|
| **Complete** | Every attribute the rules need is present |
| **Consistent** | Names match the agreed vocabulary — `document:view`, never `doc-view` elsewhere |
| **Scoped** | The right principal, action, and resource, even when several are in play |

## Coverage — the design principle

> **Every protected operation needs a clear enforcement point, a reliable way to build the
> question, and a decision that can be logged, tested, and audited.**

Middleware covers HTTP. It does **not** cover:

- Background workers and hosted services
- Scheduled jobs and timers
- Message and event consumers, whatever the broker
- Service-to-service calls
- Database triggers and migrations touching user data

Enumerate every path that reaches protected data and name its enforcement point. A path without
one is a finding, not a detail.

## ASP.NET Core

Use **resource-based authorization**. Attribute-only authorization cannot see the resource, so
it cannot answer ownership or relationship questions.

```csharp
// Requirement — names the operation, carries no logic
public sealed class DocumentOperationRequirement(string operation) : IAuthorizationRequirement
{
    public string Operation { get; } = operation;
}

// Handler — the enforcement point. It builds the question and enforces the answer;
// the decision itself belongs to the decision service.
public sealed class DocumentAuthorizationHandler(
    IAccessDecisionService<Document> decisions,
    IRequestAuthorityProvider authorities,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DocumentAuthorizationHandler> logger)
    : AuthorizationHandler<DocumentOperationRequirement, Document>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document resource)
    {
        // AuthorizationHandlerContext carries no CancellationToken. Take the request's own
        // rather than hard-coding None and dropping cancellation for the whole call chain.
        var ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        // Resolved once per request and cached in the scope. This handler runs once per
        // *resource* — a page of 50 documents enters it 50 times — while the principal and the
        // signals are properties of the request and do not change between them.
        var authority = await authorities.GetAsync(ct);

        var decision = authority.IsSuccess
            ? await decisions.EvaluateAsync(authority.Value.For(requirement.Operation), resource, ct)
            : AccessDecision.Deny(authority.Error);   // a signal could not be vouched for

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        // Record why, not just that. This is what makes the decision explainable later.
        logger.LogInformation(
            "Access denied: operation {Operation} on document {DocumentId} — rule {Rule}",
            requirement.Operation, resource.Id, decision.DeterminingRule);

        context.Fail(new AuthorizationFailureReason(this, decision.DeterminingRule));
    }
}
```

> ⚠️ **`context.Fail()` is not optional.** Simply not calling `Succeed()` leaves the requirement
> unmet today, but several handlers may serve one requirement: without `Fail()`, a second handler
> can still `Succeed()` and the denial is silently overruled. This is what makes
> *a denial always beats an allowance* actually true rather than merely intended.

> ⚠️ `IHttpContextAccessor` needs `builder.Services.AddHttpContextAccessor()`. Without it
> `HttpContext` is null, the token falls back to `None`, and cancellation is silently lost —
> precisely what the line above is there to prevent.

### Where every time-dependent check lives

The enforcement point reads the clock **once**, builds the context, and vouches for it. Signal
freshness and delegation expiry are settled here, before any rule runs, which is what lets the
decision service stay a pure function of its inputs.

```csharp
/// <summary>
/// Builds the authority for the current request and vouches for its signals.
/// </summary>
/// <remarks>
/// Registered <c>Scoped</c>: resolving it twice in one request must not query the signal
/// sources twice. Requests that authorize resources in parallel need a lock or an
/// <c>AsyncLazy</c> around the cache; sequential resource checks do not.
/// </remarks>
public sealed class RequestAuthorityProvider(
    IDateTimeProvider clock,
    ISignalSource signals,
    ILogger<RequestAuthorityProvider> logger) : IRequestAuthorityProvider
{
    private static readonly (string Name, TimeSpan MaxAge)[] Required =
    [
        (SignalNames.DeviceTrustLevel, SignalFreshness.DeviceTrustLevel),
        (SignalNames.EmploymentStatus, SignalFreshness.EmploymentStatus)
    ];

    private Result<RequestAuthority>? _cached;

    public async ValueTask<Result<RequestAuthority>> GetAsync(CancellationToken ct) =>
        _cached ??= await BuildAsync(ct);

    private async Task<Result<RequestAuthority>> BuildAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;   // the only clock read anywhere on this path

        // Principal and signals come from the validated token and server-side lookups —
        // never from a header, query string, or request body.
        var (principal, context) = await signals.CollectAsync(now, ct);

        foreach (var (name, maxAge) in Required)
        {
            if (!context.IsStale(name, maxAge, now)) continue;

            // The signal may be absent entirely, so never use the dictionary indexer here:
            // the code that makes a denial visible must not itself throw and become a 500.
            var provenance = context.ProvenanceFor(name);
            logger.LogWarning(
                "Signal {Signal} unusable: {State} (source {Source}, retrieved {RetrievedAt})",
                name,
                provenance is null ? "missing" : provenance.Verified ? "stale" : "unverified",
                provenance?.Source, provenance?.RetrievedAt);

            return Result.Failure<RequestAuthority>($"signal-unavailable-{name}");
        }

        if (context.Delegation is { } delegation && now >= delegation.ExpiresAt)
            return Result.Failure<RequestAuthority>("guardrail-delegation-expired");

        return Result.Success(new RequestAuthority(principal, context));
    }
}
```

> ⚠️ **Never measure a signal's age against `DecisionContext.EvaluatedAt`.** The gap between a
> context's own timestamp and the signals it carries is a constant, so a context that is cached,
> queued, or carried into a long-running operation reports every signal as fresh forever. Age is
> measured against a clock read at the moment of use — which is why `IsStale` takes `now`.

Controllers stay humble — they ask, they do not decide:

```csharp
[HttpGet("documents/{id:guid}")]
public async Task<IActionResult> GetDocument(Guid id, CancellationToken ct)
{
    var document = await _documents.GetByIdAsync(id, ct);
    if (document is null) return NotFound();

    var authorized = await _authorization.AuthorizeAsync(
        User, document, new DocumentOperationRequirement(AccessActions.DocumentView));

    // Return NotFound, not Forbid — Forbid confirms the resource exists.
    return authorized.Succeeded
        ? Ok(_mapper.ToResponse(document))
        : NotFound();
}
```

> **On `NotFound` vs `Forbid`:** returning `403` on a resource the caller may not even know
> about leaks its existence. Use `404` unless the caller is already entitled to know the
> resource exists.

## Paths with no middleware

Background workers, scheduled jobs, and message consumers never touch the HTTP pipeline, so
nothing authorizes them unless you do it yourself. The enforcement point goes at the top of the
orchestration service — **after** parsing, **before** any domain work.

The shape below is a message consumer, but it is the same shape for a timer, a worker loop, or
an inbound integration: parse, build the authority for *this* unit of work, decide, enforce.

```csharp
public sealed class OrderMessageProcessor(
    IMessageParser parser,
    IAccessDecisionService<Order> decisions,
    IMessageAuthorityProvider authorities,
    IDeniedMessageStore quarantine,
    IProcessingService processing,
    ILogger<OrderMessageProcessor> logger)
{
    public async Task<ProcessingOutcome> HandleAsync(
        IReadOnlyList<Message> messages, CancellationToken ct)
    {
        var retryable = new List<string>();

        foreach (var message in messages)
        {
            var parsed = parser.Parse(message);
            if (!parsed.IsSuccess)
            {
                // A parse failure is a system fault, not a denial: it must retry and then go to
                // the dead-letter path. Dropping it here discards the message and the evidence.
                logger.LogError("Message {MessageId} failed to parse — {Error}",
                    message.Id, parsed.Error);
                retryable.Add(message.Id);
                continue;
            }

            // Each message is its own unit of work, with its own clock read and its own signal
            // validation. Never reuse a principal, a context, or a decision across messages.
            var authority = await authorities.BuildAsync(parsed.Value.Envelope, ct);

            var decision = authority.IsSuccess
                ? await decisions.EvaluateAsync(
                    authority.Value.For(AccessActions.OrderProcess), parsed.Value.Resource, ct)
                : AccessDecision.Deny(authority.Error);

            if (!decision.IsAllowed)
            {
                logger.LogWarning("Message {MessageId} denied — rule {Rule}",
                    message.Id, decision.DeterminingRule);

                // Deny is final, not retryable — but it is not nothing either. Record it, or a
                // degraded signal provider quietly empties the queue leaving no trace.
                await quarantine.RecordAsync(message, decision.DeterminingRule, ct);
                continue;
            }

            try
            {
                await processing.ProcessAsync(parsed.Value, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Scope the failure to this message. Letting it escape the loop loses the
                // outcome for the entire batch, so messages already processed are redelivered
                // and processed a second time.
                logger.LogError(ex, "Message {MessageId} failed processing", message.Id);
                retryable.Add(message.Id);
            }
        }

        return new ProcessingOutcome(retryable);
    }
}
```

Register the decision service **per unit of work**, never as a singleton holding request state —
whatever container the project uses.

> ⚠️ **Separate the two kinds of "did not process".** A denial is a correct outcome and must not
> retry. A parse or infrastructure failure must. Collapse them and you either retry denials until
> they dead-letter — where a denial becomes indistinguishable from an outage — or silently delete
> messages that were never understood.

> ⚠️ **A denial that deletes a message is a denial with no audit trail.** Since a missing or
> stale signal also denies, one degraded provider can consume an entire queue while emitting
> nothing but log lines. Quarantine denied messages so the volume is visible, and alert on
> denials by *unavailable signal* — never on denials by rule.

## The shared library rule

Every enforcement point must build its question through the **same** library: action naming,
resource identification, tenant scoping, error handling, decision logging.

Without it, each service gradually grows its own version of the model, recreating exactly the
fragmentation the work was meant to remove. This is the single highest-value thing to build
first, and the easiest to skip.

## Never authorize on the caller alone

A service acting on a user's behalf is **not** the same principal as the service. Build the
question from **the authority behind the request**:

```csharp
// ❌ Confused deputy — the dashboard's own identity is the privileged one
var decision = await _decisions.EvaluateAsync(
    new AccessQuery(_serviceIdentity, AccessActions.EmployeeRead, context), department, ct);

// ✅ The initiating user's authority, with the service recorded as context
var decision = await _decisions.EvaluateAsync(
    new AccessQuery(currentUser, AccessActions.EmployeeRead,
                    context with { ActingVia = "hr-dashboard" }),
    department, ct);
```

The fix is **which principal you pass**, not an extra field. If a rule ever grants access because
the call arrived through a trusted service, the confused deputy has simply been rebuilt one layer
up.

## UI enforcement

Hiding a button is **usability, never security**. Every control hidden in the UI must be backed
by the same decision on the server. If the two disagree, the server is right and the UI is a bug.

→ Next: [04-context-and-signals.md](04-context-and-signals.md)
