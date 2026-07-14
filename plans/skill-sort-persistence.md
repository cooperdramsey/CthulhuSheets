# Skill Sort Persistence — Implementation Plan

## Goal
Make the Skills-tab **sort order** persist with the character. Today the sort mode is
component-local state that resets whenever the Skills tab is left and re-entered (the tab
components are created/destroyed by the `@switch` in `InvestigatorSheet.razor`), and it is
never saved. This feature stores the chosen sort on the saved character so it survives tab
switches, character switches, and sessions — "if I sort high-to-low, it stays that way until
I change it." It also introduces a per-character `CharacterPreferences` container so future UI
preferences have a home, and adds a global string-enum JSON converter so enums serialize as
readable names everywhere.

## Requirements (as given)
> I want to make the sorting settings from the skills tab persist with the character. Let's add
> a character preferences section to the saved character data so that between sessions the
> selection persists. For example, if I sort my skills high to low, it should remain that way
> until I change it.

## Decisions (resolved via clarification)
1. **What persists:** *Sort order only.* The `Min ≥` numeric filter and the free-text search
   box remain transient (they reset each session / on load), matching their nature as ad-hoc
   lookups. Only the three-way sort choice is saved.
2. **Model shape:** *A `CharacterPreferences` container.* Add `public CharacterPreferences
   Preferences { get; set; } = new();` to `Investigator`, holding the sort mode (and room for
   future per-character UI prefs), rather than a flat field on `Investigator`.
3. **Enum JSON format:** *String names, as global middleware.* Add a `JsonStringEnumConverter`
   to the shared `CthulhuJson` configs so all enums serialize as strings by default. The
   built-in converter still **reads** numeric values, so no existing save is broken. Applied to
   **both** `CthulhuJson.Options` (at-rest + import) and `CthulhuJson.Export` (downloaded files).
4. **Default sort:** *A → Z alphabetical.* An unset preference (existing saves, new characters)
   sorts alphabetically — identical to today's behavior — until the user picks otherwise.
5. **Enum value names:** *Friendly names* — `Alphabetical`, `HighestFirst`, `LowestFirst` — so
   they read clearly in exported JSON. The existing private `SortMode` enum (`Alpha`,
   `RegularDesc`, `RegularAsc`) is replaced by this shared public enum.

## Alternatives considered
- **Flat `SkillSortMode` property directly on `Investigator`** (rejected). Simpler now, but
  clutters the top-level model and gives future per-character UI prefs (combat sort, default
  tab, etc.) no natural home. The container costs one small class and one property.
- **Per-property `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute on the enum**
  instead of a global converter (rejected). Works, but the user explicitly asked for the
  string-enum behavior as shared middleware/default, and a global converter also future-proofs
  every enum the model gains later without per-site annotation.
- **Global app setting (one sort for all characters)** instead of per-character (rejected). The
  requirement is explicit that the setting lives "with the character" in the saved character
  data.
- **A dedicated schema-version + migration step** for the new field (rejected as unnecessary).
  Because `Preferences` defaults to `new()`, a save that lacks the property deserializes to the
  default (Alphabetical) with no code change. See "Saved-character compatibility" below.

## Assumptions
- None outstanding. (The `Min ≥` filter and text-search box are intentionally *not* persisted
  per Decision 1; the character-creation occupation-skills step sorts alphabetically only and
  has no user sort control, so it is out of scope.)

## Rules touched
This feature is **pure UI state + persistence** and implicates **no Call of Cthulhu 7e game
mechanic** — it changes neither values, thresholds, nor formulas. The only rules-adjacent
detail is that the "Highest / Lowest value first" sort keys off `Skill.EffectiveRegular`
(`Regular ?? BaseValue`, [Skill.cs](../CthulhuSheets/Models/Skill.cs#L11-L12)), which is the
correct effective skill value and is already used unchanged by the current sort. No condensed
rules file governs display ordering; nothing in `references/rules_condensed/` is affected.

## Affected code
| File | Role in this change |
|---|---|
| `CthulhuSheets/Helpers/CthulhuJson.cs` | Add `JsonStringEnumConverter` to `Options` and `Export`. |
| `CthulhuSheets/Models/CharacterPreferences.cs` *(new)* | New `SkillSortMode` enum + `CharacterPreferences` class. |
| `CthulhuSheets/Models/Investigator.cs` | Add `Preferences` property defaulting to `new()`. |
| `CthulhuSheets/Pages/Home/Components/SkillsTab.razor.cs` | Drop local `SortMode`/`_sortMode`; read/write `Investigator.Preferences.SkillSort`; add persisting `SetSort`. |
| `CthulhuSheets/Pages/Home/Components/SkillsTab.razor` | Point the sort menu's active-state, color, and click handlers at the persisted preference. |
| `CthulhuSheets.Tests/CthulhuJsonTests.cs` | Add enum-as-string + numeric-read-compat + missing-Preferences-default tests. |

## Implementation steps

1. **Add the global string-enum converter to the JSON contract.**
   In [CthulhuJson.cs](../CthulhuSheets/Helpers/CthulhuJson.cs), add
   `Converters = { new JsonStringEnumConverter() }` (namespace
   `System.Text.Json.Serialization`) to **both** the `Options` and `Export` initializers, so
   every enum serializes as its string name in at-rest, imported, and exported JSON.
   *Why it's safe:* `JsonStringEnumConverter` deserializes both string names **and** raw
   numbers, so any pre-existing save that ever stored a numeric enum still reads. Update the
   explanatory comment to note enums are written as names.
   *Verify:* `dotnet build`; existing `CthulhuJsonTests` still pass.

2. **Create the `CharacterPreferences` model and `SkillSortMode` enum.**
   New file [Models/CharacterPreferences.cs](../CthulhuSheets/Models/CharacterPreferences.cs):
   ```csharp
   namespace CthulhuSheets.Models;

   public enum SkillSortMode
   {
       Alphabetical,   // A → Z (default)
       HighestFirst,   // EffectiveRegular descending
       LowestFirst     // EffectiveRegular ascending
   }

   public class CharacterPreferences
   {
       // Default matches today's Skills-tab behavior (A → Z).
       public SkillSortMode SkillSort { get; set; } = SkillSortMode.Alphabetical;
   }
   ```
   *Verify:* `dotnet build`.

3. **Add `Preferences` to `Investigator` (saved-character compatibility).**
   In [Investigator.cs](../CthulhuSheets/Models/Investigator.cs), add near the other persisted
   sections:
   ```csharp
   // Per-character UI preferences (sort order, etc.). Defaults to new() so saves
   // written before this field existed deserialize to the defaults (Alphabetical).
   public CharacterPreferences Preferences { get; set; } = new();
   ```
   *Saved-character compatibility:* `Investigator` round-trips as camelCase JSON via
   `CthulhuJson.Options` through `ICharacterStore`. A stored JSON that predates this field
   simply omits `"preferences"`; System.Text.Json leaves the property at its initialized
   `new()` value (`SkillSort = Alphabetical`). No migration or `StorageMigrator` change is
   required. New characters created via `AddAsync`/creation flow also start Alphabetical.
   *Export/import path:* the download flow in
   [MainLayout.razor.cs](../CthulhuSheets/Layout/MainLayout.razor.cs#L82-L88) serializes via
   `CthulhuJson.Export` (which gets the converter in step 1), then re-parses only to re-inject
   the portrait — it doesn't touch `preferences`, so exported files carry
   `"preferences":{"skillSort":"…"}` correctly, and imported files lacking it default as above.
   No import/export code change is needed.
   *Verify:* `dotnet build`; round-trip test in step 6.

4. **Rewire `SkillsTab` (code-behind + markup) to the persisted preference — one atomic change.**
   The `SortMode` enum is referenced in **both** the code-behind and the `.razor` markup, so the
   code-behind and markup edits must land together; the build only compiles once both are done
   (do not run `dotnet build` between the two halves — it will fail on the still-dangling
   `SortMode` reference in the markup).

   *Code-behind* — [SkillsTab.razor.cs](../CthulhuSheets/Pages/Home/Components/SkillsTab.razor.cs):
   - Delete `private enum SortMode { Alpha, RegularDesc, RegularAsc }` and the
     `private SortMode _sortMode = SortMode.Alpha;` field.
   - In `VisibleSkills`, switch on `Investigator.Preferences.SkillSort`:
     ```csharp
     return Investigator.Preferences.SkillSort switch
     {
         SkillSortMode.HighestFirst => skills.OrderByDescending(s => s.EffectiveRegular).ThenBy(s => s.Name),
         SkillSortMode.LowestFirst  => skills.OrderBy(s => s.EffectiveRegular).ThenBy(s => s.Name),
         _                          => skills.OrderBy(s => s.Name),
     };
     ```
   - Add a persisting setter:
     ```csharp
     private async Task SetSort(SkillSortMode mode)
     {
         Investigator.Preferences.SkillSort = mode;
         await PersistAsync();
     }
     ```
   `SkillSortMode` resolves via the existing global `using`/model namespace already covering
   `Investigator` and `Skill` in this file (confirm the file compiles; add
   `using CthulhuSheets.Models;` only if the build reports it missing).

   *Markup* — [SkillsTab.razor](../CthulhuSheets/Pages/Home/Components/SkillsTab.razor) toolbar
   sort `MudMenu` (lines ~26–49):
   - Menu `Color`: `@(Investigator.Preferences.SkillSort == SkillSortMode.Alphabetical ? Color.Default : Color.Primary)`.
   - Each `MudMenuItem` `OnClick` → `@(() => SetSort(SkillSortMode.Alphabetical))` /
     `SkillSortMode.HighestFirst` / `SkillSortMode.LowestFirst` (MudBlazor accepts an async
     `Task`-returning handler).
   - Each item's `--active` class check → compare against
     `Investigator.Preferences.SkillSort == SkillSortMode.<value>`.
   Leave the `Min ≥` numeric field and text filter bindings untouched (they stay transient).
   *Verify (once both halves are done):* `dotnet build` (0 warnings, 0 errors).

5. **Manual behavior check.**
   Run the app (`/run` or `dotnet run`): on a character, set sort to "Highest value first";
   switch to another tab and back → still Highest; switch to another character and back → that
   character's own saved sort; reload the page → sort restored. Confirm a second character keeps
   an independent sort. Confirm the `Min ≥`/text filters reset as before.

6. **Tests.**
   In [CthulhuJsonTests.cs](../CthulhuSheets.Tests/CthulhuJsonTests.cs) add:
   - **Enum serializes as string:** serialize an `Investigator` with
     `Preferences.SkillSort = SkillSortMode.HighestFirst` via `CthulhuJson.Options`; assert the
     JSON contains `"skillSort":"HighestFirst"` (and not `"skillSort":1`).
   - **Numeric read still works (back-compat):** deserialize
     `{"preferences":{"skillSort":1}}`; assert `SkillSort == HighestFirst`.
   - **Missing `preferences` defaults to Alphabetical:** deserialize a minimal
     `{"name":"X"}`; assert `restored.Preferences.SkillSort == SkillSortMode.Alphabetical`.
   *Verify:* `dotnet test` — full suite green.

## Testing / verification
- `dotnet build` → 0 warnings, 0 errors.
- `dotnet test` → full suite passes, including the three new JSON tests.
- Manual (step 5): sort persists across tab switch, character switch, and page reload; each
  character has an independent sort; transient filters still reset; an exported character file
  shows `"skillSort":"HighestFirst"` (string) under `"preferences"`.

## Open risks
- **Global converter is app-wide.** Adding `JsonStringEnumConverter` to `CthulhuJson` changes
  how *every* enum in the persisted/exported graph is written. Today no enum is actually
  serialized (`SortMode` and `CreationMethod` are component-local), so there is nothing to
  break; but any future serialized enum will now be string-formatted by default — intended, and
  the reason for doing it as middleware.
- **`MudMenuItem` async `OnClick`.** The click handlers become async (`SetSort` returns
  `Task`). MudBlazor supports async event callbacks; verify no analyzer warning about an
  un-awaited call — using `@(() => SetSort(...))` returns the `Task` to the `EventCallback`,
  which awaits it. If a warning appears, wrap as `@(async () => await SetSort(...))`.
