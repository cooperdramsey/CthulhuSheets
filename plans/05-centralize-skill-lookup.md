# Centralize Skill Lookup + Well-Known Skill Names — Implementation Plan

> Item #5 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 2.

## Goal

Replace the ~seven hand-written `Skills.FirstOrDefault(s => s.Name.Equals(name,
OrdinalIgnoreCase))` lookups and the retyped magic strings (`"Cthulhu Mythos"`, `"Credit
Rating"`, `"Dodge"`) with a single `Investigator.FindSkill(name)` extension plus a
`WellKnownSkills` constants class, and collapse the two independent implementations of the
max-sanity rule (`99 − Mythos`) into one helper. This makes rules drift structurally
impossible: a drifted skill-name string or a divergent max-sanity calc is a *rules* bug, and
this app cares about rules fidelity above almost everything.

## Requirements (as given)

From the analysis, item #5:

> `Skills.FirstOrDefault(...OrdinalIgnoreCase)` is hand-written in at least seven places
> (`SkillsTab`, `SheetSidebar`, `CombatTab` ×2, `WealthTab`, `CreationOccupationSkillsStep`),
> and the strings `"Cthulhu Mythos"`, `"Credit Rating"`, `"Dodge"` are retyped at each site.
> The max-sanity rule (`99 − Mythos`) is independently implemented in both `SheetSidebar.SanMax`
> and `SkillsTab.ImproveSkills`. An `investigator.FindSkill(name)` extension plus a
> `WellKnownSkills` constants class plus one `MaxSanity(investigator)` helper makes rule drift
> structurally impossible.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Extension method vs. instance method on `Investigator`?**
   **[DEFAULT] Extension method** in `Helpers/` (e.g. `InvestigatorSkillExtensions.FindSkill`),
   keeping `Investigator` a plain persisted POCO free of behavior. Matches the existing
   `Helpers/` convention (`SkillRules`, `CharacteristicHelper`). If item #7 later adds
   first-class characteristic access as instance members, that's a separate call; for skills,
   an extension keeps the model clean.

2. **Where do the well-known name constants live?**
   **[DEFAULT] A `WellKnownSkills` static class in `Data/`** (next to `DefaultSkills`), holding
   `const string CthulhuMythos = "Cthulhu Mythos"`, `CreditRating = "Credit Rating"`,
   `Dodge = "Dodge"`, `LanguageOwn = "Language (Own)"`. `Data/` is where the canonical skill
   list already lives, so the canonical names belong beside it. `SkillRules.NonImprovableSkills`
   and `DefaultSkills.ComputeBase`'s switch should reference these constants too.

3. **Does the max-sanity helper go in `SkillRules`, a new `SanityRules`, or `SkillExtensions`?**
   **[DEFAULT] A `SanityRules` static helper** (`Helpers/SanityRules.cs`) with
   `int MaxSanity(Investigator)` = `Math.Max(0, 99 − MythosValue(inv))`, plus a
   `MythosValue(Investigator)` helper (which itself uses `FindSkill(WellKnownSkills.CthulhuMythos)`).
   Rationale: max-sanity is a sanity rule, not a skill rule; a dedicated `SanityRules` mirrors
   the existing `SkillRules`/`CharacteristicHelper` split and gives future sanity logic (bout
   thresholds, indefinite-loss) a home. **Question for user:** prefer folding it into
   `SkillRules` to avoid a new file? Planned with a dedicated `SanityRules`.

4. **Case sensitivity of `FindSkill`.**
   **[DEFAULT] Ordinal case-insensitive**, matching every existing call site. Preserve exactly.

5. **Behavior change allowed?**
   **[DEFAULT] No** — pure refactor. Every call site must produce identical results. The one
   *intended* consequence is that `SheetSidebar.SanMax` and `SkillsTab.ImproveSkills` now go
   through the same `MaxSanity` helper (they already compute the same thing, so no observable
   change — just deduplicated).

## Alternatives considered

- **Leave the lookups inline, just extract constants.** Rejected — constants alone don't
  remove the seven copies of the LINQ predicate; the extension is what collapses them and
  gives one place to change lookup semantics (e.g. if specialization matching ever needs to
  be fuzzier).
- **Put `FindSkill` and name constants all on `Investigator` as members.** Rejected — keeps
  the persisted model a POCO (extensions/helpers are the house style; the model is serialized
  and shouldn't carry logic).
- **A `SkillLookup` service injected via DI.** Rejected — overkill for a pure function over an
  in-memory list; an extension method is simpler and allocation-free.

## Assumptions

- All current call sites use ordinal case-insensitive matching (verified in the review); the
  extension preserves that.
- `MaxSanity`/`MythosValue` reproduce exactly what `SheetSidebar.SanMax` and
  `SkillsTab.ImproveSkills` compute today (`Math.Max(0, 99 − Mythos EffectiveRegular)`).
- No call site relies on getting a *new* `Skill` when absent — `FindSkill` returns `Skill?`
  (null when absent), matching `FirstOrDefault`.

## Rules touched

Preserve (not redefine). Sources: `references/rules_condensed/`.

- **Max Sanity = 99 − Cthulhu Mythos** (`ch_8_sanity.md`, `ch_4_skills.md`): the helper must
  equal `Math.Max(0, 99 − CthulhuMythos.EffectiveRegular)`, and Cthulhu Mythos defaults to 0
  when the skill is absent.
- **Non-improvable skills** (`ch_4`, `ch_5`): Credit Rating and Cthulhu Mythos never tick —
  `SkillRules.NonImprovableSkills` should reference `WellKnownSkills` constants so the names
  can't drift from the lookup sites.
- **Dodge / Language (Own)** (`ch_4`): characteristic-derived bases; the name constants used by
  `DefaultSkills.ComputeBase` and the `CombatTab` Dodge lookup should be the same constants.

## Affected code

New:
- `CthulhuSheets/Data/WellKnownSkills.cs` — name constants.
- `CthulhuSheets/Helpers/InvestigatorSkillExtensions.cs` — `FindSkill(this Investigator, string)`
  and possibly `EffectiveRegularOf(name)` convenience.
- `CthulhuSheets/Helpers/SanityRules.cs` — `MythosValue(Investigator)`, `MaxSanity(Investigator)`.

Changed (call sites repointed; behavior identical):
- `Pages/Home/Components/SkillsTab.razor.cs` — `MythosValue` and the `ImproveSkills` sanity cap
  use `SanityRules`; skill lookups use `FindSkill`.
- `Pages/Home/Components/SheetSidebar.razor.cs` — `CthulhuMythos`/`SanMax` delegate to
  `SanityRules`.
- `Pages/Home/Components/CombatTab.razor.cs` — Dodge and linked-skill lookups use `FindSkill`
  + `WellKnownSkills.Dodge`.
- `Pages/Home/Components/WealthTab.razor.cs` — Credit Rating lookup uses `FindSkill` +
  `WellKnownSkills.CreditRating`.
- `Pages/CharacterCreation/Components/CreationOccupationSkillsStep.razor.cs` — the many
  `Equals("Cthulhu Mythos"/"Credit Rating", …)` checks and `GetSkillBase` lookup use the
  constants + `FindSkill`.
- `Helpers/SkillRules.cs` — `NonImprovableSkills` built from `WellKnownSkills` constants.
- `Data/DefaultSkills.cs` — `ComputeBase`'s `"Dodge"`/`"Language (Own)"` switch uses constants.

**No persisted-model changes** — constants match the exact strings already stored in saved
skill names, so no save is affected.

## Implementation steps

1. **Add `WellKnownSkills` constants.** Create the class with the four names spelled exactly as
   in `DefaultSkills.All`. **Verify:** the constant values are byte-identical to the strings in
   `DefaultSkills.All` (Cthulhu Mythos, Credit Rating, Dodge, Language (Own)).

2. **Add `FindSkill` extension.** `public static Skill? FindSkill(this Investigator inv, string
   name) => inv.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));`
   **Verify:** compiles; unit test (plan #1 project if present): finds by any case, returns null
   when absent.

3. **Add `SanityRules`.** `MythosValue(inv)` = `inv.FindSkill(WellKnownSkills.CthulhuMythos)
   ?.EffectiveRegular ?? 0`; `MaxSanity(inv)` = `Math.Max(0, 99 − MythosValue(inv))`. **Verify:**
   returns 99 for no-Mythos, 94 for Mythos 5, 0 floor for Mythos ≥ 99. Rule: `ch_8`.

4. **Repoint `SheetSidebar` and `SkillsTab` to `SanityRules`.** Replace both local
   implementations. **Verify:** the sidebar's SanMax readout and the improve-skills 90% sanity
   cap behave identically (spot-check with a Mythos value set).

5. **Repoint the remaining skill lookups** (`CombatTab` Dodge + linked skill, `WealthTab`
   Credit Rating, `CreationOccupationSkillsStep` mythos/CR checks + `GetSkillBase`) to
   `FindSkill` + `WellKnownSkills`. **Verify:** Dodge value, credit-rating label, and the
   occupation-step CR/mythos handling are unchanged.

6. **Point `SkillRules.NonImprovableSkills` and `DefaultSkills.ComputeBase` at the constants.**
   **Verify:** ticking rules and Dodge/Language(Own) base computation unchanged; a
   `dotnet test` of `SkillRules`/`DefaultSkills` (plan #1) stays green.

7. **Grep sweep** for any remaining literal `"Cthulhu Mythos"`/`"Credit Rating"`/`"Dodge"`/
   `"Language (Own)"` outside `WellKnownSkills` and repoint. **Verify:** the only definitions of
   these strings are in `WellKnownSkills` (and `DefaultSkills.All`, the canonical source).

## Testing / verification

- `dotnet test` green (if plan #1's suite exists, the SkillRules/Sanity/data tests cover this).
- Manual: Dodge in Combat, Credit Rating label in Wealth, SanMax in the sidebar, and
  improve-skills sanity bump all behave exactly as before.
- `git grep` shows the magic strings defined once.

## Open risks

- **A missed call site** keeps a literal string and silently diverges later — the step-7 grep
  sweep is the guard.
- **`DefaultSkills.All` remains the source of the canonical spelling**; `WellKnownSkills` must
  match it exactly. If they ever disagree, that's the bug this item exists to prevent — plan
  #1's data cross-validation test (Occupations ↔ DefaultSkills) plus an added assertion that
  `WellKnownSkills` values all exist in `DefaultSkills.All` would lock it down. Recommend
  adding that assertion.
