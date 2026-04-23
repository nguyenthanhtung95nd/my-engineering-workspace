---
name: write-a-skill
description: >
  Creates new Claude Code skills with proper structure, progressive disclosure,
  and bundled resources. Use when the user wants to create, write, or build a
  new skill for this workspace.
---

# Write a Skill

## Process

1. **Gather requirements** — ask about:
   - What task or domain does the skill cover?
   - What specific use cases should it handle?
   - Does it need executable scripts or just instructions?
   - Any reference materials to include?

2. **Draft the skill** — create:
   - `SKILL.md` with concise instructions
   - Additional reference files if content exceeds 100 lines
   - Utility scripts if deterministic operations are needed

3. **Review with user** — present draft and ask:
   - Does this cover your use cases?
   - Anything missing or unclear?
   - Should any section be more or less detailed?

## Skill Structure

```
skill-name/
├── SKILL.md           # Main instructions (required)
├── REFERENCE.md       # Detailed docs (if needed)
└── EXAMPLES.md        # Usage examples (if needed)
```

## SKILL.md Template

```markdown
---
name: skill-name
description: Brief description of capability. Use when [specific triggers].
---

# Skill Name

## Quick start
[Minimal working example]

## Workflows
[Step-by-step processes for complex tasks]

## Advanced features
[Link to separate files: See REFERENCE.md]
```

## Description Requirements

The description is **the only thing Claude sees** when deciding which skill to load.

**Format:**
- Max 1024 chars
- First sentence: what it does
- Second sentence: "Use when [specific triggers]"

**Good example:**
```
Reviews C# code for security vulnerabilities, bugs, and .NET convention violations.
Use when reviewing code before PR, or when user asks for a code review.
```

**Bad example:**
```
Helps with code.
```

## Checklist Before Saving

- [ ] Description includes triggers ("Use when...")
- [ ] `SKILL.md` under 100 lines
- [ ] No time-sensitive information (versions, dates)
- [ ] Consistent terminology throughout
- [ ] Concrete examples included
- [ ] Saved to `.claude/skills/{name}/SKILL.md`
