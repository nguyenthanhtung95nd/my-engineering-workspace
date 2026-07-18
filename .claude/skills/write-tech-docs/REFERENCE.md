# Write Tech Docs — Reference

## Doc Types

### Getting Started
Goal: user runs successfully as fast as possible.
KPI: **TTHW** (Time To Hello World) — measure and minimize it.
Include: exact commands, expected output, common errors with fixes.
Omit: deep theory — link to Conceptual instead.

### Conceptual
Goal: user understands WHY, not just HOW.
Include: diagrams, architecture, design trade-offs.
Omit: step-by-step tasks — link to How-to instead.

### Tutorial
Goal: user learns a skill by doing.
Style: narrative journey, not a task list. Explain each step.

### How-to
Goal: user completes one specific real-world task.
Style: direct, no preamble. Start with Step 1.
Always include a Troubleshooting section.

### Reference
Goal: user finds information as fast as possible.
Style: consistent format per entry, scannable, no prose filler.

---

## 5C Code Sample Principles

| Principle | Rule |
|-----------|------|
| **Explained** | 1–2 lines of context before the code block |
| **Concise** | Show only the relevant part; max 80 chars per line |
| **Clear** | Real names only: `customerId`, `orderTotal` — never `foo`, `x` |
| **Usable** | Replaceable values written as `YOUR_API_KEY`, `YOUR_BUCKET_NAME` |
| **Trustworthy** | Every sample must be tested and runnable before publishing |

---

## Skimming-First Design

Research: readers scan ~28% of words in an F-pattern.

- Put the most important sentence **first** in every section
- Use H2/H3 headings for every major topic
- Use bullet lists for 3+ items
- Use tables for comparisons and parameter lists
- Use code blocks instead of prose descriptions

---

## Friction Log Technique

Before writing, role-play as the user from zero:
1. Follow the process yourself from scratch
2. Note every moment of confusion, delay, or error
3. Each friction point = a gap to fill in the doc

---

## Plussing — Review Culture

Never say "this is wrong." Say:
> "This would be clearer if we added an example of X."

Rules: critique the doc, not the author. Every criticism must include a concrete suggestion.

---

## AI-Readiness Checklist

- [ ] Clear heading hierarchy (H1 → H2 → H3)
- [ ] Standard Markdown — no custom syntax
- [ ] Stable URL structure (`/docs/api/v2/auth`)
- [ ] Accurate, working code examples
- [ ] `AGENTS.md` in repo root if directing AI crawlers

---

## Deprecation Process

Never delete docs immediately when a feature is removed:
1. Mark as **Deprecated** with a banner
2. Add Migration guide
3. State the removal date
4. Remove only after the date passes
