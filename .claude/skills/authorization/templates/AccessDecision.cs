using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace YourProject.Authorization;

/// <summary>
/// The outcome of an access evaluation, including the rule that determined it.
/// </summary>
/// <remarks>
/// Every decision carries a determining rule so that outcomes can be logged, explained to
/// support staff, and defended during an audit. A decision without a rule is not auditable.
/// </remarks>
public sealed record AccessDecision
{
    private AccessDecision(bool isAllowed, string determiningRule, string? reason)
    {
        IsAllowed = isAllowed;
        DeterminingRule = determiningRule;
        Reason = reason;
    }

    /// <summary>Whether the operation is permitted.</summary>
    [MemberNotNullWhen(false, nameof(Reason))]
    public bool IsAllowed { get; }

    /// <summary>
    /// Identifier of the rule that produced this outcome, for example
    /// <c>owner-view-edit</c> or <c>guardrail-tenant-isolation</c>. Never empty.
    /// </summary>
    public string DeterminingRule { get; }

    /// <summary>
    /// Human-readable explanation of a denial, safe to surface to support staff.
    /// Always populated when <see cref="IsAllowed"/> is <see langword="false"/>.
    /// </summary>
    public string? Reason { get; }

    /// <summary>Permits the operation under the named rule.</summary>
    /// <param name="rule">The rule that permitted it. Must not be empty.</param>
    public static AccessDecision Allow(string rule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        return new AccessDecision(true, rule, null);
    }

    /// <summary>Denies the operation under the named rule.</summary>
    /// <param name="rule">The rule that denied it. Must not be empty.</param>
    /// <param name="reason">
    /// Explanation safe to show support staff. Falls back to a generic sentence so that
    /// <see cref="Reason"/> is never null on a denial.
    /// </param>
    public static AccessDecision Deny(string rule, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        return new AccessDecision(false, rule, reason ?? "Denied by rule.");
    }

    private static readonly AccessDecision NoRuleMatched =
        new(false, "deny-by-default", "No rule permitted this operation.");

    /// <summary>
    /// The outcome when no rule permitted the operation. Absence of a rule is a denial.
    /// </summary>
    /// <remarks>
    /// Use this only when evaluation genuinely fell through every rule. A rule that ran and
    /// rejected the request must report itself through <see cref="Deny"/>, or the decision log
    /// will claim nothing matched when something did.
    /// </remarks>
    public static AccessDecision DenyByDefault() => NoRuleMatched;

    /// <summary>
    /// Keeps <see cref="Reason"/> out of the generated <c>ToString</c>, so logging a decision as
    /// a whole object cannot spill explanation detail into application logs.
    /// </summary>
    /// <param name="builder">Builder supplied by the compiler-generated <c>ToString</c>.</param>
    /// <returns><see langword="true"/>, since members were written.</returns>
    /// <remarks>Private, not protected override: the record is sealed, so nothing derives from it.</remarks>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(IsAllowed)).Append(" = ").Append(IsAllowed)
               .Append(", ").Append(nameof(DeterminingRule)).Append(" = ").Append(DeterminingRule);
        return true;
    }
}

/// <summary>
/// Everything a decision needs except the resource: who is asking, what they want to do, and
/// what was true at request time.
/// </summary>
/// <remarks>
/// Grouping these three keeps every decision method within the three-parameter limit, and makes
/// the single-resource and batch calls read identically — only the resource side varies.
/// </remarks>
/// <param name="Principal">The authority behind the request — not necessarily the caller.</param>
/// <param name="Action">An action from the project's enumerated vocabulary, never a literal.</param>
/// <param name="Context">
/// Normalised signals, already validated as fresh by the enforcement point.
/// </param>
public sealed record AccessQuery(AccessPrincipal Principal, string Action, DecisionContext Context);

/// <summary>
/// Evaluates access decisions for a resource type.
/// </summary>
/// <typeparam name="TResource">The resource the decision concerns.</typeparam>
/// <remarks>
/// <para>
/// <typeparamref name="TResource"/> is invariant: <see cref="FilterAsync"/> returns the resource
/// type as well as consuming it, so a contravariant <c>in</c> declaration would not compile.
/// </para>
/// <para>
/// Implementations are a pure function of their inputs. They must not read a clock, call
/// <c>DateTime.Now</c>, or test signal freshness — the enforcement point has already done that
/// and would not have called in with a context it could not vouch for. That is what makes a
/// decision reproducible from its inputs alone when an auditor asks months later.
/// </para>
/// </remarks>
public interface IAccessDecisionService<TResource>
{
    /// <summary>
    /// Determines whether the query's principal may perform its action on
    /// <paramref name="resource"/>.
    /// </summary>
    /// <param name="query">Principal, action, and validated context.</param>
    /// <param name="resource">The resource being acted upon.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AccessDecision"/> that always names the determining rule. Implementations
    /// must deny by default and must evaluate guardrails before any allowance.
    /// </returns>
    Task<AccessDecision> EvaluateAsync(
        AccessQuery query,
        TResource resource,
        CancellationToken ct = default);

    /// <summary>
    /// Narrows a candidate set to the resources the query's principal may act on.
    /// </summary>
    /// <param name="query">Principal, action, and validated context.</param>
    /// <param name="candidates">Candidates, in the order the caller wants them back.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The permitted subset, in the original order. Empty when nothing is permitted — never null.
    /// </returns>
    /// <remarks>
    /// List endpoints, exports, and retrieval pipelines must use this. Implementations load the
    /// relationships and attributes for the whole set in one round trip; a loop over
    /// <see cref="EvaluateAsync"/> is not an acceptable implementation, because it turns one
    /// decision into N information-point queries on the hottest path in the system.
    /// </remarks>
    Task<IReadOnlyList<TResource>> FilterAsync(
        AccessQuery query,
        IReadOnlyList<TResource> candidates,
        CancellationToken ct = default);
}
