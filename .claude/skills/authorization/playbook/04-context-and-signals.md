# 04 — Context and signals

A decision is only as trustworthy as its inputs. Context must be designed deliberately along
three axes: **which signals matter**, **how they are delivered**, and **how fresh they must be**.

## Signal inventory

Start by listing what the decisions actually need. Typical sources:

| Signal | Example | Usual source |
|---|---|---|
| Identity & role | user id, team membership | Token claims, directory |
| Account status | active, suspended, entitlement tier | Billing/HR system |
| Relationship | owner, member, manager-of | Application database |
| Device posture | managed, patched, encrypted | Endpoint management |
| Operational status | on-call, change-ticket open | PagerDuty, ITSM |
| Environmental | time, network, geolocation, risk score | Request, fraud service |

## Delivery: token vs live query

| Model | Benefit | Risk |
|---|---|---|
| **Token-embedded** | Fast; no extra call; signed | Goes stale; replay window; token bloat |
| **Live query** | Always current | Latency; upstream becomes a dependency of every decision |
| **Hybrid** | Stable facts in the token, volatile facts queried | Requires discipline about which is which |

Hybrid is usually right: a small token (identifier plus coarse scopes) with volatile signals
fetched or pushed.

## Freshness is per signal

Attributes have wildly different lifetimes. One global TTL is always wrong for most of them.

```csharp
public static class SignalFreshness
{
    // On-call rotations change constantly; a stale value grants real privilege.
    public static readonly TimeSpan OnCall           = TimeSpan.FromMinutes(5);

    // Device posture can change mid-session.
    public static readonly TimeSpan DeviceTrustLevel = TimeSpan.FromMinutes(15);

    // Employment status changes rarely and is confirmed through other channels.
    public static readonly TimeSpan EmploymentStatus = TimeSpan.FromHours(24);
}
```

Each lifetime is paired with a key from `SignalNames`, never a string literal:

```csharp
// Good — a typo here is a compile error.
context.IsStale(SignalNames.DeviceTrustLevel, SignalFreshness.DeviceTrustLevel, now)

// Bad — a typo makes the signal look missing, which fails closed silently and permanently,
// and the symptom is indistinguishable from a genuinely degraded upstream.
context.IsStale("deviceTrust", SignalFreshness.DeviceTrustLevel, now)
```

> **Propagate change; do not merely wait for expiry.** Where a signal matters, push revocation
> and status changes rather than letting a TTL run down.

## Caching, and where it hurts

| Cache location | Acceptable for | Risk |
|---|---|---|
| At the decision point | Short-lived, per request | Low |
| At the information source | Stable facts | **Increases the blast radius of staleness** — every consumer is wrong at once |
| At the edge / client | Low-risk data only | Hardest to invalidate |

## Normalisation — the part that is always skipped

Without a shared vocabulary, the *same rule text* produces *different decisions* in different
services, because `employmentStatus == "active"` means something subtly different in each.

Define the vocabulary once and share it as a library, not a service. The shape — see
[`templates/DecisionContext.cs`](../templates/DecisionContext.cs) for the full version:

```csharp
/// <summary>
/// The normalised signal vocabulary. Every decision point reads these names and no others.
/// Adding a field here is an architectural decision, not a convenience.
/// </summary>
public sealed record DecisionContext
{
    public required string           TenantId         { get; init; }
    public required EmploymentStatus EmploymentStatus { get; init; }
    public required DeviceTrust      DeviceTrustLevel { get; init; }
    public required bool             OnCall           { get; init; }
    public required DateTimeOffset   EvaluatedAt      { get; init; }

    public DelegationRecord? Delegation { get; init; }

    /// <summary>Where each signal came from and when — required for explainability.</summary>
    public required IReadOnlyDictionary<string, SignalProvenance> Provenance { get; init; }
}

public sealed record SignalProvenance(string Source, DateTimeOffset RetrievedAt, bool Verified);
```

Statuses are **enums, not strings**. A decision must not turn on `"Active"` versus `"active"`,
and both enums order their members so `default` is the least privileged value — an unset field
then grants nothing rather than everything.

> Normalisation does not make rules smarter. **It makes their inputs trustworthy.**

## Provenance, not blind trust

The decision point must always be able to show **where each signal came from** and that it
**arrived unaltered**. Internally, mutual TLS. Where that is impractical — SaaS callbacks,
browsers, bearer-token APIs — use signed payloads and verify them.

A signal with no provenance is a rumour. Treat it as missing. **A signal whose integrity was
never confirmed is a rumour too** — `IsStale` rejects an unverified value however recent it is,
because a freshly forged value is still forged.

## Fail closed

When a required signal is missing, unverified, or stale, the decision is **deny**. This is
correct behaviour, not an outage — but it must be *visible*, or it will be mistaken for one.

This check belongs at the **enforcement point**, alongside the clock read that makes it
meaningful — see [03-enforcement-points.md](03-enforcement-points.md). The decision service never
performs it, which is what keeps decision logic a pure function of its inputs.

```csharp
var now = clock.UtcNow;   // the enforcement point's clock, read once

if (context.IsStale(SignalNames.DeviceTrustLevel, SignalFreshness.DeviceTrustLevel, now))
{
    // The signal may be absent entirely, so never use the dictionary indexer here. The code
    // that makes a denial visible must not itself throw and turn a deny into a 500.
    var provenance = context.ProvenanceFor(SignalNames.DeviceTrustLevel);

    logger.LogWarning(
        "Denying {Action}: device-trust signal {State} (source {Source}, retrieved {RetrievedAt})",
        action,
        provenance is null ? "missing" : provenance.Verified ? "stale" : "unverified",
        provenance?.Source, provenance?.RetrievedAt);

    return AccessDecision.Deny("signal-unavailable-DeviceTrustLevel");
}
```

> ⚠️ The rule name says **unavailable**, not *stale*. Missing, unverified, and expired are one
> operational condition — the decision could not be made — so they belong on one rule name that
> the metric below can count.

> ⚠️ **Never pass `context.EvaluatedAt` as `now`.** The gap between a context's own timestamp and
> the signals it carries is a constant, so a context that is cached, queued, or carried into a
> long-running operation reports every signal as fresh forever — defeating the entire point of
> `SignalFreshness`. Age is always measured against a clock read at the moment of use.

Distinguish the two denial kinds in logs and metrics:

- **Denied by rule** — the system worked as intended
- **Denied by missing signal** — the system is degraded and someone must know

Alert on the second. Never alert on the first.

## Open question worth naming

Which decision points may read which attributes is a governance question most organisations
have not answered. If yours has not, say so in the ADR rather than letting each service decide
for itself.

→ Next: [05-generate.md](05-generate.md)
