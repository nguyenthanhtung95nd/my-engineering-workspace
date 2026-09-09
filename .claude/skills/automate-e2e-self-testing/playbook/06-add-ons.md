# 06 - Add-ons (opt-in)

Offer these after the spine is green; install only on request. Some require org/IT approval -- marked
**GATED**. Never auto-install a gated tool; flag it and prefer a local-first or approved alternative.

## Visual regression (local-first)
The only loop that checks *appearance* -- the bug class agents introduce most (element works, layout
broke). Commit baselines and treat diffs as a channel:
```ts
await expect(page).toHaveScreenshot('list.png');
// config: expect: { toHaveScreenshot: { animations: 'disabled', caret: 'hide', scale: 'css', maxDiffPixelRatio: 0.01 } }
```
Stabilize four flake sources: pinned Linux/Docker image (fonts), animations disabled, dynamic content
masked or a fixed clock, scrollbars. A `/design-system` route screenshot covers the whole component set in
one line. Never update a baseline "to make it pass" -- verify intent, and put the baseline swap in the same
commit. Feed the diff back via an agent failure hook that points at the newest `*-diff.png` (hooks pass
text, not image bytes).

## Accessibility gate (local-first)
Semantic locators are upstream pressure, not proof -- add a dedicated scan so "probably fine" cannot ship:
```ts
import AxeBuilder from '@axe-core/playwright';
const { violations } = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
expect(violations).toEqual([]);   // new builder per test; withTags scopes the ruleset
```
Complement with `toMatchAriaSnapshot` for structural drift (dialogs, menus, nav). Keep a short manual
checklist for what automation cannot prove (focus order/trap/return, announcement usefulness, keyboard
equivalents). Treat `violations` as blocking, `incomplete` as a human-review queue; scope any suppression
with a written reason.

## Performance budgets (local-first)
Agents ship "works, tests pass, but slower/heavier." Enforce two pre-declared numbers: client bundle size
(a bundler visualizer emitting `raw-data` JSON, gated on an env flag) and one runtime measurement of a
critical route/interaction on a **production-like preview** (never the dev server). Store thresholds in
VCS, seeded at the current green baseline plus a small buffer; the checker exits non-zero on breach. Never
raise a budget silently -- a change goes in the commit message. Skip heavyweight audits until the cheap
loop is stable.

## Automated review -- second opinion (GATED: external SaaS)
A different model with no stake in the change passing catches "wait, what?" smells (missing null checks,
auth holes, swallowed errors) the author is too close to see. Requires an approved review service. Tune it
with a repo-committed instructions file split into "what to flag" / "what to leave alone" (exclude
generated code, migrations, fixtures, lockfiles, docs) plus a directive tone ("name the line, suggest a
fix, no 'consider', say nothing if nothing is worth flagging"). Keep a vendor-neutral
`docs/review-loop-playbook.md` (severity buckets Blocking / Judgment / Noise; re-review rule) so the loop
survives tool churn. **Rule of three:** a finding recurring across 3 PRs becomes an agent rule, then a
lint/type constraint, then a test assertion. A review bot is not a test replacement and is bad at deep
domain reasoning.

## Runtime probes & custom MCP (GATED: MCP server)
Have the agent poke the running UI between edits instead of waiting for a human: navigate -> request an
accessibility snapshot of the changed region (if the new element is absent, that itself is a bug) ->
interact -> re-observe console/network -> fix before reporting done. Step 4 (re-observe) is the one agents
skip -- encode it in the instructions file, scoped to UI paths. When generic prebuilt tools force the agent to
compose many primitives (and it gets one subtly wrong ~1-in-5), wrap a single-purpose stdio MCP server that
returns a typed JSON result for one repo-specific check; register it repo-locally in `.mcp.json`. Never log
to stdout (it corrupts the protocol -- use stderr). All of this runs through a gated MCP tool.

## AI test agents (GATED: MCP / AI)
Planner -> generator -> healer agents can plan, write, and self-heal the suite from inside the coding
loop, turning verification into something the agent maintains. They faithfully implement a wrong plan --
review the generated specs yourself, and back up `.mcp.json` before scaffolding (init overwrites it).
Requires org approval for the MCP/AI tooling.
