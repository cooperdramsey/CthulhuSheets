# Fix CombatTab.RollDodge Experience-Check Drift — Implementation Plan

> Item #6 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 2.
> Two-line fix; do it when next touching `CombatTab`. Pairs naturally with #5.

## Goal

`SkillRules.ShouldMarkExperienceCheck` exists precisely so every roll path ticks skills
identically "by construction," and `RollWeapon` uses it — but `CombatTab.RollDodge`
re-implements the experience-check inline and omits the `NonImprovableSkills` guard. It's
harmless for Dodge *today* (Dodge is improvable, so the missing guard changes nothing right
now), but it is exactly the duplication the shared helper was written to prevent, and it will
silently diverge the day the check logic changes. Replace the inline check with the shared
helper.

## Requirements (as given)

From the analysis, item #6:

> `SkillRules.ShouldMarkExperienceCheck` says "Shared by every roll path … so they all tick
> identically by construction," and `RollWeapon` uses it — but `CombatTab.RollDodge`
> re-implements the check inline and omits the `NonImprovableSkills` guard (harmless for Dodge
> today, but the duplication the helper exists to prevent is already there). Two-line fix.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Just swap in the helper, or also factor the Dodge lookup?**
   **[DEFAULT] Swap in the helper; use `FindSkill`/`WellKnownSkills.Dodge` if plan #5 has
   landed, otherwise keep the existing inline lookup.** The core fix is replacing the inline
   experience-check condition with `SkillRules.ShouldMarkExperienceCheck(dodgeSkill,
   result.Total, modifier)`. If done after #5, use the centralized lookup too; if done before
   #5, leave the lookup as-is so this stays a genuinely two-line change.

2. **Behavior change?**
   **[DEFAULT] None intended and none expected.** Dodge is not in `NonImprovableSkills`, so
   adding the guard doesn't change Dodge's behavior. The change makes Dodge tick via the
   identical predicate as every other roll path — the *point* is future-proofing, not a
   present-day fix. Confirm no behavior change with a test.

## Alternatives considered

- **Leave it — it works today.** Rejected — the whole value is preventing future drift; a
  helper that one caller ignores is a latent inconsistency, and this is a two-line correction.
- **Delete the helper and inline everywhere.** Rejected — the opposite of the goal;
  centralization is what makes the ticking rule single-sourced (`ch_5`).

## Assumptions

- Dodge remains an improvable skill (not added to `NonImprovableSkills`); if that ever changes,
  the shared helper would then correctly stop ticking it — which is the desired behavior and
  another argument for the helper.
- `RollDodge`'s surrounding logic (setting `_dodgeRoll`, the persist-on-tick) is otherwise
  correct and stays.

## Rules touched

- **Experience checks / development phase** (`ch_5_game_system.md`): a skill is ticked on a
  successful roll (`roll ≤ EffectiveRegular`) with **no bonus die** (`modifier ≤ 0`), only if
  not already ticked, and never for `NonImprovableSkills` (Credit Rating, Cthulhu Mythos). The
  shared `SkillRules.ShouldMarkExperienceCheck` encodes exactly this; `RollDodge` must use it.

## Affected code

- `CthulhuSheets/Pages/Home/Components/CombatTab.razor.cs` — `RollDodge`: replace the inline
  `result.Total <= dodgeSkill.EffectiveRegular && modifier <= 0 && !dodgeSkill.HasExperienceCheck`
  condition with `SkillRules.ShouldMarkExperienceCheck(dodgeSkill, result.Total, modifier)`.

**No persisted-model changes, no data changes.**

## Implementation steps

1. **Replace the inline check in `RollDodge`.** After computing `result` and locating the
   Dodge skill, gate the tick with
   `if (dodgeSkill is not null && SkillRules.ShouldMarkExperienceCheck(dodgeSkill,
   result.Total, modifier)) { dodgeSkill.HasExperienceCheck = true; await PersistAsync(); }`.
   Remove the duplicated inline condition and its comment (the helper is self-documenting).
   **Verify:** compiles; `RollWeapon` and `RollDodge` now call the same predicate.

2. **Manual behavior check.** In the running app: roll Dodge under its value with no modifier →
   experience check ticks and persists; roll with a bonus die (modifier > 0) → does **not**
   tick; roll over the value → does not tick; roll when already ticked → no change. Identical to
   before. **Verify:** all four cases behave as expected.

3. **(If plan #1 exists) add/extend a `SkillRules` test** asserting the Dodge scenario goes
   through the same predicate — or simply rely on the existing `ShouldMarkExperienceCheck`
   tests, since `RollDodge` now delegates to it. **Verify:** green.

## Testing / verification

- The four ticking cases in step 2 behave identically to pre-change.
- `SkillRules.ShouldMarkExperienceCheck` is now the single source for Dodge, weapon, single-,
  and combined-skill roll ticking (grep confirms no other inline experience-check remains in
  `CombatTab`/`SkillsTab`).

## Open risks

- **Negligible.** The only behavioral difference the guard could introduce is for a
  `NonImprovableSkills` member, which Dodge is not. If a future edit ever links Dodge-like
  logic to Credit Rating/Cthulhu Mythos, the helper does the right thing. This is strictly a
  consistency/robustness fix.
