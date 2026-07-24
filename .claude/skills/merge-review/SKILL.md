---
name: merge-review
description: "Review a feature branch for a clean, low-impact merge into main. Use when: reviewing a submitted branch or PR before merging, assessing merge conflicts and impact, planning how to integrate a feature branch into main, pre-merge quality gate."
argument-hint: "Branch name to review (e.g. character-creation-rework)"
---

# Merge Review — Branch-to-Main Integration Review

Review the changes on a feature branch and produce a vetted plan to merge them into `main` cleanly and with minimal impact. The target branch is supplied as the argument; default to `main` as the integration branch unless told otherwise.

This skill orchestrates a five-phase workflow that combines a mergeability assessment with two passes of the `dotnet-code-review` skill (Code Review Mode on the diff, then Consulting Mode on the merge plan), ending with a plan presented for the user's final sign-off.

## When to Use

- A branch or PR needs to land in `main`
- You need to know whether a branch merges cleanly and what it touches before integrating
- You want a reviewed, low-risk integration plan rather than a blind merge

## Inputs

- **Branch** (required argument): the feature branch to review. If none is given, ask which branch.
- **Integration target**: `main` by default. Override only if the user names a different base.

## Procedure

### Phase 1: Mergeability & Impact Assessment

Establish the facts about the branch relative to the integration target before judging the code.

1. Confirm the branch exists and fetch the latest refs (`git fetch`). Identify the merge base between the branch and `main`.
2. Determine divergence: commits ahead/behind, and whether `main` has moved since the branch was cut.
3. List the full diff scope: `git diff --stat main...<branch>` — every file changed, added, deleted, with line counts.
4. Detect merge conflicts without touching the working tree: prefer `git merge-tree` (dry-run); record every conflicting file and region.
5. Classify impact by blast radius:
   - **Files touched** by logical layer (Domain/logic — `Models/`, `Helpers/`, `Data/`; Services — `Services/`, `Services/Storage/`; UI — `Pages/`, `Shared/`, `Layout/`; Tests) and by feature area.
   - **Shared/core files** changed (`Models/` — persisted shapes ripple to every saved sheet, `Services/`, `Program.cs`, DI registrations, shared components/layout) — higher risk.
   - **Public API / signature changes** that ripple to callers outside the branch's feature.
   - **Static rules-data changes** (`Data/Occupations.cs`, `Data/DefaultSkills.cs`) — are the values still rules-faithful per `references/rules_condensed/` and the `rules-review` skill? A wrong number silently corrupts every character.
   - **Persistence-shape changes** (models reachable from `Investigator`/`Roster`) — will existing saved characters still deserialize via `ICharacterStore`? Watch for renamed/retyped properties, non-nullable additions without defaults, and removed enum values.
6. Summarize: is the branch behind `main`? Are there conflicts? Is the blast radius contained to one feature or does it reach across systems?

### Phase 2: Code Review (dotnet-code-review, Code Review Mode)

Run the `dotnet-code-review` skill in **Code Review Mode** against the branch's changes (the `main...<branch>` diff scope from Phase 1). This produces the full aggregated specialist report.

Treat its **Critical Issues** as merge blockers and its **Warnings** as things the plan must address or consciously defer.

If the diff touches rules-bearing logic or data (`Models/`, `Helpers/`, `Data/`), optionally invoke the `rules-review` skill as an additional pass to catch rules-fidelity issues that the code-quality specialists would miss.

### Phase 3: Build the Merge Plan

Synthesize Phase 1 (mergeability/impact) and Phase 2 (code review) into a concrete, ordered integration plan covering:

1. **Pre-merge prep** — rebase/merge `main` into the branch first if it is behind; how each conflict from Phase 1 should be resolved (and who decides if it's ambiguous).
2. **Blockers to fix before merge** — every Critical issue from the code review, with the specific change required.
3. **Conflict resolution** — file-by-file strategy for any conflicts, with special care for DI registrations and shared components.
4. **Merge mechanics** — merge vs. squash vs. rebase, target branch, and any sequencing.
5. **Post-merge follow-ups** — Warnings/Suggestions deferred to follow-up work, plus any `CLAUDE.md` architecture-notes updates the change requires.
6. **Verification** — what must pass to confirm the merge is clean: `dotnet build` (0 warnings/0 errors), `dotnet test`, and the app running locally (`dotnet run`).

Write the plan as a clear, ordered document so it can itself be reviewed.

### Phase 4: Plan Review (dotnet-code-review, Consulting Mode)

Run the `dotnet-code-review` skill in **Consulting Mode** against the merge plan from Phase 3. Fold the resulting **plan remediations** back into the plan so what you present is already vetted.

### Phase 5: Present for Final Sign-off

Present the final, consulting-vetted plan to the user for approval. Do **not** start implementing the merge until they sign off. Include:

- A one-line mergeability verdict (clean / conflicts / behind `main`).
- The blast-radius summary from Phase 1.
- The code-review verdict and any merge blockers.
- The ordered merge plan, with remediations from Phase 4 already incorporated.
- A clear ask: approve as-is, adjust, or hold.

## Notes

- This is a **review-and-plan** skill, not an auto-merge. Leave the working tree and branches untouched except for read-only inspection, unless the user approves implementation.
- Both review passes reuse the `dotnet-code-review` skill verbatim — do not re-derive its checklists here; invoke it in the correct mode.
- If the branch is a GitHub PR, use `gh` to pull PR metadata (description, linked issues, CI status) to enrich Phase 1.
