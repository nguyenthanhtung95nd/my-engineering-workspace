# 02 — Model selection

There is no correct model, only a correct fit. Every model has a **named failure** that tells
you when it has been outgrown. Move when you see the failure, not before.

## The ladder

| Model | Decides access by | Rules live in | Strength |
|---|---|---|---|
| **Ownership check** | A column on the row | The query | Trivial, fast, obvious |
| **RBAC** | Role assigned to the user | Data (role assignments) | Matches org charts; easy to audit |
| **ReBAC** | A path through a graph of relationships | Relationship data | Matches how people think about ownership and sharing |
| **ABAC** | Attributes of user, resource, environment | Rule logic evaluated at runtime | Fine-grained, contextual, scales |
| **Policy-based** | All of the above, evaluated together outside the app | External rules + live data | One answer for every caller |

Policy-based is not a fifth sibling — it is the frame that treats roles, relationships, and
attributes as **inputs** rather than as competing systems.

## When to move — the named failures

| You have | Still fine while | **Move when** | Move to |
|---|---|---|---|
| Ownership check in query | One resource type, one rule: "the owner" | A second rule appears — team access, manager visibility, status-dependent access | RBAC, or pull the decision out of the query |
| **RBAC** | Permissions map cleanly to job function | ⭐ **Role explosion** — roles start encoding *conditions*, and role count approaches user count | ABAC for conditions; ReBAC for relationships |
| RBAC + nested `if` | — | The same rule appears in two or more services | Externalise the decision |
| **ReBAC** | Relationships are **durable**: owner, member, manager | ⚠️ You start **inventing relationships to describe conditions** | Push conditions into attributes/context — the graph is being asked to do too much |
| **ABAC** | Conditions are genuinely runtime: device, time, risk | Too many near-identical rules; nobody knows which already exists or which attribute to trust | **Governance**, not another model |

> **Role explosion is a modelling error, not an RBAC failure.** It means RBAC is being asked to
> express conditions better handled as attributes, relationships, or runtime inputs.

> **The ABAC version of role explosion is real too**: instead of too many roles, too many
> slightly different rules. The hard problem stops being *writing* a rule and becomes *knowing
> which rule already exists and whether two teams mean the same thing by the same label*.

## The decisive question

**"Can the system say *why* this person has access?"**

An ownership check cannot. It knows Doug may edit the folder; it cannot say whether that is
because he is on the marketing team or because of a temporary collaboration. The moment *why*
matters — for audit, for support, for revocation — the model has been outgrown.

## Durable vs volatile data

Rules are only as stable as the data they reference.

| Reference this | Not this |
|---|---|
| Team, project, role, relationship, ownership | Job title string, department name, a specific manager's ID, project code |
| Governed entities with an owner | Facts that change without anyone noticing |

A rule keyed on `principal.Department == "Finance"` breaks silently during a reorg. A rule keyed
on membership in a governed team survives it. **Governance is what makes rule stability
possible** — roles are assets, not labels.

## Hybrid is the normal outcome

Dynamic models **complement** RBAC; they rarely replace it. Expect a mix, and say so explicitly
in the ADR rather than pretending the system is pure. Most systems end up with role-based
coarse access plus relationship or attribute rules for the parts roles cannot express.

## Modelling relationships

When relationships drive access, model membership as **edges**, not as a collection embedded in
an entity. In EF Core terms: a join entity with an index, not a serialized list on the parent.

```csharp
// Good — an edge the store can answer directly and index
public sealed class TeamMembership
{
    public required Guid TeamId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset GrantedAt { get; init; }
}

// The composite key is the point: it makes a duplicate membership impossible in the store
// rather than merely unlikely in the code, and gives the lookup an index for free.
builder.Entity<TeamMembership>().HasKey(m => new { m.TeamId, m.UserId });
```

Model hierarchies (manager chains, folder trees) as **real hierarchies** so traversal is a
query, not a chain of `manager.manager.manager`.

> ⚠️ **But note the reach.** If the graph encodes multi-hop connections, traversal will follow
> them. Any system-wide restriction must account for the full reach of those relationships, not
> just the first hop.

## Separate "can use" from "can grant"

Being able to use a resource is **not** the same as being able to give others access to it.
Model them as two different permissions. Collapsing them produces the *"I shared it because I
could open it"* failure.

Express it as the set of actions the principal holds, drawn from the same vocabulary the
decision uses — not as a parallel bag of booleans that drifts from it:

```csharp
// The UI asks which of these it may offer; the server still decides every actual operation.
var granted = new HashSet<string>(StringComparer.Ordinal)
{
    AccessActions.DocumentView,
    AccessActions.DocumentEdit
    // DocumentShare deliberately absent — using is not granting.
};
```

> A `CanView`/`CanEdit`/`CanShare` record looks tidier, but it is a second vocabulary. When
> someone adds an action to `AccessActions`, nothing makes them add the matching flag — and the
> gap shows up as a permission that silently never appears in the UI.

## Record the decision

Whatever you choose, write an ADR: context, decision, consequences, and **what would make us
revisit this**. That last line is what prevents the model outliving its fit.

→ Next: [03-enforcement-points.md](03-enforcement-points.md)
