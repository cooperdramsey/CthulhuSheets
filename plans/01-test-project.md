# Add a Test Project for the Rules Engine — Implementation Plan

> Item #1 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 1.

## Goal

Stand up a unit-test project covering the app's deterministic rules logic — derived-stat
formulas, dice mechanics, skill rules, occupation skill-point math — plus a data
cross-validation suite that fails the build when the static game data (`Occupations.cs`,
`DefaultSkills.cs`) drifts out of sync or contains a typo. The goal is to convert the
currently-manual "does this formula still match the book" check into an automated,
one-time-cost guarantee, so future changes to any rules formula are verified by `dotnet
test` rather than by hand.

## Requirements (as given)

From the analysis document, item #1:

> There are no tests in the solution, yet a large share of the code is exactly what unit
> tests are best at: deterministic rules math. `CharacteristicHelper.cs`, `SkillRules.cs`,
> `Occupation.ComputeSkillPoints`, and `DiceRollService.RollPercentile` are all pure or
> near-pure today — testable with no refactoring. Two especially high-value cases: a data
> cross-validation test (every skill name in `Occupations.cs` exists in `DefaultSkills.All`;
> same for `SkillPointFormula` characteristic strings); and the damage-bonus/build boundary
> values, where an off-by-one is invisible until someone's character is wrong.

## Decisions (resolved via clarification)

> **Note:** the user was unavailable during this planning session and asked that clarifying
> questions be recorded rather than asked live. The questions below were resolved with the
> defaults marked **[DEFAULT]**; revisit any before implementation if the default is wrong.

1. **Test framework — xUnit vs NUnit vs MSTest.**
   **[DEFAULT] xUnit.** It is the de-facto standard for modern .NET, is what `dotnet new
   xunit` scaffolds, and has first-class support in the .NET 10 SDK already installed
   (10.0.301). No project convention exists yet to honor, so pick the mainstream default.

2. **Assertion style — plain xUnit `Assert` vs FluentAssertions.**
   **[DEFAULT] plain xUnit `Assert`.** Zero extra dependencies, and FluentAssertions'
   licensing changed to commercial for v8+. Keep the dependency surface minimal. If the
   user later wants more readable assertions, `Shouldly` (MIT) is the drop-in to add.

3. **How to test `DiceRollService`, which uses a non-injectable `private readonly Random`.**
   **[DEFAULT] Test the observable invariants that hold for *any* RNG seed**, not exact
   sequences: value ranges (`Roll(sides)` ∈ [1,sides]; percentile ∈ [1,100]), the
   bonus-die-keeps-lowest / penalty-die-keeps-highest relationship by rolling many times and
   asserting the statistical/structural property, and the "00 tens + 0 units = 100" edge via
   the `Combine` behavior. Do **not** refactor `DiceRollService` to inject `Random` as part
   of *this* item — that is a code change with save/behavior surface and belongs to its own
   task. Record it as a follow-up (see Open risks). Where a test genuinely needs
   determinism, run enough iterations that the invariant is exercised across the full digit
   space (e.g. loop 0–9 is impossible to force without seeding, so assert over N=10000 runs).

4. **Scope of "rules engine" for this first test pass.**
   **[DEFAULT] Cover the pure/near-pure logic reachable *without* refactoring:**
   `CharacteristicHelper` (`RecomputeDerived` band tables, `TryImproveEducation` given a
   seedable dice double is not available — see below, `RollLuck`/`RollPlacePool` ranges),
   `SkillRules.ShouldMarkExperienceCheck` + `NonImprovableSkills`, `Occupation.Evaluate` /
   `ComputeSkillPoints`, `Skill`/`Characteristic` derived properties (Half/Fifth rounding),
   `DiceRollService`, and the data cross-validation suite. The creation-step and
   `ImproveSkills` state machines are **explicitly out of scope here** — they are not unit
   testable until item #2 extracts them; this plan notes the seam but does not test them.

5. **`TryImproveEducation`, `RollLuck`, `RollPlacePool`, and everything that takes a
   `DiceRollService` — how to make deterministic?**
   **[DEFAULT] Introduce a minimal seam without changing production behavior:** these helper
   methods take a concrete `DiceRollService`. The lowest-risk way to get determinism is to
   make `DiceRollService` accept an optional `Random` via a new constructor overload
   (`public DiceRollService(Random? random = null)`), defaulting to `new()` exactly as today
   — a purely additive change with no behavioral or serialization impact. Tests then pass a
   `new DiceRollService(new Random(seed))`. This is the one small production edit this plan
   permits, because it is additive and unblocks deterministic testing of every
   dice-consuming helper. **Question for user:** is adding a seedable constructor overload to
   `DiceRollService` acceptable, or should tests stay purely statistical/range-based and
   leave the class untouched? Planned assuming **yes, add the overload** (marked in steps).

6. **Where does the test project live and is it added to the solution?**
   **[DEFAULT] `CthulhuSheets.Tests/` at repo root, added to `CthulhuSheets.slnx`.** Mirrors
   the standard `<Project>/<Project>.Tests/` layout. It references the app project
   (`CthulhuSheets.csproj`). Note the app is `Microsoft.NET.Sdk.BlazorWebAssembly` targeting
   `net10.0`; the test project is a plain `Microsoft.NET.Sdk` targeting `net10.0` and
   references the WASM app project — this is supported (the referenced types are ordinary
   .NET types; no browser/JS runtime is exercised by these tests).

7. **CI wiring.**
   **[DEFAULT] Note it, don't build it here.** The repo has `.github/workflows/static.yml`
   (GitHub Pages deploy). Adding a `dotnet test` CI gate is valuable but is a separate
   concern from creating the test project; this plan produces the runnable tests and flags CI
   as a fast follow (see Open risks). **Question for user:** want CI added in this item or as
   its own?

## Alternatives considered

- **No new project; put tests in a folder inside the app.** Rejected — a WASM app project
  shouldn't carry a test runner; keeping tests in a separate SDK-style project is standard
  and keeps the published app payload clean.
- **Refactor `DiceRollService` to fully inject an `IRandom` abstraction now.** Rejected for
  this item — larger surface than needed; the additive optional-`Random` constructor gets
  determinism with near-zero risk. A full abstraction can come with item #2 if wanted.
- **Test the creation-step state machines directly via bUnit (component testing).** Rejected
  for now — those live in component code-behind and aren't cleanly unit-testable until item
  #2 extracts them. bUnit is the right tool *after* extraction, or for a later UI-test item.
- **FluentAssertions for readability.** Rejected on licensing/dependency grounds (see
  decision #2).

## Assumptions

- Adding an additive `public DiceRollService(Random? random = null)` constructor is
  acceptable (decision #5). If not, the affected dice-consuming tests fall back to
  range/statistical assertions only.
- `net10.0` test project referencing the WASM app project builds and runs under the
  installed SDK 10.0.301 (expected to be fine; verified at step 1).
- The static data in `Occupations.cs`/`DefaultSkills.cs` is intended to be internally
  consistent (every occupation skill and formula characteristic is valid) — so a
  cross-validation failure indicates a real bug, not an intended exception. If any
  occupation legitimately references a skill *not* in the default list (e.g. a
  specialization the default list omits), that becomes an allowed-exceptions set in the test.

## Rules touched

These tests assert the code matches the following 7e rules. Source: `references/rules_condensed/`.

- **Damage Bonus / Build** (`ch_3_creating_investigators.md`, STR+SIZ table): 2–64 →
  −2/−2; 65–84 → −1/−1; 85–124 → 0/0; 125–164 → +1D4/+1; 165–204 → +1D6/+2; 205–284 →
  +2D6/+3; 285–364 → +3D6/+4; 365–444 → +4D6/+5; 445–524 → +5D6/+6. The code's bands in
  `CharacteristicHelper.RecomputeDerived` (`<=64`, `<=84`, `<=124`, `<=164`, `<=204`,
  `<=284`, `<=364`, `<=444`, else) must reproduce these boundaries exactly — the highest-
  value assertions are the band *edges* (64/65, 84/85, 124/125, …).
- **HP** = (SIZ + CON) ÷ 10, rounded down (`ch_3`). **MP** = POW ÷ 5 (`ch_3`). **Starting
  SAN** = POW (`ch_3`/`ch_8`).
- **MOV** (`ch_3`): base 8; STR and DEX both > SIZ → 9; STR and DEX both < SIZ → 7;
  otherwise 8. Age penalties: 40s −1, 50s −2, 60s −3, 70s −4, 80s −5.
- **EDU improvement check** (`ch_3`): roll D100; if result > current EDU, add 1D10, max EDU
  99. `CharacteristicHelper.TryImproveEducation`.
- **Luck** = 3D6 × 5 (`ch_3`). `CharacteristicHelper.RollLuck` → range [15,90].
- **Place Rolls pool** (`ch_3` Option 3): 5× (3D6 × 5) + 3× ((2D6+6) × 5).
  `CharacteristicHelper.RollPlacePool` → 8 values, each in the correct per-die range.
- **Point Buy** (`ch_3` Option 4): 460 points, each characteristic 15–90.
  `CharacteristicHelper.PointBuyTotal/Min/Max` constants.
- **Bonus / Penalty dice** (`ch_5_game_system.md`): one units die + N tens dice sharing the
  units digit; bonus keeps the **lower** tens, penalty keeps the **higher**; a tens of 00
  with units 0 reads as 100. `DiceRollService.RollPercentile`.
- **Skill improvement / experience checks** (`ch_4_skills.md`, `ch_5`): a skill is ticked
  only on a successful roll (roll ≤ value) with no bonus die; Credit Rating and Cthulhu
  Mythos are **never** ticked. `SkillRules.ShouldMarkExperienceCheck` +
  `NonImprovableSkills`.
- **Half / Fifth** (`ch_4`): integer division by 2 and 5, rounded down. `Skill.Half/Fifth`,
  `Characteristic.Half/Fifth`.
- **Occupation skill points** (`ch_3`): sum of `characteristic × multiplier` formulas, plus
  one chosen option formula where the occupation offers "either X×2 or Y×2".
  `Occupation.ComputeSkillPoints`/`Evaluate`.

## Affected code

New:
- `CthulhuSheets.Tests/CthulhuSheets.Tests.csproj` — xUnit test project, `net10.0`,
  references the app project.
- `CthulhuSheets.Tests/CharacteristicHelperTests.cs` — DB/Build band edges, HP/MP/SAN/MOV,
  EDU improvement, Luck/PlacePool ranges.
- `CthulhuSheets.Tests/DiceRollServiceTests.cs` — Roll range, percentile range,
  bonus/penalty relationship, the 00/0 = 100 edge.
- `CthulhuSheets.Tests/SkillRulesTests.cs` — tick/no-tick truth table, non-improvable
  guard.
- `CthulhuSheets.Tests/OccupationTests.cs` — `Evaluate`/`ComputeSkillPoints` including the
  chosen-option formula.
- `CthulhuSheets.Tests/ModelDerivedValueTests.cs` — `Skill` and `Characteristic`
  Half/Fifth/EffectiveRegular rounding.
- `CthulhuSheets.Tests/GameDataConsistencyTests.cs` — the cross-validation suite
  (Occupations ↔ DefaultSkills; formula characteristic strings valid).

Changed (additive only):
- `CthulhuSheets/Services/DiceRollService.cs` — add `public DiceRollService(Random? random =
  null)` overload defaulting to `new()`; existing parameterless behavior unchanged (decision
  #5). **Only if decision #5 is approved.**
- `CthulhuSheets.slnx` — add the test project.
- `.gitignore` — ensure test `bin/`/`obj/` and any coverage output are ignored (they are,
  via existing patterns; verify).

No persisted-model changes. Nothing reachable from `Investigator`/`Roster` changes shape, so
there is **no saved-character compatibility concern** for this item.

## Implementation steps

1. **Scaffold the test project — and treat the app-reference as a go/no-go gate.**
   `dotnet new xunit -n CthulhuSheets.Tests -o CthulhuSheets.Tests` at repo root. Set
   `<TargetFramework>net10.0</TargetFramework>`. Add `<ProjectReference
   Include="..\CthulhuSheets\CthulhuSheets.csproj" />`. Add the project to
   `CthulhuSheets.slnx`. **Verify:** `dotnet build` succeeds and `dotnet test` runs (0 tests
   or the template test passes); delete the template `UnitTest1.cs`.
   **Go/no-go:** referencing a `Microsoft.NET.Sdk.BlazorWebAssembly` project from a plain
   test SDK project normally works because the types under test (`CharacteristicHelper`,
   `DiceRollService`, `Occupation`, `Skill`, `DefaultSkills`, `Occupations`) are ordinary
   .NET types and no JS/browser runtime is invoked. **If** the reference fails to build (e.g.
   a transitive `MudBlazor`/`Magic.IndexedDb` asset conflict), fall back to: extract the
   pure rules types (`Helpers/`, `Models/`, `Data/`) into a new
   `CthulhuSheets.Core` class-library (`net10.0`, no Blazor/Mud deps) that both the app and
   the tests reference. That extraction is a larger change — do **not** do it preemptively;
   only if step 1 proves the direct reference unworkable. Record which path was taken.

2. **(If decision #5 approved) Add the seedable constructor to `DiceRollService`.**
   Add `public DiceRollService(Random? random = null) { _random = random ?? new(); }` and
   make `_random` assigned from it; keep all existing method bodies identical. **Verify:**
   app still builds; the existing DI registration (`AddScoped<DiceRollService>()` in
   `Program.cs`) still resolves via the parameterless path (optional param → still a usable
   default ctor for DI).

3. **`ModelDerivedValueTests` — the cheapest, highest-certainty tests first.**
   Assert `Characteristic.Half`/`Fifth` = value/2, value/5 (floor) and null when Regular is
   null; assert `Skill.EffectiveRegular` = `Regular ?? BaseValue`, and `Half`/`Fifth` derive
   from `EffectiveRegular`. Include a rounding case (e.g. Regular 55 → Half 27, Fifth 11).
   **Verify:** `dotnet test` green. Satisfies `ch_4` half/fifth.

4. **`CharacteristicHelperTests` — DB/Build band edges (the flagship case).**
   Table-driven (`[Theory]`/`[InlineData]`) over every band boundary pair: STR+SIZ of 64 →
   ("-2",−2) and 65 → ("-1",−1); 84/85; 124/125; 164/165; 204/205; 284/285; 364/365;
   444/445; and one in the top band (e.g. 500 → ("5d6",6)). Drive via an `Investigator` with
   STR/SIZ set so STR+SIZ hits each target, call `RecomputeDerived`, assert
   `DamageBonus`/`Build`. Also assert HP = (SIZ+CON)/10 floor, MP = POW/5, starting SAN =
   POW, and the MOV rules (both>SIZ→9, both<SIZ→7, mixed→8) with an age penalty case (age 55
   subtracts 2). **Verify:** green; these are the assertions that catch a silent off-by-one.

5. **`CharacteristicHelperTests` — dice-consuming helpers (needs step 2's seam).**
   With a seeded `DiceRollService`, assert `RollLuck` ∈ [15,90]; `RollPlacePool` returns 8
   values with the first 5 in [15,90] (3D6×5) and last 3 in [40,90] ((2D6+6)×5 → min
   (2+6)×5=40, max 90). **Do NOT assert the book's "recommended min 40 for INT and SIZ"
   (`ch_3` Option 3) — that is a table-side *recommendation* to the player, not a rule the
   code enforces or should; baking it into a test would be a false expectation.**
   `TryImproveEducation` never exceeds EDU 99 and only improves when
   the rolled value > current (assert both an improve and a no-improve outcome by choosing
   seeds/among many iterations). If decision #5 is rejected, downgrade these to N-iteration
   range assertions. **Verify:** green.

6. **`DiceRollServiceTests`.**
   `Roll(sides)` ∈ [1,sides] over many iterations for sides ∈ {2,4,6,10,20,100}; `Roll`
   never returns 0 or sides+1. `RollPercentile()` ∈ [1,100]. Bonus/penalty: over N=10000
   iterations, `RollPercentile(+1)` and `(+2)` results are ≤ what a comparable normal path
   would tend to (assert the *structural* rule via the exposed `Expression` string listing
   candidates and that the chosen `Total` equals the min for bonus / max for penalty — the
   expression already contains the candidate list, so parse it and assert Total == Min/Max of
   candidates). Assert the 100 edge appears (over enough iterations a 00-tens/0-units occurs
   → Total 100 present). **Verify:** green. Satisfies `ch_5` bonus/penalty.

7. **`SkillRulesTests`.**
   Truth table for `ShouldMarkExperienceCheck`: success + modifier ≤ 0 + not-yet-checked +
   improvable → true; failure → false; modifier > 0 (bonus die) → false; already checked →
   false; Credit Rating / Cthulhu Mythos (any case) → false. Assert `NonImprovableSkills`
   contains exactly those two, case-insensitively. **Verify:** green. Satisfies `ch_4`/`ch_5`
   ticking.

8. **`OccupationTests`.**
   `Evaluate` maps each of the 8 characteristic strings to `Regular × multiplier` and unknown
   → 0. `ComputeSkillPoints` sums the fixed formulas and adds the chosen option when
   supplied; equals fixed-only when option is null. Use a small hand-built `Occupation` +
   `Investigator` with known characteristics so the arithmetic is checkable by hand (e.g.
   EDU 60 × 4 = 240; Artist EDU 60×2 + POW 50×2 = 220). **Verify:** green. Satisfies `ch_3`
   occupation points.

9. **`GameDataConsistencyTests` — the drift guard.**
   (a) Every skill name listed in every `Occupations.All[*].Skills` exists in
   `DefaultSkills.All` (case-insensitive), **except** an explicit allowed-exceptions set for
   any legitimately-non-default entries (start empty; populate only if a real exception is
   found — a match failure here is the bug we want). (b) Every
   `SkillPointFormulas`/`SkillPointFormulaOptions` `Characteristic` string is one of the 8
   valid abbreviations (STR/CON/SIZ/DEX/APP/INT/POW/EDU) — i.e. none would silently evaluate
   to 0. (c) `CreditRatingMin ≤ CreditRatingMax` for every occupation, and each is in
   [0,99]. (d) No duplicate skill names within a single occupation, and each occupation has
   ≤ 8 named skills. **Verify:** green *or* a real data bug is surfaced (see Open risks — the
   `>= 80 and <= 90` age-band boundary vs the book's 80–89 is a candidate the DB/MOV tests or
   a dedicated age-band test may flag; if in scope, add an age-band boundary test asserting
   the code's brackets are contiguous and cover 15–90 with no gaps/overlaps).

10. **README/CLAUDE note (optional, low cost).**
    Add a one-line "Run tests: `dotnet test`" to the repo README so the suite is
    discoverable. **Verify:** n/a.

## Testing / verification

- `dotnet test` from repo root runs the full suite green (or red only where it has found a
  genuine data/formula bug, which is then filed).
- `dotnet build` of the solution (app + tests) succeeds.
- Spot-check that the DB/Build band test actually fails if a boundary is edited (mutate one
  band edge locally, confirm red, revert) — proves the test has teeth.
- Confirm the app still runs (`dotnet run --project CthulhuSheets`) after the additive
  `DiceRollService` change, if applied.

## Open risks

- **Decision #5 (seedable `Random`)** is the one production edit; if the user rejects it, the
  dice-consuming helper tests degrade to range/statistical-only. Flagged for approval.
- **The data cross-validation suite may go red on first run** — that's success, not failure
  (it means it found a real drift). Any failures must be triaged as either a data bug to fix
  or a legitimate exception to allow-list, *with the user*, before the suite is considered
  passing.
- **Possible real bug to confirm:** the age bracket in
  `CreationCharacteristicsStep.GetBracket` uses `>= 80 and <= 90` where the rules table lists
  80–89 (age cap is 90). This is out of scope to *fix* here (it's creation-step code, item
  #2 territory) but a boundary test could document it. Recorded as a follow-up.
- **CI gate not included** (decision #7). Adding `dotnet test` to a CI workflow is a
  recommended fast follow so the suite actually guards `main`.
- **`DiceRollService` statistical tests** must use enough iterations to be non-flaky but not
  slow; N≈10000 is fine (microseconds). Keep them deterministic in *outcome* (asserting
  invariants that hold for all seeds), never asserting a specific random value.
