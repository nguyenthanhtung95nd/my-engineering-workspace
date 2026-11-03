namespace YourProject.Authorization;

/// <summary>
/// The complete set of authorizable operations. Adding a member here is a deliberate
/// architectural act — it widens what the system can be asked to permit.
/// </summary>
/// <remarks>
/// <para>
/// Every enforcement point names its operation from here — HTTP handler, background worker,
/// message consumer, agent tool gateway alike. A string literal at a call site is how two
/// spellings of the same action enter a codebase, and from there two different decisions.
/// </para>
/// <para>
/// These are constants rather than an enum on purpose, which is the one place this template
/// departs from the project's usual "enums, not strings" rule. Action names cross process
/// boundaries: they are written to decision logs, matched by external policy engines, and read
/// by auditors years later. An enum member renamed during a refactor silently rewrites history;
/// a constant's value is stable even when the symbol moves. The trade-off is that the compiler
/// cannot stop an unknown action reaching the decision service, so deny-by-default must catch
/// it — see the final fall-through in every decision service.
/// </para>
/// <para>
/// Pick the separator, casing, and singular/plural once, then never deviate.
/// </para>
/// </remarks>
public static class AccessActions
{
    /// <summary>Read a document's content.</summary>
    public const string DocumentView = "document:view";

    /// <summary>Change a document's content.</summary>
    public const string DocumentEdit = "document:edit";

    /// <summary>Grant another principal access to a document. Separate from edit, on purpose.</summary>
    public const string DocumentShare = "document:share";

    /// <summary>Read an order.</summary>
    public const string OrderView = "order:view";

    /// <summary>Run an order through processing.</summary>
    public const string OrderProcess = "order:process";

    /// <summary>Read an employee record.</summary>
    public const string EmployeeRead = "employee:read";
}

/// <summary>
/// Operations that change the rules, the infrastructure, or the credentials rather than doing
/// the work. Autonomous principals are denied these outright.
/// </summary>
/// <remarks>
/// Keep this list separate from <see cref="AccessActions"/> rather than marking members inside
/// it. A separate set means the guardrail is a membership test that cannot be forgotten when
/// somebody adds a new control-plane operation to the main vocabulary.
/// </remarks>
public static class ControlPlaneActions
{
    /// <summary>Edit an access rule.</summary>
    public const string PolicyEdit = "policy:edit";

    /// <summary>Change a workflow definition.</summary>
    public const string WorkflowEdit = "workflow:edit";

    /// <summary>Alter infrastructure.</summary>
    public const string InfrastructureChange = "infrastructure:change";

    /// <summary>Issue credentials.</summary>
    public const string CredentialMint = "credential:mint";

    private static readonly HashSet<string> All =
        new(StringComparer.Ordinal)
        {
            PolicyEdit, WorkflowEdit, InfrastructureChange, CredentialMint
        };

    /// <summary>Whether the named action belongs to the control plane.</summary>
    /// <param name="action">An action name.</param>
    /// <returns><see langword="true"/> when no autonomous principal may perform it.</returns>
    public static bool Contains(string action) => All.Contains(action);
}
