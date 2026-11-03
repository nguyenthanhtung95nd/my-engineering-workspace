# 07 — Governance

Version control, CI, and automated tests are **not** governance. Teams with all three still
produce near-duplicate rules, rules that outlive their purpose, and "temporary" exceptions that
run for nine months.

> **Access rules accumulate technical debt exactly like code** — and without owners, review
> schedules, and feedback, nobody notices.

**Governance is assurance, not control.** It must produce **evidence**, not paperwork. If a
governance step does not generate an artefact that is useful during an audit, delete the step.

## Authoring order — logic comes last

The most counter-intuitive and most valuable rule here:

1. **Identify intent** — what business outcome is this about?
2. **Define purpose and scope** — who, what, under which circumstances?
3. **Document the rationale** — write the ADR *now*, while the reason is fresh
4. **Model entities, relationships, attributes** — what facts does the decision need?
5. **Write the logic — last**

Starting at step 5 produces rules that work and that nobody can explain six months later.

## Decision records

One record per meaningful access rule. Reference its ID from the code.

```markdown
# ADR-014 — Managers may view documents owned by their reports

## Context
Support escalations required managers to see their reports' documents. The previous
workaround was a shared service account, which made attribution impossible.

## Decision
A manager may view and edit documents owned by a direct or indirect report, traversing
the management hierarchy. Sharing is NOT included — viewing is not granting.

## Consequences
- Removes the shared service account and restores per-user attribution.
- Traversal follows the full chain: a VP reaches everything beneath them. Accepted
  deliberately; the tenant-isolation guardrail still bounds it.
- Requires the management hierarchy to be accurate. Owner: People Systems.

## What would make us revisit this
- Hierarchy depth exceeding four levels in practice.
- Any request to extend this rule to sharing or deletion.

## Related
Rule ID: `manager-of-owner` · Tests: `DocumentAccessDecisionServiceTests` · Owner: Platform Identity
```

The **"what would make us revisit this"** section is what stops a rule outliving its fit.

## Roles

| Role | Responsibility |
|---|---|
| **Owner** | Accountable for continued relevance — *after* publication, not just at merge |
| **Approver** | Confirms the rule matches the intended business rule |
| **Author** | Writes it |
| **Reviewer** | **A different person.** Enforced by branch protection, not by convention |
| **Auditor** | Verifies reviews happened on schedule and records are complete |

Separation of duties that relies on people remembering is not separation of duties.

## Start small — the ladder

| Step | Do this | When |
|---|---|---|
| 1 | **Decision records** for access rules | Now — needs no tooling at all |
| 2 | Named owner + periodic review | **Quarterly is enough** initially |
| 3 | Version control + at least one peer review before merge | Once there are more than ~10 rules |
| 4 | Identity lifecycle hygiene (joiners/movers/leavers) | **Only when** manual management starts producing errors |
| 5 | Build on **evidence, not ceremony** | Throughout |

Do not skip to step 4. Adding identity governance before you can explain a single decision
produces process without assurance.

## Rule lifecycle

```
Create → Review & approve → Deploy & use → Monitor → Retire
   ↑                                           │
   └──────────────── Update ───────────────────┘
```

**Retire is the forgotten phase.** Schedule it. A rule with no traffic for two quarters is
either dead or masking a coverage gap — both need investigating.

## Monitoring

Decision logs turn access behaviour into evidence. Every decision is recorded — but not every
decision belongs in the application log:

```csharp
// Denials are rare, individually interesting, and the thing you alert on.
if (!decision.IsAllowed)
{
    logger.LogInformation(
        "AccessDecision DENY principal={PrincipalId} action={Action} " +
        "resource={ResourceId} rule={DeterminingRule} tenant={TenantId}",
        principal.Id, action, resource.Id, decision.DeterminingRule, principal.TenantId);
}
else
{
    // Allows are the overwhelming majority and scale with rows returned, not with requests:
    // one page of 50 results is 50 decisions. They are audit evidence, so they are kept — but
    // through a buffered sink, not the request log.
    audit.RecordAllow(principal.Id, action, resource.Id, decision.DeterminingRule);
}
```

> ⚠️ **Log volume follows resources, not requests.** Writing every ALLOW at `Information` makes
> an endpoint's logging cost proportional to its page size — the one thing on the request path
> that grows without anybody deciding it should.

> ⚠️ **No PII in these logs** (`rules/checklist.md`). Identifiers, never names or emails.

What to watch for:

| Signal | What it means |
|---|---|
| A rule that never fires | Dead, or a coverage gap elsewhere |
| A rule that fires for everything | Too broad — probably a wildcard |
| Rising denials on one path | Either an attack or a rule that no longer matches reality |
| Repeated manual exceptions | **No shared rule can express the real intent** — fix the rule, not the exception |

## Audit artefacts fall out of normal work

If these are produced continuously, audit preparation stops being a project:

- Decision records, tagged to the controls they satisfy
- Version-control history showing author ≠ reviewer
- CI test results covering permit **and** deny
- Decision logs
- Review meeting outcomes linked to rule IDs

## The division of responsibility

> **Identity governance governs the *inputs* to a decision. Policy governance governs the
> *logic* that evaluates those inputs.**

Getting this backwards — asking the identity team to own business access rules, or asking
product teams to own employment status — is the most common organisational failure here.

→ Next: [08-ai-and-agents.md](08-ai-and-agents.md)
