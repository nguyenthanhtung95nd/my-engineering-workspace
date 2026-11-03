# 05 — Generate

Write the decision, not another `if`. Follow the workspace conventions: `Result<T>`,
`CancellationToken` end to end, primary constructors, sealed classes, XML docs on public members.

## Order of work

**Vocabulary → rules → tests.** Never the reverse. Naming the actions and resources first is
what prevents `document:view` in one place and `doc-view` in another.

## 1. Name the vocabulary

Decide singular/plural, casing, and separators **once**, then never deviate.

```csharp
public static class AccessActions
{
    public const string DocumentView  = "document:view";
    public const string DocumentEdit  = "document:edit";
    public const string DocumentShare = "document:share";   // separate from edit, on purpose
}
```

Every enforcement point names its operation from here — HTTP handler, background worker, message
consumer, agent tool gateway alike. A string literal at a call site is how two spellings of the
same action enter the codebase, and from there two different decisions.

See [`templates/AccessActions.cs`](../templates/AccessActions.cs) for the full version, including
`ControlPlaneActions` and the reason these are constants rather than an enum: action names are
written to decision logs and read by auditors years later, so a renamed enum member would
silently rewrite history. The cost is that the compiler cannot stop an unknown action reaching
the decision service — which is what the deny-by-default fall-through is for.

> **Enumerate actions explicitly.** Never a wildcard action and never an "all" permission. A
> wildcard silently grants every operation added later — including ones that did not exist when
> the rule was reviewed.

## 2. The decision result

Carry **why**, not just the verdict. Without the determining rule, no decision is explainable
and no log is evidence.

```csharp
/// <summary>
/// The outcome of an access evaluation, including the rule that determined it.
/// </summary>
public sealed record AccessDecision
{
    private AccessDecision(bool isAllowed, string determiningRule, string? reason)
    {
        IsAllowed       = isAllowed;
        DeterminingRule = determiningRule;
        Reason          = reason;
    }

    public bool    IsAllowed       { get; }

    /// <summary>Identifier of the rule that produced this outcome. Never empty.</summary>
    public string  DeterminingRule { get; }

    /// <summary>Human-readable explanation, safe to surface to support staff.</summary>
    public string? Reason          { get; }

    /// <summary>Permits the operation under the named rule.</summary>
    public static AccessDecision Allow(string rule)
    {
        // A decision with no rule is not auditable, so it cannot be constructed at all.
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        return new AccessDecision(true, rule, null);
    }

    /// <summary>Denies the operation under the named rule.</summary>
    public static AccessDecision Deny(string rule, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        // Reason is never null on a denial — every refusal can be explained to support staff.
        return new AccessDecision(false, rule, reason ?? "Denied by rule.");
    }

    /// <summary>The default outcome when no rule matched.</summary>
    public static AccessDecision DenyByDefault() =>
        new(false, "deny-by-default", "No rule permitted this operation.");
}
```

See [`templates/AccessDecision.cs`](../templates/AccessDecision.cs) for the full version, which
adds `[MemberNotNullWhen(false, nameof(Reason))]` so the compiler knows `Reason` is populated
whenever `IsAllowed` is false.

## 3. The decision service

Deny by default; denial rules evaluated first and always winning.

```csharp
/// <summary>
/// Evaluates access decisions for documents.
/// </summary>
public sealed class DocumentAccessDecisionService(
    IDocumentRelationshipReader relationships) : IAccessDecisionService<Document>
{
    /// <summary>
    /// Determines whether the query's principal may perform its action on
    /// <paramref name="resource"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="AccessDecision"/> that is always populated with the determining rule.
    /// Absence of a permitting rule yields a denial.
    /// </returns>
    public async Task<AccessDecision> EvaluateAsync(
        AccessQuery query,
        Document resource,
        CancellationToken ct = default)
    {
        var (principal, action, context) = query;

        // ── The trust boundary comes first. Nothing below it means anything across tenants, and
        //    ordering it first stops a cross-tenant attempt being recorded as some lesser
        //    violation — which would keep the most serious signal you have off every metric. ──
        if (resource.TenantId != principal.TenantId)
            return AccessDecision.Deny("guardrail-tenant-isolation");

        if (context.EmploymentStatus is not EmploymentStatus.Active)
            return AccessDecision.Deny("guardrail-employment-not-active");

        if (context.DeviceTrustLevel is not DeviceTrust.Managed)
            return AccessDecision.Deny("guardrail-managed-device-required");

        // Delegatability binds everyone, the owner included. A guardrail placed *after* an
        // allowance is not a guardrail — whoever that allowance covers walks straight past it.
        if (action == AccessActions.DocumentShare && !resource.IsDelegatable)
            return AccessDecision.Deny("share-requires-delegatable-document");

        // ── Allowances, narrowest first. Every allowance enumerates the actions it covers:
        //    there is no "full access" rule, because that is a wildcard with a friendlier name. ──
        if (resource.OwnerId == principal.Id)
            return action switch
            {
                AccessActions.DocumentView or AccessActions.DocumentEdit =>
                    AccessDecision.Allow("owner-view-edit"),
                AccessActions.DocumentShare => AccessDecision.Allow("owner-share"),
                _ => AccessDecision.DenyByDefault()
            };

        if (action is AccessActions.DocumentView or AccessActions.DocumentEdit
            && await relationships.IsManagerOfOwnerAsync(principal.Id, resource.OwnerId, ct))
            return AccessDecision.Allow("manager-of-owner");

        if (action == AccessActions.DocumentView
            && await relationships.IsReaderAsync(principal.Id, resource.Id, ct))
            return AccessDecision.Allow("reader-team-view");

        if (action == AccessActions.DocumentShare)
            return await relationships.IsReaderAsync(principal.Id, resource.Id, ct)
                ? AccessDecision.Allow("delegatable-share")
                : AccessDecision.Deny("share-requires-reader-relationship");

        // Reached only when no rule applied — including when `action` is not in the vocabulary.
        return AccessDecision.DenyByDefault();
    }
}
```

Three things this does **not** do: it does not branch on a `bool` parameter, it does not return
`null`, and it never reads a clock — signal freshness and delegation expiry were settled at the
enforcement point before this method was called, so the decision is a pure function of its inputs
and replays identically when an auditor asks about it a year later.

> ⭐ **There is no `owner-full-access`.** An allowance covering every action is an "all"
> permission, and it silently grants each operation added to the vocabulary later. The owner
> branch enumerates its actions for exactly the reason the vocabulary does.

> **`DenyByDefault()` means "nothing matched".** When a rule ran and rejected the request, say so
> with `Deny("<what-failed>")`. Otherwise the decision log tells a support engineer that no rule
> applied, while a rule is sitting right there having applied.

## 4. Tests — permit and deny, always paired

Access rules fail differently from application code: the dangerous failure is a **silent
allow**, which no happy-path test catches. Every permit test needs its deny twin.

```csharp
public class DocumentAccessDecisionServiceTests
{
    private readonly IDocumentRelationshipReader _relationships =
        Substitute.For<IDocumentRelationshipReader>();
    private readonly DocumentAccessDecisionService _sut;

    public DocumentAccessDecisionServiceTests() =>
        _sut = new DocumentAccessDecisionService(_relationships);

    [Fact]
    public async Task EvaluateAsync_WhenOwnerEdits_Allows()
    {
        // Arrange
        var document = Document(owner: "user-1", tenant: "tenant-a");

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", AccessActions.DocumentEdit), document);

        // Assert
        Assert.True(decision.IsAllowed);
        Assert.Equal("owner-view-edit", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenOwnerSharesNonDelegatableDocument_Denies()
    {
        // Arrange — ownership is the strongest allowance there is. The guardrail must still
        // beat it, or "can use" has quietly become "can grant" for one class of principal.
        var document = Document(owner: "user-1", tenant: "tenant-a", delegatable: false);

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", AccessActions.DocumentShare), document);

        // Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("share-requires-delegatable-document", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenOwnerOnUnmanagedDevice_DeniesViaGuardrail()
    {
        // Arrange — owner would otherwise be allowed; the guardrail must still win.
        var document = Document(owner: "user-1", tenant: "tenant-a");

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", AccessActions.DocumentEdit, device: DeviceTrust.Unmanaged), document);

        // Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("guardrail-managed-device-required", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenOwnerNoLongerEmployed_Denies()
    {
        // Arrange — a leaver keeps owning their documents; they must stop reaching them.
        var document = Document(owner: "user-1", tenant: "tenant-a");

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", AccessActions.DocumentView,
                  employment: EmploymentStatus.Terminated), document);

        // Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("guardrail-employment-not-active", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenResourceBelongsToAnotherTenant_DeniesOnTenantRule()
    {
        // Arrange — also on an unmanaged device, so two guardrails could fire. The tenant rule
        // must be the one recorded: it is the serious one, and alerts are keyed on rule names.
        var document = Document(owner: "user-1", tenant: "tenant-b");

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", AccessActions.DocumentView, device: DeviceTrust.Unmanaged), document);

        // Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("guardrail-tenant-isolation", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenReaderAndDocumentNotDelegatable_DeniesShare()
    {
        // Arrange
        var document = Document(owner: "user-1", tenant: "tenant-a", delegatable: false);
        _relationships.IsReaderAsync("user-2", document.Id, Arg.Any<CancellationToken>())
                      .Returns(true);

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-2", AccessActions.DocumentShare), document);

        // Assert — being able to read is not being able to grant.
        Assert.False(decision.IsAllowed);
        Assert.Equal("share-requires-delegatable-document", decision.DeterminingRule);
    }

    [Fact]
    public async Task EvaluateAsync_WhenActionIsNotInTheVocabulary_DeniesByDefault()
    {
        // Arrange — even for the owner, an unknown action falls through to the default.
        var document = Document(owner: "user-1", tenant: "tenant-a");

        // Act
        var decision = await _sut.EvaluateAsync(
            Query("user-1", "document:exfiltrate"), document);

        // Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("deny-by-default", decision.DeterminingRule);
    }

    // ── Builders. Time is a fixed constant, never DateTimeOffset.UtcNow. ──

    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private static AccessQuery Query(
        string principalId,
        string action,
        DeviceTrust device = DeviceTrust.Managed,
        EmploymentStatus employment = EmploymentStatus.Active) =>
        new(new AccessPrincipal(principalId, "tenant-a", PrincipalKind.User),
            action,
            new DecisionContext
            {
                TenantId         = "tenant-a",
                EmploymentStatus = employment,
                DeviceTrustLevel = device,
                OnCall           = false,
                EvaluatedAt      = Now,
                // Freshness was settled at the enforcement point; by the time a context reaches
                // the decision service its signals are already known to be usable.
                Provenance = new Dictionary<string, SignalProvenance>(StringComparer.Ordinal)
                {
                    [SignalNames.DeviceTrustLevel] =
                        new("endpoint-management", Now.AddMinutes(-1), Verified: true)
                }
            });

    private static Document Document(string owner, string tenant, bool delegatable = true) =>
        new() { Id = "doc-1", OwnerId = owner, TenantId = tenant, IsDelegatable = delegatable };
}
```

> **Assert the determining rule, not just the verdict.** A test that only checks `IsAllowed`
> passes even when the *wrong rule* produced the right answer — and that rule will produce a
> wrong answer somewhere else.

## 5. Lists and retrieval — never evaluate in a loop

`EvaluateAsync` answers for one resource. A list endpoint, an export, or a retrieval pipeline
calls `FilterAsync`, and the implementation loads relationships for the whole candidate set in
**one** round trip.

```csharp
// ❌ N+1 — one relationship query per candidate, on the hottest path in the system.
foreach (var document in candidates)
    if ((await _decisions.EvaluateAsync(query, document, ct)).IsAllowed)
        permitted.Add(document);

// ✅ One decision pass, one relationship round trip.
var permitted = await _decisions.FilterAsync(query, candidates, ct);
```

```csharp
public async Task<IReadOnlyList<Document>> FilterAsync(
    AccessQuery query,
    IReadOnlyList<Document> candidates,
    CancellationToken ct = default)
{
    if (candidates.Count == 0) return [];

    // One query for the whole set, not one per document.
    var readable = await relationships.GetReadableIdsAsync(
        query.Principal.Id, [.. candidates.Select(c => c.Id)], ct);

    return [.. candidates.Where(c => IsPermitted(query, c, readable))];
}
```

> ⚠️ **`FilterAsync` and `EvaluateAsync` must agree.** Two code paths expressing the same rules
> is the fragmentation this skill exists to prevent, now inside one class. Share the predicate,
> and test that both answer identically for the same inputs.

> ⚠️ **For retrieval, order matters.** Authorize candidate identifiers, *then* fetch content —
> see [08-ai-and-agents.md](08-ai-and-agents.md) Part 3.

## Test checklist

For every rule, cover:

- [ ] The permit case
- [ ] The matching deny case
- [ ] A guardrail that **overrides** a would-be permit
- [ ] Cross-tenant access
- [ ] The "can use but cannot grant" boundary — **including for the owner**
- [ ] An action **not** in the enumerated list
- [ ] Two guardrails firing at once — assert the *more serious* one is recorded
- [ ] `FilterAsync` returns the same verdicts as `EvaluateAsync` for the same inputs

At the enforcement point, separately:

- [ ] A missing, unverified, or stale required signal
- [ ] An expired delegation

Use whichever mocking library the project already uses; the structure of these tests does not
change with it.

→ Next: [06-review.md](06-review.md)
