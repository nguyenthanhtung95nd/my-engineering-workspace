# Team AI Engineering Standards

You are a senior engineer maintaining the team's AI-assisted engineering standards.
This is a living document — update it via pull request, reviewed by a senior engineer before merging.

## Model Selection Guide

| Task type | Model | Rationale |
|-----------|-------|-----------|
| Code generation, review, debugging | Sonnet | Default for the majority of engineering tasks |
| Architecture decisions, security audits | Opus | Complex multi-step reasoning required |
| Log classification, high-volume extraction | Haiku | High volume, simple pattern recognition |

## Approved Command Library

| Command | Purpose | Required when |
|---------|---------|--------------|
| `/review` | CCSE-based code review | Before every PR submission |
| `/security-review` | OWASP Top 10 security review | Any auth, payment, or data access change |
| `/generate` | Production-ready scaffolding | New repository, service, or controller |
| `/debug` | Root cause analysis | Production bug with unclear cause |
| `/refactor` | Three-phase legacy refactoring | Code older than 2 years with no test coverage |
| `/architect` | Architecture planning and ADRs | Any major technical decision |
| `/devops` | Dockerfile, CI/CD, postmortem | Infrastructure changes and incidents |
| `/saas` | Multi-tenancy, billing, admin | New B2B SaaS features |
| `/workflow` | Task planning and PR descriptions | Starting any non-trivial task |
| `/pipeline` | Batch processing and automation | Multi-item generation or processing |
| `/test` | Tests for existing code | Adding coverage to untested code |
| `/migrate` | Migration safety review | Any EF Core migration before applying |
| `/incident` | Real-time incident triage | Production is down or degraded |
| `/perf` | Performance profiling | Endpoint slower than 2× target |
| `/compliance` | GDPR and SOC2 readiness | Pre-launch or compliance audit |
| `/onboard` | New codebase orientation | Joining a new project or repository |

## Quality Gates

### Before every PR:
- [ ] `/review` completed — no CRITICAL findings
- [ ] `/security-review` completed for any auth, payment, or data access changes
- [ ] Tests written for all new code
- [ ] XML doc comments on all new public APIs

### Before architecture decisions:
- [ ] `/architect` completed with full context provided
- [ ] ADR drafted and shared with the team
- [ ] Team review completed before implementation begins

### Before production deployments:
- [ ] Security checklist completed
- [ ] Rollback plan documented
- [ ] Monitoring alerts confirmed active

## Monthly Cost Budget
**Target: $50 per developer per month**

Alert at $40. Review usage at $50.

### Cost reduction guidance
- Trim context to the relevant sections — do not paste entire files when a method suffices
- Use Haiku for high-volume, simple classification tasks
- Cache results for repeated identical prompts
- Specify output scope: "in three bullet points" produces less output than "explain thoroughly"

## Onboarding Checklist for New Team Members

### Day 1
- [ ] Clone the `claude-mastery` workspace
- [ ] Read `.claude/CLAUDE.md` in full
- [ ] Complete all six hands-on exercises in `team-standards`
- [ ] Shadow a senior engineer in a Claude Code session on a real task

### Week 1
- [ ] Use every approved command at least once on real work
- [ ] Run `/review` on at least one PR (your own or a teammate's)
- [ ] Document one new pattern discovered during the week
- [ ] Propose one improvement to the command library (no merge required yet)

## Contributing to the Command Library

To add or modify a command:
1. Test the prompt on at least five real examples
2. Document the trigger condition, expected output format, and known limitations
3. Submit a pull request to the `claude-mastery` repository
4. Obtain one senior engineer review before merging
5. Announce the change in the team engineering channel

## Metrics Tracked

### Weekly (per developer)
- Commands used — are people using the tools?
- PR review cycle count — target: ≤ 2 rounds per PR

### Monthly (team)
- API cost per developer — target: < $50
- Bug escape rate — bugs found in production vs. caught in review
- Features shipped per sprint
