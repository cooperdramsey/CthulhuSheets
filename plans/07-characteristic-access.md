# First-Class Characteristic Access on Investigator — Implementation Plan

> Item #7 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 2.

## Goal

Collapse the several duplicated "characteristic by name" `switch` statements and the several
"list all eight characteristics" enumerations into single, canonical accessors on
`Investigator`: `GetCharacteristic(string abbrev)` and an `IEnumerable<Characteristic>
Characteristics` (or a `(string Abbrev, Characteristic Stat)` list). Every place that needs to
resolve or iterate characteristics then has **one** integration point instead of six, and the
snapshot/restore helpers in the creation step become one-line loops.

## Requirements (as given)

From the analysis, item #7:

> The "characteristic by name" switch exists three times (`Occupation.Evaluate`,
> `CreationCharacteristicsStep.GetCharacteristicByName`, and implicitly in `StoreBaseValues`),
> and the "list all eight" enumeration exists three more times (StatsTab, the creation step
> defs, `RecomputeDerived`'s locals). Adding `Investigator.GetCharacteristic(string abbrev)` and
> `IEnumerable<Characteristic> Characteristics` collapses all of them, and
> `StoreBaseValues`/`RestoreBaseValues`/`StoreHpBaseValues` become one-line loops.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Members on `Investigator` vs. an extension helper?**
   **[DEFAULT] Instance members on `Investigator`** (`GetCharacteristic`, `Characteristics`),
   even though item #5 chose an *extension* for skills. Rationale: the eight characteristics
   are fixed named properties **of** the investigator (unlike the variable `Skills` list), the
   mapping abbrev→property is intrinsic to the type, and `[JsonIgnore]` computed members are
   already the model's idiom (`Characteristic.Half/Fifth`, `Skill.EffectiveRegular`). These
   accessors read existing properties and store nothing new, so they don't affect
   serialization. **Question for user:** prefer an extension to keep `Investigator` a pure
   POCO? Planned as instance members with `[JsonIgnore]` on the enumerable.

2. **What's the canonical shape of the enumeration — bare `Characteristic` or paired with its
   abbreviation/label?**
   **[DEFAULT] Provide both:** `Characteristics` yielding the eight `Characteristic` objects
   (each already carries its `Name`, e.g. "STR"), which is enough for most call sites; the
   display label ("Strength") stays a UI concern in `StatsTab`'s `CharacteristicList`. So the
   model exposes the eight stats in canonical order; the human label mapping remains in the UI
   where it belongs. `GetCharacteristic("STR")` returns the STR `Characteristic`.

3. **Should `Occupation.Evaluate`'s switch be replaced too?**
   **[DEFAULT] Yes** — `Occupation.Evaluate(formula, investigator)` becomes
   `investigator.GetCharacteristic(formula.Characteristic)?.Regular ?? 0) * formula.Multiplier`,
   preserving the "unknown characteristic → 0" behavior (an unknown abbrev returns null →
   treated as 0, exactly as today). This is one of the three duplicated switches the item
   targets.

4. **Behavior change?**
   **[DEFAULT] None.** Pure refactor. `GetCharacteristic` must reproduce the existing switches
   exactly, including throwing for genuinely-unknown names where the current code throws
   (`CreationCharacteristicsStep.GetCharacteristicByName` throws `ArgumentException`) vs.
   returning 0 where the current code tolerates unknowns (`Occupation.Evaluate`). **This is a
   subtle divergence to preserve:** see Assumptions — the two current switches handle unknown
   names *differently*. Decision: `GetCharacteristic` returns `Characteristic?` (null for
   unknown); callers that currently throw keep throwing by null-checking, callers that
   currently coalesce to 0 keep coalescing.

## Alternatives considered

- **A `Dictionary<string, Characteristic>` built per call.** Rejected — allocates on every
  access; a `switch` expression or a computed enumerable is allocation-light and just as
  clear.
- **An enum for the eight characteristics.** Rejected for this item — the app pervasively uses
  the string abbreviations ("STR"…) as the currency (formulas, age targets, serialized skill
  derivations); introducing an enum is a larger change with its own conversions. String-keyed
  accessors match the existing design; an enum could be a separate future refactor.
- **Extension method (like #5).** Considered; rejected in favor of instance members because the
  characteristics are fixed intrinsic properties (decision #1). Noted as a user question.

## Assumptions

- **Unknown-name handling differs between the two current switches and must be preserved:**
  `Occupation.Evaluate` returns 0 for an unknown characteristic; `GetCharacteristicByName`
  throws. Resolving `GetCharacteristic` to return `Characteristic?` lets each caller keep its
  current semantics (coalesce-to-0 vs. throw-on-null). Verify each call site preserves its
  behavior.
- The canonical order (STR, CON, SIZ, DEX, APP, INT, POW, EDU) matches the order used in
  `RecomputeDerived`, `StatsTab`, and the creation defs; `Characteristics` uses this order.
- Adding `[JsonIgnore]` computed members does not change the serialized shape (confirmed by the
  existing `Half`/`Fifth`/`EffectiveRegular` precedent).

## Rules touched

Preserve (not redefine). Sources: `references/rules_condensed/`.

- **Occupation skill points** (`ch_3`): `Occupation.Evaluate` = `characteristic.Regular ×
  multiplier`, unknown → 0. Behavior must be identical after switching to `GetCharacteristic`.
- **Age modifiers / derived stats** (`ch_3`): the creation step's snapshot/restore and the
  physical-deduction/EDU-check targeting all resolve characteristics by abbrev; the accessor
  must return the same objects so age application is unchanged.
- No formula values change — this item only changes *how* a characteristic is located, not any
  rule.

## Affected code

New/changed on the model:
- `CthulhuSheets/Models/Investigator.cs` — add `Characteristic? GetCharacteristic(string
  abbrev)` (switch over the eight `Name`s) and `[JsonIgnore] IEnumerable<Characteristic>
  Characteristics` (the eight in canonical order).

Repointed call sites:
- `CthulhuSheets/Models/Occupation.cs` — `Evaluate` uses `GetCharacteristic` (coalesce null → 0).
- `Pages/CharacterCreation/Components/CreationCharacteristicsStep.razor.cs` —
  `GetCharacteristicByName` delegates to `GetCharacteristic` (null → throw, preserving current
  `ArgumentException`); `StoreBaseValues`/`RestoreBaseValues`/`StoreHpBaseValues` become loops
  over `Characteristics`/`StatNames`.
- `Pages/Home/Components/StatsTab.razor.cs` — `CharacteristicList` can build its
  (label, stat) pairs from `Characteristics` + a label map, or stay (it's a UI label list;
  optional to touch).
- `Helpers/CharacteristicHelper.cs` — `RecomputeDerived`'s per-stat locals may read via the
  named properties as today (no need to change) or via `GetCharacteristic`; leave as-is unless
  it reduces duplication cleanly. (The item's win is the switches/snapshots, not `RecomputeDerived`.)

**No persisted-model changes** — the accessors are `[JsonIgnore]`/method members over existing
properties. Saved characters unaffected.

## Implementation steps

1. **Add `GetCharacteristic` + `Characteristics` to `Investigator`.** Switch over "STR"…"EDU"
   → the eight properties (case-insensitive to match `Occupation.Evaluate`'s current
   behavior — note `GetCharacteristicByName` is case-sensitive today; standardize on
   case-insensitive, which is a superset and won't break current callers that pass exact
   abbrevs). `Characteristics` yields them in canonical order. **Verify:** unit test (plan #1):
   `GetCharacteristic("pow")` and `("POW")` both return POW; unknown returns null;
   `Characteristics` yields 8 in order.

2. **Repoint `Occupation.Evaluate`.** `(investigator.GetCharacteristic(formula.Characteristic)
   ?.Regular ?? 0) * formula.Multiplier`. **Verify:** `OccupationTests` (plan #1) stay green;
   unknown characteristic still yields 0.

3. **Repoint the creation step.** `GetCharacteristicByName(name)` → `GetCharacteristic(name) ??
   throw new ArgumentException($"Unknown characteristic: {name}")` (preserves current throw).
   Rewrite `StoreBaseValues`/`RestoreBaseValues`/`StoreHpBaseValues` as loops over the eight
   (using `StatNames`/`Characteristics`). **Verify:** every generation method + every age
   bracket (incl. deductions + reset) behaves identically in the running app.

4. **(Optional) Repoint `StatsTab.CharacteristicList`** to derive stats from `Characteristics`
   with a label lookup, if it reduces duplication without obscuring the labels. **Verify:**
   the stats grid renders the same eight cards with the same labels/order.

5. **Grep sweep** for other `switch`/if-chains over "STR"/"CON"/… and repoint where it clarifies.
   **Verify:** the abbrev→property mapping is defined once (in `GetCharacteristic`).

## Testing / verification

- `dotnet test` green (Occupation + any new characteristic-accessor tests).
- Manual: full character creation across all methods and age brackets is behavior-identical;
  the stats tab renders identically.
- `git diff` shows no formula/value change — only lookup/enumeration consolidation.

## Open risks

- **Unknown-name semantics divergence** (Assumptions) is the one sharp edge: `Evaluate`
  tolerates unknowns (→0), the creation step throws. Returning `Characteristic?` and letting
  each caller keep its behavior is the safe resolution; verify both paths in step 2/3.
- **Case sensitivity:** standardizing on case-insensitive is a safe superset, but confirm no
  caller *relies* on a case-sensitive miss (none should).
- Keeping the display-label map in the UI (not the model) is deliberate — don't push
  "Strength"/"Constitution" labels into the model; the model speaks abbreviations.
