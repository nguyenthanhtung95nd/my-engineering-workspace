namespace YourProject.Authorization;

/// <summary>
/// The normalised signal vocabulary every decision point reads.
/// </summary>
/// <remarks>
/// Without a shared vocabulary the same rule text produces different outcomes in different
/// services, because each one means something subtly different by the same attribute name.
/// Adding a member here is an architectural decision, not a convenience — it widens what every
/// rule in the system is allowed to depend on.
/// </remarks>
public sealed record DecisionContext
{
    /// <summary>Tenant the request is scoped to. A trust boundary, not a filter column.</summary>
    public required string TenantId { get; init; }

    /// <summary>Employment standing, converted from the source system's string at the boundary.</summary>
    public required EmploymentStatus EmploymentStatus { get; init; }

    /// <summary>Device posture, converted from the source system's string at the boundary.</summary>
    public required DeviceTrust DeviceTrustLevel { get; init; }

    /// <summary>Whether the principal is currently on call.</summary>
    public required bool OnCall { get; init; }

    /// <summary>
    /// When this context was assembled, for audit and correlation.
    /// </summary>
    /// <remarks>
    /// This is **not** the clock for freshness checks. Comparing a signal against the timestamp
    /// of the context that carries it makes the gap between them a constant, so a context reused
    /// or cached for an hour still reports every signal as fresh. Freshness is measured at the
    /// enforcement point against a clock read at that moment — see <see cref="IsStale"/>.
    /// </remarks>
    public required DateTimeOffset EvaluatedAt { get; init; }

    /// <summary>
    /// The service through which the request reached this system, when the principal is not the
    /// direct caller. Recorded so a decision can be explained afterwards; it never widens one.
    /// </summary>
    public string? ActingVia { get; init; }

    /// <summary>
    /// A delegation record when the principal is acting under delegated authority.
    /// </summary>
    public DelegationRecord? Delegation { get; init; }

    /// <summary>
    /// Where each signal came from and when, keyed by <see cref="SignalNames"/>. A signal without
    /// provenance is a rumour and must be treated as missing.
    /// </summary>
    /// <remarks>
    /// Build this with an explicit <see cref="StringComparer.Ordinal"/>. A culture-sensitive or
    /// case-insensitive comparer makes lookups behave differently between environments, and a
    /// lookup that silently misses here becomes a permanent denial nobody can trace.
    /// </remarks>
    public required IReadOnlyDictionary<string, SignalProvenance> Provenance { get; init; }

    /// <summary>
    /// Whether the named signal is unfit to decide on: missing, unverified, or past its
    /// permitted lifetime.
    /// </summary>
    /// <param name="signal">A name from <see cref="SignalNames"/>, never a literal.</param>
    /// <param name="maxAge">Permitted age for this signal, from <see cref="SignalFreshness"/>.</param>
    /// <param name="now">
    /// The current instant, read from the enforcement point's injected clock. Never pass
    /// <see cref="EvaluatedAt"/>: a context cannot measure its own staleness.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the signal is missing, its integrity was never confirmed, it
    /// is older than <paramref name="maxAge"/>, or it is timestamped in the future. In each case
    /// the caller must fail closed.
    /// </returns>
    public bool IsStale(string signal, TimeSpan maxAge, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);

        if (!Provenance.TryGetValue(signal, out var p) || !p.Verified)
            return true;        // a freshly forged value is still forged

        // A retrieval timestamp in the future means clock skew upstream, not an unusually fresh
        // signal. Accepting it would keep a stale value alive for the whole skew window.
        var age = now - p.RetrievedAt;
        return age < TimeSpan.Zero || age > maxAge;
    }

    /// <summary>
    /// Provenance of the named signal, or <see langword="null"/> when none was supplied.
    /// </summary>
    /// <remarks>
    /// A missing signal is an expected state on the fail-closed path, so logging one must not
    /// itself throw. Never reach for the dictionary indexer there.
    /// </remarks>
    public SignalProvenance? ProvenanceFor(string signal) =>
        Provenance.TryGetValue(signal, out var p) ? p : null;
}

/// <summary>Where a signal came from, when it was retrieved, and whether it was verified.</summary>
/// <param name="Source">Originating system.</param>
/// <param name="RetrievedAt">When the value was obtained.</param>
/// <param name="Verified">
/// Whether integrity was confirmed (signature or mutual TLS). An unverified signal is unusable
/// however recent it is.
/// </param>
public sealed record SignalProvenance(string Source, DateTimeOffset RetrievedAt, bool Verified);

/// <summary>Device posture, ordered so the default value is the least trusted.</summary>
public enum DeviceTrust
{
    /// <summary>Posture could not be established. Never treat as trusted.</summary>
    Unknown = 0,

    /// <summary>A known device outside endpoint management.</summary>
    Unmanaged = 1,

    /// <summary>Enrolled in endpoint management and meeting its policy.</summary>
    Managed = 2
}

/// <summary>Employment standing, ordered so the default value grants nothing.</summary>
public enum EmploymentStatus
{
    /// <summary>Standing could not be established.</summary>
    Unknown = 0,

    /// <summary>Employment has ended.</summary>
    Terminated = 1,

    /// <summary>Employed, but access is suspended.</summary>
    Suspended = 2,

    /// <summary>Employed and in good standing.</summary>
    Active = 3
}

/// <summary>
/// Keys used in <see cref="DecisionContext.Provenance"/>. Never write these as literals: a typo
/// makes the signal look missing, which fails closed silently and permanently, and the symptom
/// is indistinguishable from a genuinely degraded upstream.
/// </summary>
public static class SignalNames
{
    /// <summary>Key for <see cref="DecisionContext.OnCall"/>.</summary>
    public const string OnCall = nameof(DecisionContext.OnCall);

    /// <summary>Key for <see cref="DecisionContext.DeviceTrustLevel"/>.</summary>
    public const string DeviceTrustLevel = nameof(DecisionContext.DeviceTrustLevel);

    /// <summary>Key for <see cref="DecisionContext.EmploymentStatus"/>.</summary>
    public const string EmploymentStatus = nameof(DecisionContext.EmploymentStatus);
}

/// <summary>
/// Permitted lifetimes per signal. Attributes have very different lifetimes; a single global
/// TTL is wrong for most of them.
/// </summary>
public static class SignalFreshness
{
    /// <summary>On-call rotations change constantly and grant real privilege.</summary>
    public static readonly TimeSpan OnCall = TimeSpan.FromMinutes(5);

    /// <summary>Device posture can change mid-session.</summary>
    public static readonly TimeSpan DeviceTrustLevel = TimeSpan.FromMinutes(15);

    /// <summary>Employment status changes rarely and is confirmed through other channels.</summary>
    public static readonly TimeSpan EmploymentStatus = TimeSpan.FromHours(24);
}

/// <summary>
/// A bounded grant of authority from one principal to another for a specific task.
/// </summary>
/// <remarks>
/// Delegation never creates permissions. It narrows existing ones to a task-bound scope, so a
/// delegate can never exceed what the delegator already had.
/// </remarks>
public sealed record DelegationRecord
{
    /// <summary>Identifier of the principal granting the authority.</summary>
    public required string DelegatorId { get; init; }

    /// <summary>Identifier of the only principal permitted to use this record.</summary>
    public required string DelegateId { get; init; }

    /// <summary>Actions permitted under this delegation — a subset of the delegator's own.</summary>
    public required IReadOnlySet<string> AllowedActions { get; init; }

    /// <summary>Identifiers of the resources this delegation reaches.</summary>
    public required IReadOnlySet<string> ResourceScope { get; init; }

    /// <summary>
    /// Expiry, enforced at the enforcement point before evaluation so that decision logic
    /// stays free of clock dependencies.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Whether this record authorises <paramref name="principalId"/> to perform
    /// <paramref name="action"/> on <paramref name="resourceId"/>.
    /// </summary>
    /// <param name="principalId">The principal presenting the delegation.</param>
    /// <param name="action">The action being attempted.</param>
    /// <param name="resourceId">Identifier of the resource being acted upon.</param>
    /// <returns>
    /// The narrowing rule that failed, or <see langword="null"/> when the delegation covers this
    /// operation. A non-null result is a denial reason, not a permission.
    /// </returns>
    /// <remarks>
    /// Every narrowing dimension is checked here so that callers cannot enforce three of the
    /// four and quietly skip the fourth. Expiry is deliberately absent: it needs a clock, and
    /// belongs at the enforcement point with the rest of the time-dependent checks.
    /// </remarks>
    public string? FindNarrowingViolation(string principalId, string action, string resourceId)
    {
        if (principalId != DelegateId) return "guardrail-delegation-wrong-delegate";
        if (!AllowedActions.Contains(action)) return "guardrail-action-outside-delegation";
        if (!ResourceScope.Contains(resourceId)) return "guardrail-resource-outside-delegation";
        return null;
    }
}

/// <summary>The authority behind a request. Not necessarily the caller.</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="TenantId">Tenant the principal belongs to.</param>
/// <param name="Kind">Whether this is a human, a service, or an agent.</param>
public sealed record AccessPrincipal(string Id, string TenantId, PrincipalKind Kind);

/// <summary>The class of actor making a request.</summary>
public enum PrincipalKind
{
    /// <summary>A human user.</summary>
    User,

    /// <summary>A workload acting on its own behalf.</summary>
    Service,

    /// <summary>An autonomous agent that plans its own actions.</summary>
    Agent,

    /// <summary>An agent spawned by another agent. Has no authority without a delegation.</summary>
    SubAgent
}
