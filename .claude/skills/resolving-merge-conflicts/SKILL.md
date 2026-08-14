---
name: resolving-merge-conflicts
description: >
  Resolves an in-progress git merge or rebase conflict by understanding each
  change's original intent, preserving both sides where possible, running the
  project's checks, and completing the merge/rebase. Use when a merge or rebase
  is mid-conflict (conflict markers present, or git reports unmerged paths).
---

# Resolving Merge Conflicts

Resolve an in-progress merge/rebase deliberately — understand intent first,
never guess, always finish. **Never run `git merge --abort` or `git rebase --abort`.**

## 1. See the current state

- `git status` — merge vs rebase, and which paths are unmerged.
- `git log --oneline --left-right --merge` — the commits on each side.
- For a rebase, note which commit is currently being applied (`git status` shows it).
- Open each conflicting file and read every `<<<<<<< / ======= / >>>>>>>` hunk.

## 2. Find the primary source of each conflict

For each conflicting hunk, understand **why** each side made its change before touching it:

- `git log -p --follow <file>` and `git blame` on the conflicting lines, both sides.
- Read the commit messages behind each side (`git show <sha>`).
- Check the associated PR / issue / ticket where discoverable (`gh pr list`,
  `gh pr view`, commit trailers) for the original intent.
- Do not proceed on a hunk until you can state, in one sentence, what each side wanted.

## 3. Resolve each hunk

- **Preserve both intents** where the changes are compatible — combine them.
- Where they are genuinely **incompatible**, pick the side matching the merge's
  stated goal (the branch/PR being merged) and note the trade-off in your summary.
- **Do not invent new behaviour** — only reconcile what each side already did.
- Remove all conflict markers. Re-read the resolved region for correctness and for
  anything the merge silently broke (duplicated imports, dropped edits, stale calls).
- Apply the project's engineering rules (`@.claude/rules/`) to the resolved code.

## 4. Run the project's automated checks

Detect the stack from indicator files at the repo root, then run checks in order —
**typecheck/build → tests → format** — and fix anything the merge broke:

| Indicator | Build / typecheck | Test | Format |
|-----------|-------------------|------|--------|
| `*.csproj` / `*.sln` | `dotnet build` | `dotnet test` | `dotnet format` |
| `package.json` | `npm run build` (or `tsc --noEmit`) | `npm test` | `npm run format` / prettier |
| `go.mod` | `go build ./...` | `go test ./...` | `gofmt -w` |
| `Cargo.toml` | `cargo build` | `cargo test` | `cargo fmt` |
| `pyproject.toml` | — | `pytest` | `ruff` / `black` |

For .NET, locate the correct `.sln`/`.csproj` before running. Loop until every check is clean.

## 5. Finish the merge/rebase

- Stage everything: `git add -A`.
- **Merge:** commit to conclude it (`git commit --no-edit` keeps the merge message).
- **Rebase:** `git rebase --continue`, then return to step 1 for the next conflicting
  commit. Repeat until all commits are rebased and the rebase reports done.
- Never leave the repository in a mid-merge/mid-rebase state.

## Summary to report

- Files resolved and, per incompatible hunk, which side won and the trade-off.
- Check results (build/test/format: pass/fail).
- Merge/rebase completion status.
