---
name: architecture-decision
description: >
  Load automatically when the user asks about system design, architecture choices,
  microservices decomposition, database design, scalability, or technology selection.
  Also load when the conversation includes phrases like "should we", "which is better",
  "trade-offs", "design this", or "help me architect".
context: auto
---

# Architecture Decision Framework

## Core Principle

Right-sized solution over best technology.
Solve today's problem. Design for tomorrow's scale. Don't build for a scale you haven't reached.

## Evaluation Dimensions

| Dimension | Key questions |
|-----------|--------------|
| Performance | What are the p50/p95/p99 latency targets? What is the read/write ratio? |
| Reliability | Consistency vs. availability trade-off? What is the acceptable failure mode? |
| Operability | How does this fail at 3am? How hard is it to debug? What does monitoring look like? |
| Cost | Infrastructure cost at current scale? At 10x scale? |
| Time to market | Does the team have the expertise to build and operate this? |

## Scale Calibration

Use these benchmarks before recommending a solution. Many teams over-engineer for scale they will never reach.

### Application architecture
```
< 10K users       Monolith + single database is the correct choice
< 100K users      Monolith + read replica may be all you need
< 1M users        Selective service extraction (Strangler Fig pattern)
> 1M users        Microservices decomposition is justified
```

### Messaging and queuing
```
< 1K events/day    Database polling with Hangfire is appropriate
< 100K events/day  Azure Service Bus Standard tier
> 1M events/day    Evaluate Kafka or Azure Event Hubs
```

### Caching
```
Single instance    IMemoryCache — zero cost, zero operational complexity
Multi-pod AKS      Redis distributed cache
> 10M req/day      Two-tier: in-process memory + Redis
```

## ADR Template

Use this when a significant architectural decision has been made.

```markdown
# ADR-[N]: [Title]

Date: [YYYY-MM-DD]
Status: Proposed | Accepted | Deprecated

## Context
[What is the situation that requires a decision? What constraints exist?]

## Options Considered

| Option | Strengths | Weaknesses |
|--------|-----------|-----------|
| A      | ...       | ...       |
| B      | ...       | ...       |

## Decision
[What was chosen and the primary reason for choosing it over the alternatives.]

## Consequences

**Positive:**
- [benefit 1]

**Negative:**
- [trade-off 1]

**Risks:**
- [risk 1]

## Review Date
[When to revisit this decision — especially important for "start simple, scale later" choices.]
```

## Challenge Questions

Always ask these before finalizing a recommendation:

1. What assumptions are we making that could be wrong?
2. What evidence would cause us to change this decision?
3. What is the cost of getting this wrong?
4. Can we reverse this decision without significant pain?
5. Does our team have the expertise to build and operate this for the next two years?
