# Extract Creation-Step Rule State Machines — Implementation Plan

> Item #2 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 1.
> **Depends on / pairs with [01-test-project.md](01-test-project.md)** — the whole point of
> extraction is to make these state machines unit-testable, so do this before (or alongside)
> writing their tests.

## Goal

Move the rules **state-machine logic** out of the character-creation component code-behinds
(`CreationCharacteristicsStep.razor.cs`, 515 lines; `CreationOccupationSkillsStep.razor.cs`,
412 lines) and out of `SkillsTab.ImproveSkills`, into plain, injectable, unit-testable C#
classes. The components become thin binding shells that call into the extracted sessions;
the trickiest rules in the app (point-buy budget, place-rolls pool, Modify Low Rolls, Human
Potential, age brackets + deductions, occupation/personal allocation with the 75% cap and
credit-rating bounds, the development-phase skill-improvement + 90% sanity bonus) become
testable in isolation. **No behavior change** — this is a pure refactor that preserves every
current rule and interaction exactly.

## Requirements (as given)

From the analysis, item #2:

> Most of their content isn't UI — it's rules state machines… Extracting these into plain
> classes (e.g. `CharacteristicGenerationSession`, `SkillAllocationSession`, a
> `DevelopmentPhase` helper next to `SkillRules`) pays three ways: the components shrink to
> thin bindings, the trickiest rules in the app become unit-testable, and future features get
> a place to live that isn't a 500-line partial class.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **How far to extract — logic-only vs. also move drag/drop UI state?**
   **[DEFAULT] Extract rules/allocation state and computation; leave pure-UI transient state
   in the component.** The `PoolDie.Zone`/`_draggedDie` drag mechanics are UI concerns; the
   *values* and their validity (which die is assigned to which stat, whether the point budget
   balances) are rules concerns. Draw the line so the session owns "what the numbers are and
   whether they're legal," and the component owns "how the user manipulates them
   (drag/drop, which stat is being edited)." The session exposes methods the drag handlers
   call (e.g. `AssignDie`, `ReturnToPool`); the component keeps `_draggedDie`.

2. **One session class or several?**
   **[DEFAULT] Three focused classes**, matching the three problem areas:
   `CharacteristicGenerationSession` (methods, add-ons, age), `SkillAllocationSession`
   (occupation + personal points, cap, CR bounds), and a `DevelopmentPhase` static helper
   (the `ImproveSkills` logic) placed next to `SkillRules` in `Helpers/`. Splitting the two
   large steps is natural; keeping development-phase as a static helper mirrors the existing
   `SkillRules`/`CharacteristicHelper` style.

3. **Where do the new classes live and what namespace?**
   **[DEFAULT] `CthulhuSheets/Creation/` for the two session classes**
   (`namespace CthulhuSheets.Creation`), and `DevelopmentPhase` in
   `CthulhuSheets/Helpers/` next to `SkillRules`. Rationale: sessions are creation-specific
   stateful objects (not stateless helpers), so they deserve their own folder rather than
   being lumped into `Helpers/`. **Question for user:** prefer `Helpers/` for all of them to
   minimize new folders, or a dedicated `Creation/` folder? Planned with `Creation/`.

4. **Do the sessions take a `DiceRollService` by constructor?**
   **[DEFAULT] Yes** — `CharacteristicGenerationSession` needs dice (roll all, place pool,
   low-roll 1D6, Human Potential 1D10, age EDU checks + Luck). Inject it via constructor so
   the session is testable with a seeded `DiceRollService` (see plan #1 decision #5). The
   component constructs the session in `OnParametersSet`/`OnInitialized`, passing the
   injected `Dice` and the `Investigator`.

5. **Is this allowed to change any observable behavior or rule?**
   **[DEFAULT] No.** This is a structural refactor. If the extraction *surfaces* a latent bug
   (e.g. the `>= 80 and <= 90` age-band edge vs. the book's 80–89, or a rounding quirk), do
   **not** fix it as part of this item — record it as a finding and fix it under a separate
   task with its own test. Keeping refactor and behavior-change separate keeps the diff
   reviewable and the git history honest.

6. **Testing added in this item, or in #1?**
   **[DEFAULT] This item makes the code testable and adds a smoke test per session; the
   comprehensive rules tests are #1's job.** Add at least one round-trip test per session
   proving the extraction preserved behavior (e.g. point-buy validity, an age bracket's full
   application, an allocation reaching the cap). The exhaustive coverage lives in #1's suite,
   which can now reach these classes.

## Alternatives considered

- **Leave logic in components, test via bUnit.** Rejected — bUnit tests are slower, more
  brittle, and force the rules assertions through rendering; the logic is plain arithmetic
  that deserves plain unit tests. Extraction is the enabling move.
- **One giant `CreationSession` class.** Rejected — it would be a 900-line class reproducing
  the two-step problem in one file. Three focused classes match the three cohesive rule
  areas.
- **Move logic into `CharacteristicHelper`/`SkillRules` as more static methods.** Rejected
  for the *stateful* parts — the generation and allocation flows carry mutable in-progress
  state (pools, allocations, pending deductions, base-value snapshots for reset). Static
  helpers fit stateless computation (`DevelopmentPhase` qualifies); the stateful flows want
  instance classes.
- **Do the whole thing as one big-bang PR.** Rejected — sequence it (characteristics session
  first, then allocation, then development-phase) so each is independently reviewable and the
  app stays green between steps.

## Assumptions

- No behavior change is intended or acceptable (decision #5). The definition of "done" is
  byte-for-byte identical creation outcomes for the same inputs/dice.
- The extracted classes reference only `Models/`, `Data/`, `Helpers/`, and `DiceRollService`
  — no MudBlazor/Blazor types — so they are trivially unit-testable and could later move to a
  `CthulhuSheets.Core` lib if plan #1's go/no-go forces one.
- The component's `Validate()` contract (called by `CharacteristicStep`/`OccupationStep`
  before advancing the stepper) is preserved: it delegates to the session's validity property
  and, for the occupation step, still calls `FinalizeSkills()`.

## Rules touched

This item must **preserve** (not redefine) these mechanics — the extracted code has to
reproduce them exactly. Sources: `references/rules_condensed/`.

- **Generation methods** (`ch_3` Options): Roll (3D6×5, (2D6+6)×5 for SIZ/INT/EDU); Place
  Rolls (5×3D6×5 + 3×(2D6+6)×5, assign freely); Point Buy (460 pts, each 15–90); Quick Fire
  (fixed array 80/70/60/60/50/50/50/40). Add-ons: Start Over (advisory, 3+ stats < 50);
  Modify Low Rolls (3+ pre-×5 totals < 10 → +1D6 spread, cap 90); Human Potential (+1D10,
  combinable, max 99).
- **Age bands** (`ch_3` table): EDU flat/checks, physical deductions and their target stats,
  APP reduction, double-Luck for 15–19; the EDU improvement check (D100 > current → +1D10,
  max 99); Luck 3D6×5. **Note:** the book's oldest band is **80–89** with age cap 90; the
  code currently models `>= 80 and <= 90` — preserve current behavior here and flag the
  discrepancy for a separate fix (decision #5).
- **Derived stats** (`ch_3`): via `CharacteristicHelper.RecomputeDerived` — unchanged, still
  called at the end of age application.
- **Occupation skill points** (`ch_3`): `Occupation.ComputeSkillPoints` + chosen option;
  **Personal Interest** = INT × 2. The **75% starting cap** is a Keeper option (on by
  default). Credit Rating must land within the occupation's min–max. Cthulhu Mythos gets no
  starting points; Credit Rating isn't allocated from the personal pool the same way (per
  current code).
- **Development phase** (`ch_5`): for each ticked skill roll 1D100; improve if roll > current
  **or** roll > 95; add 1D10 (may exceed 100). Credit Rating & Cthulhu Mythos never improve.
  Reaching **90%+** grants **+2D6 Sanity**, capped at **99 − Cthulhu Mythos** (`ch_8`).

## Affected code

New:
- `CthulhuSheets/Creation/CharacteristicGenerationSession.cs` — owns method selection, the
  dice pool + assignments, point-buy budget, Modify Low Rolls, Human Potential, age bracket
  selection + application + deductions, base-value snapshots for reset, and the derived
  readiness flags (`BaseStatsReady`, `StatsReady`, `DerivedReady`, `BaseLocked`, etc.).
- `CthulhuSheets/Creation/SkillAllocationSession.cs` — owns occupation selection/apply,
  custom-occupation build, the two allocation dictionaries, cap toggle + re-clamp,
  CR grand-total bounds, `GetMax*Allocation`, and `FinalizeSkills`.
- `CthulhuSheets/Helpers/DevelopmentPhase.cs` — the `ImproveSkills` logic as a pure(ish)
  method taking the investigator + dice, returning the list of `ImprovementResult`s and
  mutating skills/sanity per the rules.
- Smoke tests in the `CthulhuSheets.Tests` project from plan #1 (one per extracted unit).

Changed:
- `CreationCharacteristicsStep.razor.cs` — reduced to: hold a
  `CharacteristicGenerationSession`, expose its properties to the markup, forward UI events
  (drag handlers, method change) to it. `CharacteristicDef`/`AgeBracket`/`PoolDie` records
  move to (or are shared with) the session as appropriate.
- `CreationCharacteristicsStep.razor` — bindings repoint from `_field`/local methods to
  `_session.X`; **no visual/markup structure change**.
- `CreationOccupationSkillsStep.razor.cs` / `.razor` — same treatment with
  `SkillAllocationSession`.
- `SkillsTab.razor.cs` — `ImproveSkills` delegates to `DevelopmentPhase`; the
  `ImprovementResult` record can move next to it or stay (decide in step).

**No persisted-model changes.** The sessions operate on the same `Investigator` object and
write the same fields; nothing reachable from `Investigator`/`Roster` changes shape, so
**saved characters are unaffected**. (Confirm no accidental property renames during
extraction.)

## Implementation steps

> Sequence so the app builds and behaves identically after **each** step. Extract one unit at
> a time; do not start the next until the current one is green and manually smoke-tested.

1. **Extract `DevelopmentPhase` (smallest, lowest-risk first).**
   Create `Helpers/DevelopmentPhase.cs` with a method like
   `IReadOnlyList<ImprovementResult> Run(Investigator inv, DiceRollService dice)` that
   reproduces `SkillsTab.ImproveSkills` exactly: filter ticked & improvable skills; per skill
   roll 1D100, improve on `roll > current || roll > 95` with +1D10; on crossing into 90%+
   grant +2D6 Sanity capped at 99 − Mythos; clear the tick. Move/duplicate the
   `ImprovementResult` record to a shared location. Repoint `SkillsTab.ImproveSkills` to call
   it and `await PersistAsync()`. **Verify:** improve-skills flow in the running app behaves
   identically; add a `DevelopmentPhaseTests` smoke test (seeded dice → known improvement +
   sanity bump). Rule: `ch_5` development phase + `ch_8` SAN cap.

2. **Introduce `SkillAllocationSession` (occupation step) — construct but don't yet remove
   old code.** Create `Creation/SkillAllocationSession.cs` holding: `_selectedOccupation`,
   `_chosenFormulaOption`, `_occupationSkillNames`, custom-occupation fields, `_allocations`,
   `_personalAllocations`, `_capSkillsAt75`, and all the computed properties/methods
   (`OccupationSkillPoints`, `PointsRemaining`, `PersonalPointsRemaining`,
   `CreditRatingGrandTotal`, `IsAllocationValid`, `GetSkillBase`, `GetMaxAllocation`,
   `GetMaxPersonalAllocation`, `InitializeAllocations`, `InitializePersonalAllocations`,
   `ApplyOccupation`, `ClearOccupation`, custom-occupation build/sync, `OnCapToggled`,
   `FinalizeSkills`, `PopulateDefaults`). Take `Investigator` (and, if needed for defaults,
   nothing else) via constructor. **Verify:** compiles.

3. **Repoint `CreationOccupationSkillsStep` to the session; delete the moved code.**
   The `.razor.cs` keeps only `[Parameter] Investigator`, the `_session` field constructed in
   `OnParametersSet`, `Validate()` (delegates to `_session.IsOccupationConfirmed &&
   _session.IsAllocationValid`, then `_session.FinalizeSkills()`), the skill-filter UI state,
   and thin event forwarders the markup binds to. Update `.razor` bindings to `_session.X`.
   **Verify:** full occupation-step walkthrough in the app is behavior-identical
   (select occupation, allocate to the cap, hit CR bounds, custom occupation, personal
   interest, load defaults). Rule fidelity preserved: `ch_3` skill points, INT×2 personal,
   75% cap, CR range.

4. **Introduce `CharacteristicGenerationSession` — construct but don't yet remove old code.**
   Create `Creation/CharacteristicGenerationSession.cs` holding the method/add-on/age state
   and all computed flags and operations from `CreationCharacteristicsStep.razor.cs`:
   `ChangeMethod`, `RollAll`/`Roll`, place-pool roll + assignment operations
   (`AssignDie`/`ReturnToPool`/`ResetDiceToPool` — the value side of drag/drop), Point Buy
   setters + validity, Modify Low Rolls flow, Human Potential flow, age selection +
   `ApplyAgeModifiers` + deductions + `ResetAgeModifiers`, base-value snapshots, and the
   readiness flags. Inject `DiceRollService` via constructor. Keep `PoolDie`/drag identity in
   the component; the session exposes assignment by value/target. **Verify:** compiles.

5. **Repoint `CreationCharacteristicsStep` to the session; delete the moved code.**
   The `.razor.cs` keeps `[Parameter] Investigator`, `[Inject] DiceRollService`, the
   `_session` constructed in `OnParametersSet`, `_draggedDie` + drag handlers that call
   `_session` assignment methods, `Validate()` (delegates to `_session.DerivedReady`), and
   any pure-render helpers. Update `.razor` bindings. **Verify:** exhaustively walk every
   method (Roll incl. reroll/start-over/modify-low-rolls; Place Rolls drag/drop/reset; Point
   Buy budget; Quick Fire; Human Potential enable/roll/confirm/reset; every age bracket incl.
   deductions and reset) and confirm identical behavior. Rule fidelity: `ch_3` methods + age.

6. **Add per-session smoke tests** (in plan #1's project): point-buy validity + a full age
   bracket for the characteristic session; an allocation reaching cap + CR bound enforcement
   for the allocation session. **Verify:** `dotnet test` green.

7. **Sweep for accidental drift.** Diff the pre/post behavior mentally against each rule in
   "Rules touched"; grep for any property that got renamed; confirm no `Investigator` field
   is written differently than before. **Verify:** `git diff` shows only mechanical moves +
   binding repoints, no formula edits.

## Testing / verification

- After **each** extraction step, run the app (`dotnet run --project CthulhuSheets`) and walk
  the affected creation step end-to-end; outcomes must match pre-refactor exactly.
- `dotnet test` green (smoke tests + plan #1 suite if present).
- `git diff` review confirms no rule/formula/threshold constant changed value — only location.
- Create a character start-to-finish and confirm the saved JSON is identical in shape to a
  character created before the refactor (spot-check a few fields).

## Open risks

- **Scope creep into behavior changes.** The strongest risk is "fixing" something while
  moving it. Discipline: preserve exactly; log discrepancies (esp. the `80–90` age-band edge)
  as separate findings, don't fix here.
- **Drag/drop coupling.** The place-rolls drag mechanic is the trickiest UI/logic seam; if
  the value/identity split proves awkward, it's acceptable for the session to hold the pool
  list and the component to hold only `_draggedDie` — reassess at step 4.
- **Blazor re-render timing.** Moving state into a plain object the component holds is fine
  for rendering (the component still calls the session and re-renders), but confirm
  `StateHasChanged` still fires where needed (event handlers return to the component, which
  re-renders normally). No `@bind` should point *through* the session in a way that breaks
  two-way binding — expose plain get/set or explicit change methods.
- **This is a large diff.** Sequencing (steps 1→5, one unit at a time, green between each) is
  the mitigation; do not collapse into one commit.
