# 08 — AI and agents

Two distinct problems, often confused:

1. **Using an AI assistant to work on access rules** — a design-time activity with guardrails.
2. **Authorizing an AI agent that acts on its own** — a runtime problem.

---

# Part 1 — Using AI on access rules

## The governing assumption

> **Decisions stay deterministic and are always made by the system. AI assistance is limited to
> design-time work: authoring, reviewing, and testing.**

Access control depends on determinism, explainability, auditability, and predictable failure.
A language model's characteristics are strengths elsewhere and **liabilities here**.

## What AI may never do

| Never | Why |
|---|---|
| Make the access decision | No determinism, no reproducibility |
| Be the authoritative explanation of a decision | It infers intent from structure, not from what executed |
| Hold access rules in its prompt | A prompt is not an enforcement point |
| Be the sole detector of a violation | Misses are silent |
| Filter results after retrieval | Too late — see Part 3 |

> ⭐ **Plausible explanations are not correct decisions.** An explanation can sound right, and
> even *match* the verdict, while describing an evaluation that never happened. **Always compare
> it against the real decision trace.**

## What AI is genuinely good at here

**Design time** — explaining existing behaviour, exploring alternative ways to express an
intent, surfacing what a change would affect.

**Runtime** — summarising and clustering decision logs, surfacing outliers and rare permits,
drafting incident narratives, explaining evidence to non-technical stakeholders.

For log analysis, split the work so the model is never load-bearing:

```
deterministic rule  →  flags the anomaly        (must not depend on the model)
replay/verification →  confirms against the rule that was actually in effect
the model           →  summarises, groups, drafts the narrative
```

If the model misses a pattern or explains it badly, **detection still works**.

## The five guardrails

Attach these to every AI session about access rules. They are not optional.

1. **The existing code and vocabulary are the source of truth.** A proposal that invents
   entities, actions, or attributes is *incorrect by definition*.
2. **Do not invent new entities, actions, or attributes.** Extending the vocabulary is an
   architectural decision made by a human.
3. **Prefer least-privilege outcomes by default.**
4. **Always supply example requests and expected outcomes.** This forces proposals to be judged
   concretely rather than accepted as abstract logic.
5. **Output is advisory and will be reviewed by a human.** The assistant drafts; it does not
   decide.

## Context to attach before asking anything

- The access vocabulary (`AccessActions` and the entity model)
- The current decision service and its rules
- A representative request
- The existing tests and their expected decisions

> If a response mentions an action, attribute, or entity that does not exist in your code —
> **stop and fix the context**, not the prompt. The model is filling a gap you left.

## The six-step method

> **A rule change begins with an intended outcome, not with code.**

| Step | Do | Watch for |
|---|---|---|
| **1. Establish current behaviour** | Start from one concrete request you already understand. Confirm the current outcome, then ask *why* | The answer must name a specific rule and condition — not restate the request |
| **2. State the desired outcome precisely** | "Deny *this* request while leaving all other reader access unchanged" | **Vague intent produces overly broad changes and side effects** |
| **3. Ask for several options** | Request multiple expressions of the same intent | Treat them as *alternatives*, **not recommendations** |
| **4. Evaluate as a reviewer** | Judge on clarity, scope, maintainability | Which expresses intent most clearly? Which minimises unintended access? Which will be easiest to change later? |
| **5. Apply deliberately** | Choose one and **apply it yourself**. Update tests. Re-run the original request | **A surprising result is a signal to slow down, not to add conditions** |
| **6. Reflect** | — | It must be explainable in plain language and defensible in an audit |

## Useful prompts

- *"Which rule permits this request, and what relationship on the resource makes it true?"*
- *"Explain this access decision as you would to a product manager."* — if the explanation
  surprises you, review the rule
- *"What change would deny this request while leaving other access unchanged?"* — ask **without
  editing anything**
- *"What if we modified an existing rule instead of adding a new one?"*
- *"What if we expressed this as a carve-out rather than an extra condition?"*

These three shapes — a new denial rule, a narrowed allowance, a carve-out — can be logically
equivalent yet **differ completely as design**. Which one is right is a human judgement.

> ⭐ **Guardrails are not something added after adopting AI. They are what makes AI assistance
> safe to use in the first place.**

---

# Part 2 — Authorizing AI agents

## Agents are another class of principal

Not a special system — the same model, the same decision points, the same governance. What
changes is that **agents plan their own actions**.

Three properties that make them different:

- **Dynamic planning** — they decide what to do at runtime
- **Tool-based execution** — they reach real systems through tools
- **Adaptive behaviour** — they revise plans on new information

> The question shifts from *"may this application access this resource?"* to
> **"is this action safe for this agent to perform right now?"** — from static configuration to
> continuous governance.

> **Knowing who the agent is tells you nothing about what it will do next.** Authentication
> cannot govern an agent.

## Build the question from the authority behind the agent

| Input | Value |
|---|---|
| **Principal** | The agent |
| **Action** | The tool invocation |
| **Resource** | The system or data the tool touches |
| **Context** | The task · the delegation · **the initiating human** · the tenant |

The decision must answer: *could this agent, acting under this authority and in this context,
cause this tool to perform this action on this resource?*

## Prompts are not a security boundary

> **A model can be persuaded, confused, or compromised. The tool call must still pass through a
> decision made outside the model, on the execution path.**

Put the enforcement point **between the agent and the tool** — never inside the agent's
reasoning, never in its instructions.

```csharp
/// <summary>
/// Intercepts every tool invocation an agent attempts and enforces the access decision
/// before the tool executes.
/// </summary>
public sealed class AgentToolGateway(
    IAccessDecisionService<ToolTarget> decisions,
    IToolRegistry tools,
    ILogger<AgentToolGateway> logger)
{
    public async Task<ToolResult> InvokeAsync(
        AccessPrincipal agent, ToolCall call, AgentTaskContext task, CancellationToken ct)
    {
        // Resolve the tool to an action already in the vocabulary. Composing the action name
        // from the tool name instead would silently enrol every tool added later.
        if (!tools.TryResolveAction(call.ToolName, out var action))
            return ToolResult.Denied("tool-not-in-vocabulary", "Unknown tool.");

        var decision = await decisions.EvaluateAsync(
            new AccessQuery(agent, action, task.ToDecisionContext()),
            call.TargetResource,
            ct);

        if (!decision.IsAllowed)
        {
            // The full rule goes to the log, which is where auditors look.
            logger.LogInformation(
                "Agent {AgentId} denied {Tool} on {ResourceId} — rule {Rule}",
                agent.Id, call.ToolName, call.TargetResource.Id, decision.DeterminingRule);

            // A denial is INPUT to the agent's reasoning, not an exception — but only the
            // part that is safe to put in a model's context goes back.
            return ToolResult.Denied(AgentSafeDenials.For(decision.DeterminingRule));
        }

        return await tools.ExecuteAsync(call, ct);
    }
}
```

> ⭐ **A denial is not an error returned to a caller — it is input the agent reasons about.**
> Return it as structured data so the agent can replan, not as a thrown exception.

## What a denial may tell the agent

The rule that decided is evidence for the audit log. It is **not** automatically safe to put in
a model's context. `guardrail-tenant-isolation` tells the agent the resource exists and belongs
to someone else — the same disclosure that [03](03-enforcement-points.md) forbids over HTTP by
returning `404` instead of `403`. A model's context leaks more readily than an HTTP response: it
reaches the transcript, the next tool call, and whatever the agent says to a user.

But replacing every rule with "not permitted" throws away the thing this whole page is built on
— an agent that can replan instead of retrying blindly. So classify, rather than mask:

```csharp
/// <summary>
/// Maps an internal rule to what an agent may be told. Unmapped rules fall back to a generic
/// denial, so a new rule is never disclosed by default.
/// </summary>
public static class AgentSafeDenials
{
    private static readonly Dictionary<string, AgentDenial> Safe = new(StringComparer.Ordinal)
    {
        // Safe: describes the agent's own authority, reveals nothing about the resource.
        ["guardrail-action-outside-delegation"] =
            new("outside-delegation", "This task's delegation does not cover that action."),
        ["guardrail-agents-excluded-from-control-plane"] =
            new("control-plane", "Agents cannot change rules or infrastructure."),
        ["guardrail-delegation-expired"] =
            new("delegation-expired", "This task's delegation has expired.")

        // Deliberately absent: tenant isolation, resource scope, ownership — every rule whose
        // firing confirms something about a resource the agent was not entitled to know about.
    };

    public static AgentDenial For(string determiningRule) =>
        Safe.TryGetValue(determiningRule, out var denial)
            ? denial
            : new AgentDenial("not-permitted", "This action is not permitted for the current task.");
}
```

> ⭐ **Replanning needs to know about the agent's own authority, never about the resource.**
> That line is what separates a useful denial from a disclosure.

## Policy as guide, not only gate

Reactive denial alone teaches an agent its boundaries only by trial and error. Let it ask what
it is allowed to do **before** planning:

```csharp
/// <summary>
/// Returns the operations this agent may currently perform, for planning purposes only.
/// </summary>
/// <remarks>
/// This grants nothing. Every action is still evaluated at execution time.
/// </remarks>
public Task<IReadOnlyList<AllowedOperation>> QueryConstraintsAsync(
    AccessPrincipal agent, AgentTaskContext task, CancellationToken ct);
```

> ⭐ **Constraint queries guide planning. Decisions enforce.** The query must never short-circuit
> the check.

## Childproof the control plane

Separate the tools that **do the work** from the tools that **change the rules**.

> **Assume an agent will eventually try everything it can reach. Keep the important knobs out of
> reach.**

| Category | Examples | Agents |
|---|---|---|
| **Operational** | read, summarise, write to a scoped path, call a business API | Permitted, scoped |
| **Control plane** | edit access rules, change workflow definitions, alter infrastructure, mint credentials | **Denied outright** |

```csharp
// A guardrail, evaluated before any allowance.
if (principal.Kind is PrincipalKind.Agent or PrincipalKind.SubAgent
    && ControlPlaneActions.Contains(action))
    return AccessDecision.Deny("guardrail-agents-excluded-from-control-plane");
```

## Delegation is data, and it only narrows

When an agent hands work to a sub-agent, the handover must be **structured data evaluated at
decision time** — never implicit trust inherited from configuration.

```csharp
/// <summary>
/// A bounded grant from one principal to another for a specific task.
/// Delegation never creates permissions; it narrows existing ones.
/// </summary>
public sealed record DelegationRecord
{
    public required string                Delegator        { get; init; }
    public required string                Delegate         { get; init; }
    public required IReadOnlySet<string>  AllowedActions   { get; init; }
    public required IReadOnlySet<string>  ResourceScope    { get; init; }
    public required DateTimeOffset        ExpiresAt        { get; init; }
}
```

A delegation narrows along **three** dimensions — who, what, and which — and all three must be
enforced. Checking two of them and leaving the third as documentation is the common failure:

```csharp
// No delegation means no authority at all — not inherited authority.
if (principal.Kind is PrincipalKind.SubAgent && context.Delegation is null)
    return AccessDecision.Deny("guardrail-subagent-requires-delegation");

// Who, what, and which — all three, from one call that cannot be partially applied.
if (context.Delegation?.FindNarrowingViolation(principal.Id, action, resource.Id) is { } violation)
    return AccessDecision.Deny(violation);
```

`FindNarrowingViolation` lives on `DelegationRecord` in
[`templates/DecisionContext.cs`](../templates/DecisionContext.cs) deliberately: scattering the
three checks across call sites is how `ResourceScope` ends up declared, documented, and never
tested — leaving a sub-agent delegated one document able to reach every document in the tenant.

> ⚠️ **Expiry is not in that list.** It needs a clock, so it is enforced at the enforcement
> point along with signal freshness — see [03-enforcement-points.md](03-enforcement-points.md).
> Decision logic reads no clock at all, which is what keeps it deterministic and replayable.

> ⭐ **Delegation does not bypass the decision; it becomes an input to it.** An agent cannot
> delegate authority it does not have, and cannot widen authority by delegating. **Agents can
> help each other, but they cannot grant powers they do not already have.**

A missing delegation is the safest state: *it cannot exceed its authority because it has none.*

---

# Part 3 — Retrieval and RAG

> ⭐ **Authorize before retrieval, never after generation.**

Retrieval is the **last point** where access can be enforced reliably. Once unauthorized content
is in the model's context, it can influence the output in ways that are hard to detect or audit
— even when the final answer looks compliant.

**Correct shape:**

```csharp
// 1. Ask the index for CANDIDATE IDENTIFIERS and scores only — never content.
var candidates = await _index.SearchIdentifiersAsync(query, limit: 50, ct);

// 2. Authorize the candidates as ONE batch — never EvaluateAsync in a loop, or 50 candidates
//    become 50 relationship queries on the hottest path in the system.
var permitted = await _decisions.FilterAsync(
    new AccessQuery(principal, AccessActions.DocumentView, context), candidates, ct);

// 3. Only now fetch content, and only for what was permitted.
var content = await _documents.GetContentAsync(permitted, ct);
```

> ⚠️ Fetching content first and filtering afterwards **still exposes that material to the
> application**, regardless of what the response contains.

**Never** attempt this with instructions:

```
❌ "Do not reveal information the user is not allowed to see."
❌ Redacting the generated answer afterwards.
```

> ⭐ **Safe by construction, not safe by instruction.** Prompts still shape quality, tone, and
> framing — they must not carry responsibility for access boundaries.

---

## The line that matters most

Handing judgement to the model because there are many signals to weigh **mistakes a hard
engineering problem for a change in who should be in charge**. The volume of context is a reason
to build better machinery for handling signals — **not** a reason to hand the judgement to a
system that cannot tell you why it said yes.
