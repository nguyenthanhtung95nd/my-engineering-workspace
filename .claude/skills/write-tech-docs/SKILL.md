---
name: write-tech-docs
description: >
  Creates complete, production-ready technical documentation following
  docs-for-developers best practices: skimming-first design, 5C code samples,
  and AI-ready structure. Covers Getting Started, Conceptual, Tutorial, How-to,
  and Reference doc types. Use when the user needs to write, draft, or improve
  technical documentation for a feature, API, service, or library. Outputs both
  an in-conversation draft and a saved markdown file.
---

# Write Tech Docs

## Process

### 1. Gather context

Ask these questions **one at a time**, waiting for each answer:

1. **What are you documenting?** (feature name, API endpoint, service, library)
2. **What type of doc?**
   - Getting Started — install, configure, first success (minimize TTHW)
   - Conceptual — why/how the system works, architecture, design
   - Tutorial — learn a skill step-by-step (training-focused)
   - How-to — solve a specific real-world task
   - Reference — look up API, CLI, config, or SDK details
3. **Who is the audience?** (brand new / familiar / expert)
4. **Where will this file live?** (e.g. `docs/api-auth.md`)

Skip any question that is already obvious from context.

### 2. Select template

| Doc Type | Structure |
|----------|-----------|
| Getting Started | Prerequisites → Install → Configure → First run → Next steps |
| Conceptual | Overview → Key concepts → How it works → When to use |
| Tutorial | Goal → Prerequisites → Steps 1…N → Verify → What you learned |
| How-to | Goal → Prerequisites → Steps → Troubleshooting |
| Reference | Description → Parameters / Options → Examples → Related |

### 3. Draft

Apply rules from [REFERENCE.md](./REFERENCE.md):
- Skimming-first: most important info first, headings, bullets, tables
- 5C code samples: Explained · Concise · Clear · Usable · Trustworthy
- Real names only in code (`customerId`, not `foo`)
- Replaceable values as `YOUR_API_KEY`, `YOUR_DATABASE_URL`
- At least one working code example per major section
- Friction Log check: no assumed knowledge, no skipped "obvious" steps

Show the complete draft in the conversation first.

### 4. Confirm and save

After user confirms, save to the path from Step 1.

### 5. Self-review checklist

- [ ] Correct doc type structure followed
- [ ] No assumed knowledge (Curse of Knowledge check)
- [ ] All code samples meet 5C
- [ ] Headings enable skimming (H1 → H2 → H3)
- [ ] Placeholder values use `YOUR_*` format
- [ ] File saved to the correct path

## Output

- Full draft shown in conversation (copy-ready markdown)
- File saved to `docs/{name}.md` or path specified in Step 1
