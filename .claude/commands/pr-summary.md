---
allowed-tools: Read(*), Bash(git diff*), Bash(git log*)
description: Generate a PR description from git diff and commit history. Surfaces any findings from /code-review or /security-review if they ran in this session. Use as the final step before raising a PR.
---

# PR Description Generator

Generates a ready-to-paste PR description from the current branch's changes.
Does **not** push or open the PR.

## Instructions

1. Run `git diff main...HEAD --stat` to understand the change scope.
2. Run `git log main...HEAD --oneline` to read commit messages and infer intent.
3. Read each changed file to understand what was modified and why.
4. If `/code-review` or `/security-review` ran earlier in this session, surface their Critical and High findings in "Review notes".
5. Write the PR body using the format below.

## PR Title Format

Use conventional commit format:
```
type(scope): short description
```

Common types: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`

Examples:
- `feat(products): add soft delete with IsActive flag`
- `fix(orders): prevent null reference when description is missing`
- `refactor(auth): extract JWT validation into middleware`

## PR Body Format

```markdown
## Summary
- <what changed — 1 sentence>
- <why it was needed — 1 sentence>
- <any notable decision made — optional>

## Changes
- `path/to/File.cs` — <what changed and why>
- `path/to/FileTests.cs` — <what tests were added or updated>

## Review notes
<If /code-review or /security-review found Critical or High issues, list them here with their fix status.>
<If both were clean: "No blocking issues found by /code-review or /security-review.">
<If neither ran: "Run /code-review and /security-review before merging.">

## Test plan
- [ ] `dotnet test` passes locally — zero failures
- [ ] <specific scenario to verify manually>
- [ ] <integration test scenario if DB or external API is involved>

## Risks
<Anything reviewers should pay extra attention to. Examples: schema change, breaking API contract, auth logic touched.>
<"None" if no risks identified.>
```

## Output

Print the PR title on the first line, then the PR body.
The output is ready to paste — do not add explanations around it.
