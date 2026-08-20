---
name: rules-review
description: >-
  Code-review the CthulhuSheets app with the Call of Cthulhu 7e rules as the
  spec. Use when the user asks to "review the rules implementation", "review the
  recent changes", "check the app follows the rules", "rules-check the character
  creation / skills / combat / sanity", or to audit a specific formula (damage
  bonus, HP, skill points, credit rating, etc.). Reviews the stated scope (by
  default the most recent changes per git; otherwise a feature/tab/formula or the
  whole codebase) for both rules fidelity AND code quality — bugs, edge cases,
  bad rounding, dead/duplicated logic. The condensed rules in
  references/rules_condensed/ are the source of truth.
---

# Rules Review

Review the CthulhuSheets implementation **with the Call of Cthulhu 7th Edition
rules as the spec.** This is a code review, not just a rules-fidelity audit: for
the code in scope, check both that it **matches the rule** and that it is
**correct and clean** as code (edge cases, rounding, off-by-one, dead or
duplicated logic, validation that lets bad state through). The **condensed rules
in `references/rules_condensed/` are the source of truth** — they define the
expected behavior. Report findings; do not change code unless the user explicitly
asks for fixes.

## Source of truth

`references/rules_condensed/*.md` — each chapter is a lean, rules-only digest.
Map the area under review to its chapter:

| Area | Rules file(s) |
|---|---|
| Characteristics, derived stats (Build/Damage Bonus, HP, MP, MOV, Luck), point-buy, age modifiers, occupation skill points, credit-rating range, backstory | `ch_3_creating_investigators.md` |
| Skill list & base values, half/fifth, specializations, Dodge/Language/Credit Rating, skill improvement (development phase) | `ch_4_skills.md` |
| Success levels (Regular/Hard/Extreme/Critical/Fumble), pushing rolls, bonus & penalty dice, opposed rolls, Luck spending | `ch_5_game_system.md` |
| Weapons, damage, attacks/round, firearms, HP loss / major wound / dying, healing, MOV in combat | `ch_6_combat.md` |
| Starting SAN (= POW), SAN max (99 − Cthulhu Mythos), SAN loss/recovery, insanity thresholds | `ch_8_sanity.md` |
| Magic points, spellcasting, tome study | `ch_9_magic.md`, `ch_11_tomes_of_eldritch_lore.md`, `ch_12_spells.md` |
| Equipment / cash / spending level by Credit Rating | `ch_3` + `references/rules_condensed/appendix_equipment.md` |

If a rule the code depends on isn't in the condensed file, check the fuller
`references/rules_md/<name>.md` before concluding the code is wrong.

## Where the rules live in the code

Start from these, then follow references:

- **Models:** `CthulhuSheets/Models/` — `Characteristic.cs`, `Skill.cs`,
  `HitPoints.cs`, `MagicPoints.cs`, `Luck.cs`, `Sanity.cs`, `Wealth.cs`,
  `Weapon.cs`, `Occupation.cs`, `Investigator.cs`; plus roster/metadata models
  (`Roster.cs`, `RosterEntry.cs`, `FellowInvestigator.cs`) — rarely
  rules-bearing but part of the persisted shape.
- **Derived-stat logic:** `CthulhuSheets/Helpers/CharacteristicHelper.cs`
  (point-buy constants, EDU improvement, Luck roll, `RecomputeDerived`).
- **Static rules data:** `CthulhuSheets/Data/Occupations.cs` and
  `CthulhuSheets/Data/DefaultSkills.cs` — the canonical skill list with printed
  base values (characteristic-derived bases like Dodge/Language (Own) are
  computed per-investigator, not stored). Both are prime audit targets: a wrong
  number here silently corrupts every character.
- **Creation flow / validation:** `CthulhuSheets/Pages/CharacterCreation/Components/`
  (`CreationCharacteristicsStep`, `CreationOccupationSkillsStep`,
  `CreationWealthStep`, etc.).
- **Play-time computation:** `CthulhuSheets/Pages/Home/Components/`
  (`StatsTab`, `SkillsTab`, `CombatTab`, `WealthTab`, `InfoTab`, `ItemsTab`,
  `SheetSidebar`, `InvestigatorSheet`, `DiceFab`) and
  `CthulhuSheets/Services/DiceRollService.cs`.
- **Persistence:** `CthulhuSheets/Services/InvestigatorService.cs` and
  `CthulhuSheets/Services/Storage/` (`ICharacterStore`,
  `IndexedDbCharacterStore`, `LocalStorageCharacterStore`) — characters
  round-trip as JSON; `CthulhuSheets/Pages/Roster/` manages multiple
  characters. Not rules logic, but in scope for the quality axis below.

Note the partial-class pattern: a feature is usually split across
`X.razor` (markup) + `X.razor.cs` (logic). Check the `.razor.cs` for formulas.

## Key formulas to verify (7e)

These are the highest-value checks — confirm the code matches:

- **Damage Bonus / Build** from STR+SIZ bands: 2–64 → −2/−2; 65–84 → −1/−1;
  85–124 → 0/0; 125–164 → +1D4/1; 165–204 → +1D6/2; 205–284 → +2D6/3;
  285–364 → +3D6/4; then +1D6/+1 build per +80.
- **Hit Points** = (CON + SIZ) / 10, rounded down.
- **Magic Points** = POW / 5, rounded down.
- **Sanity (starting)** = POW. **SAN max** = 99 − Cthulhu Mythos %.
- **Luck (starting)** = 3D6 × 5 (or by chosen creation method).
- **Move (MOV):** both STR **and** DEX < SIZ → 7; both STR **and** DEX > SIZ → 9;
  otherwise (incl. all three equal, or only one ≥ SIZ) → 8. Watch the strict
  `<` / `>` boundaries. Age penalty: 40s −1, 50s −2, 60s −3, 70s −4, 80s −5.
- **Skill base values** per `ch_4` (e.g. Dodge = ½ DEX, Language (Own) = EDU,
  Credit Rating base 0). **Half** = value/2, **Fifth** = value/5 (round down).
- **Occupation skill points** = the occupation's formula (commonly EDU×4, or
  EDU×2 + a chosen characteristic ×2). **Personal-interest points** = INT × 2.
- **Credit Rating** is capped to the occupation's min–max range; spending
  level / cash / assets derive from it (appendix_equipment).
- **Characteristic point-buy / roll** ranges and the age modifiers
  (EDU improvement checks, deductions for young/old) per `ch_3`.

Treat these as a checklist, not the full set — derive any others from the
condensed files for the area in scope.

## Procedure

1. **Scope.** Confirm what to review. **If unscoped, default to the most recent
   changes per git** — run `git diff HEAD` (and `git log --oneline -5` /
   `git diff main...HEAD` for branch work) and review the changed lines plus the
   code they touch. Other scopes: a feature, a tab, a formula, or the whole
   codebase. State the scope you settled on at the top of the report.
2. **Map scope to rules.** For each rules-bearing thing the in-scope code touches,
   find its chapter (tables above) and **read the relevant condensed rule file(s)
   in full.** Skip files for areas the scope doesn't touch.
3. **Locate the implementation** (tables above) and read the formulas/validation.
4. **Review against the rule on two axes:**
   - **Rules fidelity** — does each value, threshold, rounding rule, and
     constraint match the rule? Watch for: wrong rounding (round vs floor),
     off-by-one band edges (≥ vs >), missing age/specialization cases, hardcoded
     values that should be derived, validation that allows out-of-range input.
   - **Code quality** — given the rule as the spec, is the code correct and
     clean? Look for bugs and unhandled edge cases (null/zero/min/max inputs),
     logic duplicated across components that can drift, dead or unreachable
     branches, and over-complex expressions of a simple rule. Flag quality issues
     even where the output happens to be right today. Two project-specific
     checks:
     - **Persistence compatibility** — characters are serialized to
       IndexedDB/localStorage as JSON via `ICharacterStore`. If the diff changes
       a persisted model (anything reachable from `Investigator` or `Roster`),
       verify previously saved characters still deserialize sanely (renamed or
       retyped properties, non-nullable additions with no default, removed enum
       values). Flag any change that would break or silently zero a saved sheet.
     - **Styling tokens** — if the diff touches a `.razor.css`, spacing /
       border-radius / icon sizes must use the design tokens from `CLAUDE.md`
       (`--space-*`, `--radius-*`, `--icon-*`), not new raw px values.
5. **Report findings** (format below). Cite the rule and the code location.
6. Only edit code if the user asked for fixes; otherwise stop at the report.

## Report format

Open with the **scope** you reviewed (e.g. "the diff on `main` since HEAD~1" or
"whole CharacterCreation flow"). Then group findings by area. For each:

- **✅ Correct** — value/formula matches the rule and the code is sound; one line,
  with the code location.
- **❌ Mismatch** — code disagrees with the rule: what it does vs. what the rule
  says, with the `file.cs:line` location and the citing condensed-rules file.
  Note severity (breaks a character vs. cosmetic).
- **🐛 Code issue** — the rule is met (or not directly involved) but the code has
  a bug, unhandled edge case, duplication, or needless complexity. Say what's
  wrong and the risk, with the `file.cs:line` location.
- **⚠️ Unverifiable / gap** — rule the code should enforce but doesn't, or a case
  the rules don't cover. Say what's missing.

End with a short summary: counts of correct / mismatches / code issues / gaps,
and the highest-priority fixes. Reference code as clickable `path:line` links.

## Guardrails

- The condensed rules win ties. If a condensed file looks wrong or incomplete,
  check `references/rules_md/` before flagging the code, and say which you used.
- `references/` is gitignored (copyrighted source) and exists only on this
  machine. If `references/rules_condensed/` is missing, **stop and tell the
  user** — do not review against rules recalled from memory.
- Don't invent rules from memory — cite the file. CoC editions differ; this app
  targets 7e.
- Verify line numbers by reading the file; never guess a location.
