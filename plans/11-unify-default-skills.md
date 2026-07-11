# Unify Default-Skill Population — Implementation Plan

> Item #11 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 3.

## Goal

`CreationOccupationSkillsStep.PopulateDefaults` and `SkillsTab.LoadDefaultSkills` are
near-identical loops that seed the standard skill list onto an investigator — but they differ
in one field: creation sets `IsDefault = true`, the sheet doesn't. Move the shared logic to
one method (`DefaultSkills.AddMissingTo(investigator)`) and settle the `IsDefault` semantics
once, so the same skill can't end up flagged differently depending on where it was added.

## Requirements (as given)

From the analysis, item #11:

> `CreationOccupationSkillsStep.PopulateDefaults` and `SkillsTab.LoadDefaultSkills` are
> near-identical, except creation sets `IsDefault = true` and the sheet version doesn't — so the
> same skill ends up flagged differently depending on where it was added. Move it to
> `DefaultSkills.AddMissingTo(investigator)` and decide the `IsDefault` semantics once.

## What `IsDefault` actually does (verified)

`Skill.IsDefault` is **set** only in `CreationOccupationSkillsStep.PopulateDefaults` and
**read** only in `CreationOccupationSkillsStep.razor` (two `@if (!skill.IsDefault)` guards
that gate the per-skill **delete** button — i.e. default skills can't be removed during
creation, custom-added ones can). It is **not** read anywhere on the play-time sheet, and
`SkillsTab.LoadDefaultSkills` never sets it. So the flag means "a standard skill seeded by the
creation flow, not removable in the creation UI." The divergence is currently latent (the two
methods aren't invoked on the same skill in the same context), but it's exactly the kind of
inconsistency that bites later.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **What should `IsDefault` mean, canonically?**
   **[DEFAULT] "This skill came from the standard `DefaultSkills` list" — set it `true`
   whenever `AddMissingTo` seeds a standard skill, from *either* entry point.** Rationale: the
   flag's only current job is "is this a standard skill (vs. a user-added custom one)," which is
   true regardless of where it was seeded. Making `AddMissingTo` always set `IsDefault = true`
   unifies the behavior and is the least-surprising meaning. The creation UI's "can't delete
   defaults" behavior is preserved (defaults are still flagged); the sheet gains a correctly-set
   flag it currently ignores anyway (no visible change there today, but now correct if the sheet
   ever reads it). **Question for user:** is it acceptable that skills loaded via the sheet's
   "Load Defaults" now carry `IsDefault = true` (persisted)? Planned **yes** — it's the correct
   meaning and harmless (nothing on the sheet gates on it). If you'd rather the sheet-loaded
   ones stay `false`, `AddMissingTo` can take an `isDefault` parameter (see alt).

2. **Signature of the unified method.**
   **[DEFAULT] `DefaultSkills.AddMissingTo(Investigator investigator)`** — adds every
   `DefaultSkills.All` entry not already present (case-insensitive), computing each base via
   `ComputeBase`, setting `IsDefault = true`. Returns nothing (mutates the list) or the count
   added (minor convenience). Both call sites call it, then persist as they do now.
   **Alternative kept in reserve:** `AddMissingTo(investigator, bool markDefault = true)` if
   decision #1's "always true" is rejected.

3. **Behavior change on the sheet?**
   **[DEFAULT] Only that sheet-loaded default skills now carry `IsDefault = true`** (persisted
   in their JSON). No UI on the sheet reads it, so no visible change today. The creation flow is
   unchanged. If this persisted-flag change is unwanted, use the parameterized signature (alt in
   #2).

## Alternatives considered

- **Leave two methods; just copy the `IsDefault = true` into the sheet version.** Rejected —
  still two copies that can drift; the point is one source of truth.
- **Parameterize `markDefault`.** Kept as the fallback if the user wants sheet-loaded defaults
  to stay `IsDefault = false`. Slightly more flexible but preserves the very divergence we're
  removing, so not the default.
- **Drop `IsDefault` entirely and derive "is standard" by checking membership in
  `DefaultSkills.All`.** Tempting (removes a persisted flag), but the creation UI's
  removability gate is a real behavior; deriving it is a larger change and `IsDefault` also
  captures "was seeded as a default" which membership can't fully express for edited skills.
  Out of scope; note as a possible future simplification.

## Assumptions

- `IsDefault`'s only consumer is the creation-step delete-button gate (verified). Setting it
  `true` from the sheet path has no visible effect today.
- Persisting `IsDefault = true` on sheet-loaded default skills is acceptable (decision #1/#3).
  If not, switch to the parameterized signature.
- Both call sites currently: skip skills already present by name (case-insensitive), compute
  base via `DefaultSkills.ComputeBase`, and add. `AddMissingTo` reproduces this exactly.

## Rules touched

Preserve. Source: `references/rules_condensed/`.

- **Standard skill list & base values** (`ch_4_skills.md`): `DefaultSkills.All` is the canonical
  list; `ComputeBase` substitutes Dodge = ½DEX and Language (Own) = EDU. `AddMissingTo` must
  keep using `ComputeBase` so characteristic-derived bases are correct. No base value changes.

## Affected code

Changed:
- `CthulhuSheets/Data/DefaultSkills.cs` — add `public static void AddMissingTo(Investigator
  investigator)` (or `int` returning count) encapsulating the seed loop, setting `IsDefault =
  true`.
- `Pages/CharacterCreation/Components/CreationOccupationSkillsStep.razor.cs` — `PopulateDefaults`
  becomes a call to `DefaultSkills.AddMissingTo(Investigator)`.
- `Pages/Home/Components/SkillsTab.razor.cs` — `LoadDefaultSkills` becomes
  `DefaultSkills.AddMissingTo(Investigator); await PersistAsync();`.

**Persisted-model note:** `IsDefault` is an existing `Skill` property already serialized;
existing saves already have it (`false` for sheet-loaded ones). After this change, newly
sheet-loaded defaults get `true`. No schema change — same property, and old saves deserialize
fine. No migration needed.

## Implementation steps

1. **Add `DefaultSkills.AddMissingTo`.** Move the shared loop in: for each `(name, baseVal)` in
   `All`, skip if a skill with that name already exists (case-insensitive), else add
   `new Skill { Name = name, BaseValue = ComputeBase(name, baseVal, investigator), IsDefault =
   true }`. **Verify:** unit test (plan #1): on an empty investigator adds all standard skills
   with correct (characteristic-derived) bases and `IsDefault = true`; on a partial list adds
   only the missing ones and doesn't duplicate.

2. **Repoint `CreationOccupationSkillsStep.PopulateDefaults`** to call it. **Verify:** creation
   flow seeds defaults identically; the delete-button gate (`!skill.IsDefault`) still hides
   delete for standard skills and shows it for custom ones.

3. **Repoint `SkillsTab.LoadDefaultSkills`** to call it + persist. **Verify:** "Load Defaults"
   on the sheet adds missing standard skills with correct bases; existing skills untouched;
   persisted.

4. **Confirm the `IsDefault` semantics** are consistent: create a character, then on the sheet
   remove a default skill and re-add via Load Defaults — it comes back flagged `IsDefault =
   true` (consistent with creation). **Verify:** no divergence remains.

## Testing / verification

- `dotnet test` green (the `AddMissingTo` test).
- Creation: defaults seeded, non-removable in the creation UI; custom skills removable.
- Sheet: "Load Defaults" adds missing standard skills with correct bases and persists.
- `git grep` shows the seed loop defined once (in `DefaultSkills.AddMissingTo`).

## Open risks

- **The persisted-flag change** (sheet-loaded defaults now `true`) is the only behavior delta;
  it's invisible today because nothing on the sheet reads `IsDefault`. If a future feature makes
  the sheet gate on it, this unified meaning is the correct one. If the user wants the old
  sheet behavior (`false`), switch to the parameterized `AddMissingTo(investigator, markDefault)`
  — flagged in decision #2.
- Otherwise negligible — a straightforward dedup with a settled semantic.
