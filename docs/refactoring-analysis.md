# CthulhuSheets — Codebase Refactoring Analysis

> Full architectural/refactoring review of the codebase, ranked highest→lowest value-add.
> Generated 2026-07-11. This is the source-of-truth backlog; each item is (or will be)
> expanded into a vetted implementation plan under `plans/`.
>
> **Overall verdict:** this is a genuinely well-kept codebase. The rules logic is carefully
> commented with chapter citations, the storage layer documents its hard-won IndexedDB
> workarounds, persistence is consistently wired, and components dispose their event
> subscriptions. The findings below are about leverage, not rescue.

## Status tracker

| # | Item | Tier | Plan file |
|---|------|------|-----------|
| 1 | Add a test project for the rules engine | 1 | `plans/01-test-project.md` |
| 2 | Extract creation-step rule state machines | 1 | `plans/02-extract-creation-state-machines.md` |
| 3 | Portrait storage: separate record + fix import/export asymmetry | 1 | `plans/03-portrait-storage.md` |
| 4 | Delete Bootstrap (8.5 MB dead weight) | 1 | `plans/04-delete-bootstrap.md` |
| 5 | Centralize skill lookup + well-known skill names | 2 | `plans/05-centralize-skill-lookup.md` |
| 6 | Fix CombatTab.RollDodge drift | 2 | `plans/06-rolldodge-drift.md` |
| 7 | First-class characteristic access on Investigator | 2 | `plans/07-characteristic-access.md` |
| 8 | One shared JsonSerializerOptions | 2 | `plans/08-shared-json-options.md` |
| 9 | Slim InvestigatorService: extract migration, collapse dupes | 3 | `plans/09-slim-investigator-service.md` |
| 10 | Extract "roll vs threshold" markup component | 3 | `plans/10-threshold-check-component.md` |
| 11 | Unify default-skill population | 3 | `plans/11-unify-default-skills.md` |
| 12 | Small cleanups (batched) | 3 | `plans/12-small-cleanups.md` |

---

## Tier 1 — Structural wins

### 1. Add a test project — the rules engine is pure logic with zero coverage

There are no tests in the solution, yet a large share of the code is exactly what unit
tests are best at: deterministic rules math. `Helpers/CharacteristicHelper.cs`
(damage-bonus/build/MOV tables, EDU improvement), `Helpers/SkillRules.cs`,
`Models/Occupation.cs` (`ComputeSkillPoints`), and `Services/DiceRollService.cs`
(`RollPercentile` — the bonus/penalty tens-dice mechanic) are all pure or near-pure
today — testable with no refactoring.

**Why it's the top item:** this project's whole discipline (the `rules-review` skill, the
rule citations in comments) is about rules fidelity, and right now every change to a
formula is verified by hand. Tests convert that recurring manual cost into a one-time
cost. Two especially high-value cases:

- A **data cross-validation test**: assert every skill name in `Data/Occupations.cs`
  exists in `Data/DefaultSkills.All`. A typo like "Firearms (Handguns)" would currently
  fail *silently* — the occupation-skill match in the creation step just never fires. Same
  for the `SkillPointFormula` characteristic strings, which `Occupation.Evaluate` silently
  maps to 0 when unknown.
- The damage-bonus/build boundary values (`<= 64`, `<= 84`…), where an off-by-one is
  invisible until someone's character is wrong.

### 2. Extract the creation-step rule state machines out of component code-behind

The two biggest code files after the data table are creation steps:
`CreationCharacteristicsStep.razor.cs` (515 lines) and
`CreationOccupationSkillsStep.razor.cs` (412 lines). Most of their content isn't UI — it's
rules state machines: point-buy budget validation, the place-rolls/quick-fire pool, Modify
Low Rolls, Human Potential, age brackets and deduction pools; occupation/personal
allocation pools with the 75% cap and credit-rating bounds. `SkillsTab.ImproveSkills` (the
development phase, including the 90%+ sanity bonus) is the same story.

Extracting these into plain classes (e.g. `CharacteristicGenerationSession`,
`SkillAllocationSession`, a `DevelopmentPhase` helper next to `SkillRules`) pays three
ways: the components shrink to thin bindings, the trickiest rules in the app become
unit-testable (finding #1 can't reach them otherwise), and future features (e.g. a
different creation method) get a place to live that isn't a 500-line partial class. This is
the single biggest maintainability lever in the codebase.

### 3. Portrait storage: inline base64 makes every save heavy and has already created a real bug

Portraits are stored as a data URL inside `Investigator.PortraitDataUrl`, which means:

- **Import/export asymmetry (latent bug):** `PortraitDialog` accepts files up to **5 MB**
  (≈6.7 MB as base64), but `MainLayout.HandleFileSelected` caps import at **1 MB**. A
  character exported with a large portrait cannot be re-imported — the app's own backup
  story breaks.
- **Every save rewrites the portrait.** Each `@bind-Value:after="PersistAsync"` (every
  field commit on every tab) reserializes the entire investigator, portrait included, into
  IndexedDB. Ticking a condition checkbox can write megabytes.
- **The roster page loads every character in full just to show thumbnails** —
  `Roster.RefreshAsync` calls `GetCharacterAsync` per entry for `PortraitDataUrl` alone.

The clean fix is a separate portrait record keyed by character id (the IndexedDB
`meta`/`characters` pattern already supports this shape), with export/import bundling it.
Minimum viable fix: raise the import cap to match the 5 MB upload limit — that's one line
and removes the data-loss trap.

### 4. Delete Bootstrap — 8.5 MB of dead weight that every user downloads

`wwwroot/index.html` links `bootstrap.min.css`, and `wwwroot/lib/bootstrap/` is 8.5 MB.
Verified: **zero Bootstrap classes are used anywhere** — every `row`/`container` match is a
custom scoped-CSS name (`age-input-row`, `combat-container`); MudBlazor provides
everything. Because this is a PWA, the published service worker **precaches the entire
asset manifest**, so Bootstrap is downloaded and cached by every user on first visit and
revalidated after every deploy. Removing it is a direct payload/first-load win, not just
repo hygiene. While there: the Blazor-template leftovers in `wwwroot/css/app.css`
(`.btn-primary`, `.valid.modified`, etc. — keep `.validation-message` if you want it for
the EditForm in the profile step, though MudBlazor renders its own errors).

---

## Tier 2 — Drift-prevention refactors (small effort, compounding payoff)

### 5. Centralize skill lookup and well-known skill names

`Skills.FirstOrDefault(s => s.Name.Equals(..., OrdinalIgnoreCase))` is hand-written in at
least seven places (`SkillsTab`, `SheetSidebar`, `CombatTab` ×2, `WealthTab`,
`CreationOccupationSkillsStep`), and the strings `"Cthulhu Mythos"`, `"Credit Rating"`,
`"Dodge"` are retyped at each site. The max-sanity rule (`99 − Mythos`) is independently
implemented in both `SheetSidebar.SanMax` and `SkillsTab.ImproveSkills`. An
`investigator.FindSkill(name)` extension plus a `WellKnownSkills` constants class plus one
`MaxSanity(investigator)` helper makes rule drift structurally impossible — which matters
here more than in most apps, because a drifted constant is a *rules* bug.

### 6. CombatTab.RollDodge proves the point — it already drifted

`SkillRules.ShouldMarkExperienceCheck` says "Shared by every roll path … so they all tick
identically by construction," and `RollWeapon` uses it — but `CombatTab.RollDodge`
re-implements the check inline and omits the `NonImprovableSkills` guard (harmless for
Dodge today, but the duplication the helper exists to prevent is already there). Two-line
fix; do it when touching the file.

### 7. Give Investigator first-class characteristic access

The "characteristic by name" switch exists three times (`Occupation.Evaluate`,
`CreationCharacteristicsStep.GetCharacteristicByName`, and implicitly in
`StoreBaseValues`), and the "list all eight" enumeration exists three more times (StatsTab,
the creation step defs, `RecomputeDerived`'s locals). Adding
`Investigator.GetCharacteristic(string abbrev)` and
`IEnumerable<Characteristic> Characteristics` collapses all of them, and
`StoreBaseValues`/`RestoreBaseValues`/`StoreHpBaseValues` become one-line loops. New
characteristic-adjacent features then have one integration point instead of six.

### 8. One shared JsonSerializerOptions

The camelCase/case-insensitive options are instantiated in five places:
`InvestigatorService`, `IndexedDbCharacterStore`, `LocalStorageCharacterStore`,
`Roster.razor.cs`, and two ad-hoc variants in `MainLayout` (the import one omits the
camelCase policy and only works because case-insensitivity covers it). Serializer-settings
drift is a data-corruption class of bug; a static `CthulhuJson.Options` (+ an `Export`
variant with `WriteIndented`) ends it.

---

## Tier 3 — Cohesion and cleanup

### 9. Slim InvestigatorService: extract migration, collapse duplicate methods

`InvestigatorService` currently owns store selection, active-character state, roster
maintenance, persistence, *and* ~80 lines of one-time localStorage→IndexedDB migration.
Moving the three `Migrate*` methods into a dedicated `StorageMigrator` makes the service's
real job readable at a glance. Also: `AddAsync` and `ImportAsync` are identical except for
Guid handling (collapse into one), and `LoadAsync` **has zero callers** — delete it.

### 10. Extract the repeated "roll vs threshold" markup into a component

The pattern *if roll ≤ value show green check else red X* is copy-pasted roughly **twelve
times** across `SkillsTab.razor` (3×), `CombatTab.razor` (6×), and `StatsTab.razor` (3×),
each ~8 lines with placeholder-span else-branches. A tiny `ThresholdCheckIcon` component
(params: `Roll`, `Threshold`) removes ~120 lines of markup and guarantees the success/fail
visuals stay identical everywhere — this is the markup-side twin of finding #5. The
bonus/penalty label switch duplicated between `DiceFab` and `RollButton` is the same idea
in miniature.

### 11. Unify default-skill population — it has an inconsistency

`CreationOccupationSkillsStep.PopulateDefaults` and `SkillsTab.LoadDefaultSkills` are
near-identical, except creation sets `IsDefault = true` and the sheet version doesn't — so
the same skill ends up flagged differently depending on where it was added. Move it to
`DefaultSkills.AddMissingTo(investigator)` and decide the `IsDefault` semantics once.

### 12. Small items, worth batching into any nearby PR

- **Dead files/artifacts:** `Helpers/StringOrNumberJsonConverter.cs` is an *empty file*;
  `CthulhuSheets.csproj.lscache` is a tracked build artifact (add to .gitignore).
- **`eval` interop in RollButton:** `OpenPopover` calls
  `JSRuntime.InvokeAsync<double>("eval", "window.innerWidth")` twice. Add a one-line
  `getViewport()` helper to app.js — one round-trip instead of two, and `eval` will bite
  you the day you add a CSP to the PWA.
- **`StateHasChanged` from service events:** `MainLayout` and `Home` invoke
  `StateHasChanged` directly from `OnChanged`; `Roster` correctly wraps in `InvokeAsync`.
  Harmless on WASM today, but worth unifying on the correct pattern.
- **Startup redirect worth double-checking:** `Home.OnInitialized` redirects to the roster
  when `Current is null`, but MainLayout's `RestoreActiveAsync` is still awaiting at that
  moment — so a returning user with an active character may land on the roster instead of
  their sheet. If that's intended as the landing page, fine; if not, gate the redirect on
  initialization having completed.
- **Google Fonts CDN** in index.html: an offline PWA loses Roboto; self-hosting it (or
  relying on MudBlazor's fallbacks) makes offline rendering consistent.
- **Occupations.cs `TODO` about JSON storage:** resolve it the other way — *keep* it as C#
  (typo-proofing via finding #1's cross-validation test beats runtime JSON parsing) and
  delete the TODO.

---

## Suggested order of attack

- #4 and the 1-line import-cap fix from #3 are quick wins.
- #1 + #2 together are the big one (extract, then test).
- #5–#8 are each an hour or less and prevent the bug class this project cares most about.
- Items #5, #6, #8, #11, and the cleanups in #12 are well-scoped enough to hand straight to
  an implementer agent.
