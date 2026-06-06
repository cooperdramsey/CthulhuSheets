---
name: rules-review
description: >-
  Review the CthulhuSheets app against the Call of Cthulhu 7e rules. Use when
  the user asks to "review the rules implementation", "check the app follows the
  rules", "rules-check the character creation / skills / combat / sanity", or to
  audit a specific formula (damage bonus, HP, skill points, credit rating, etc.)
  for correctness. The condensed rules in references/rules_condensed/ are the
  source of truth; this skill compares the C#/Blazor implementation to them and
  reports discrepancies.
---

# Rules Review

Audit the CthulhuSheets implementation for fidelity to Call of Cthulhu 7th
Edition. The **condensed rules in `references/rules_condensed/` are the source of
truth.** Find every place the code computes, validates, or constrains a
game value and confirm it matches the rule. Report discrepancies; do not change
code unless the user explicitly asks for fixes.

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
| Magic points, spellcasting, tome study | `ch_9_magic.md`, `ch_11`, `ch_12` |
| Equipment / cash / spending level by Credit Rating | `ch_3` + `references/rules_md/appendix_equipment.md` |

If a rule the code depends on isn't in the condensed file, check the fuller
`references/rules_md/<name>.md` before concluding the code is wrong.

## Where the rules live in the code

Start from these, then follow references:

- **Models:** `CthulhuSheets/Models/` — `Characteristic.cs`, `Skill.cs`,
  `HitPoints.cs`, `MagicPoints.cs`, `Luck.cs`, `Sanity.cs`, `Wealth.cs`,
  `Weapon.cs`, `Occupation.cs`, `Investigator.cs`.
- **Derived-stat logic:** `CthulhuSheets/Helpers/CharacteristicHelper.cs`.
- **Static rules data:** `CthulhuSheets/Data/Occupations.cs`.
- **Creation flow / validation:** `CthulhuSheets/Pages/CharacterCreation/Components/`
  (`CreationCharacteristicsStep`, `CreationOccupationSkillsStep`,
  `CreationWealthStep`, etc.).
- **Play-time computation:** `CthulhuSheets/Pages/Home/Components/`
  (`StatsTab`, `SkillsTab`, `CombatTab`, `WealthTab`) and
  `CthulhuSheets/Services/DiceRollService.cs`.

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

1. **Scope.** Confirm what to review (a feature, a tab, a formula, or the whole
   app). If unscoped, default to the derived-stat + creation formulas above.
2. **Read the relevant condensed rule file(s)** for the area in full.
3. **Locate the implementation** (tables above) and read the formulas/validation.
4. **Compare** each value, threshold, rounding rule, and constraint against the
   rule. Watch specifically for: wrong rounding (round vs floor), off-by-one band
   edges (≥ vs >), missing age/specialization cases, hardcoded values that should
   be derived, and validation that allows out-of-range input.
5. **Report findings** (format below). Cite the rule and the code location.
6. Only edit code if the user asked for fixes; otherwise stop at the report.

## Report format

Group findings by area. For each:

- **✅ Correct** — value/formula matches; one line, with the code location.
- **❌ Mismatch** — what the code does vs. what the rule says, with the
  `file.cs:line` location and the citing condensed-rules file. Note severity
  (breaks a character vs. cosmetic).
- **⚠️ Unverifiable / gap** — rule the code should enforce but doesn't, or a case
  the rules don't cover. Say what's missing.

End with a short summary: count of correct / mismatches / gaps, and the
highest-priority fixes. Reference code as clickable `path:line` links.

## Guardrails

- The condensed rules win ties. If a condensed file looks wrong or incomplete,
  check `references/rules_md/` before flagging the code, and say which you used.
- Don't invent rules from memory — cite the file. CoC editions differ; this app
  targets 7e.
- Verify line numbers by reading the file; never guess a location.
