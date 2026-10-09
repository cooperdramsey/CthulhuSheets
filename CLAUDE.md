# CthulhuSheets — Project Guide

A Call of Cthulhu 7e investigator sheet app.

## Verifying changes

Always verify your changes before reporting them as done:

- **`dotnet build`** must succeed with 0 warnings, 0 errors.
- **`dotnet test`** must pass — run the full test suite to confirm nothing regressed, not just a
  build. Do this for every change with runtime impact, even a small one.

## Architecture & Conventions

A Blazor WebAssembly app in three logical layers, plus tests:

- **Domain / logic** — `CthulhuSheets/Models/`, `Helpers/` and `Data/`. Models, pure rules logic, and static
  rules data. Pulls in no Blazor, `IJSRuntime` or storage types. Rules math (derived stats, skill and sanity
  rules) lives in `Helpers/` — `CharacteristicHelper.cs`, `SkillRules.cs`, `SanityRules.cs`.
- **Services** — `CthulhuSheets/Services/` (`InvestigatorService`, `DiceRollService`) and `Services/Storage/`
  (`ICharacterStore`, `IndexedDbCharacterStore`, `LocalStorageCharacterStore`, `StorageMigrator`).
- **UI** — `Pages/`, `Shared/`, `Layout/`. Components are thin: they render state and forward intent. A feature
  is `X.razor` (markup) + `X.razor.cs` (logic) + a scoped `X.razor.css`.
- **Tests** — `CthulhuSheets.Tests`.

Hard rules, never traded away:

- **Rules math lives in the domain layer, never in components.**
- **Persisted models round-trip as JSON.** Anything reachable from `Investigator` or `Roster` is saved through
  `ICharacterStore` (IndexedDB, falling back to localStorage). A change to these needs a plan for characters
  already saved — a default, or a migration through `StorageMigrator`.
- **Static rules data is code, and a wrong value corrupts every character.** `Data/Occupations.cs` and
  `Data/DefaultSkills.cs` hold printed rules values; check them against the rules reference.
- Async methods carry the `Async` suffix; styling follows the design tokens and MudBlazor palette below.

The rules-bearing parts of the code are mapped file by file in the `rules-review` skill.

## Review & Verification

- **Review skill:** `dotnet-code-review` (the full eight-specialist aggregate). It applies to plans too, in
  Consulting Mode.
- **Conformance review:** `rules-review` checks the code against the Call of Cthulhu 7th Edition rules. The
  condensed rules in `references/rules_condensed/` are the source of truth (fuller text in
  `references/rules_md/`). Run it on any plan or change that touches rules-bearing logic or data.
- **Testing:** new domain logic (`Models`, `Helpers`, `Services`) comes with unit tests in
  `CthulhuSheets.Tests` — that is the point of keeping logic out of components.
- **Done means:** the bar under **Verifying changes** above — `dotnet build` clean and `dotnet test` green.

## Documentation & Vocabulary

- **Living docs:** none beyond this file; keep the architecture notes above current when a system changes.
- **Plans and requirements:** `plans/` (kebab-case slugs) and `plans/requirements/`.
- **Vocabulary** — use the Call of Cthulhu 7e terms exactly and invent no synonyms: Characteristic
  (STR/CON/SIZ/DEX/APP/INT/POW/EDU/LUCK), Skill, Occupation, Sanity, Investigator, Weapon, Damage Bonus, Build,
  Credit Rating.

## Tech & styling architecture

- **Blazor WebAssembly + MudBlazor** (Material Design component library).
- Styling lives in **scoped `.razor.css` files** (one per component, auto-scoped by Blazor) plus the global
  stylesheet [CthulhuSheets/wwwroot/css/app.css](CthulhuSheets/wwwroot/css/app.css).
- **Colors** come from MudBlazor palette tokens (`--mud-palette-*`), e.g. `var(--mud-palette-divider)`,
  `var(--mud-palette-surface)`.
- CSS custom properties declared in `:root` in `app.css` are **globally visible to every scoped `.razor.css`**
  (scoping only rewrites selectors, not `var()` lookups), so the design tokens below work everywhere.

## Spacing / radius / icon scale

Defined in `:root` in [app.css](CthulhuSheets/wwwroot/css/app.css). Use a token for **every padding, margin,
gap, border-radius, and custom icon size** — do not introduce new raw px spacing values.

### Spacing (4px grid; `2xs` is the one deliberate sub-grid value for tight internal rows)

| Token         | Value | Use for |
|---------------|-------|---------|
| `--space-2xs` | 2px   | Micro — tight internal rows (stat-card internals, sidebar rows) |
| `--space-xs`  | 4px   | Tiny gaps |
| `--space-sm`  | 8px   | Small gaps / compact padding |
| `--space-md`  | 12px  | **Standard** — the default gap between peer elements |
| `--space-lg`  | 16px  | Large — card padding, grid gaps |
| `--space-xl`  | 20px  | Extra large |
| `--space-2xl` | 24px  | Massive — section / content padding |

### Border radius

`--radius-sm` 6px · `--radius-md` 8px · `--radius-lg` 10px · `--radius-xl` 12px

### Icon size

`--icon-sm` 16px · `--icon-md` 20px · `--icon-lg` 24px

### Snapping rule (for off-grid legacy values you encounter)

`1–3px → --space-2xs` · `6px → --space-sm` · `10px → --space-sm or --space-md` · `14px → --space-lg` ·
`18px → --space-lg`. Pick the neighbor that best preserves the layout.

### When raw px is still allowed

The scale governs **spacing and corners**, not every dimension. Keep px for:
- Structural/layout dimensions — grid track sizes, `max-width`, fixed component widths/heights
  (e.g. `width: 250px`).
- `font-size` — typography is a separate concern; leave it in `rem`.
- `1px` borders.

### Designer's intent

Prefer **consistency over precision** — when unsure, pick the nearest token. Keep `--space-md` (standard) as
the default gap between peer elements, and reserve larger steps for hierarchically important blocks
(e.g. stat cards) where readability matters more.

### Migration status

The scale is established and applied in `InvestigatorSheet.razor.css` (tab rail, tab buttons, content pane,
outer padding). Other component CSS files still use legacy hardcoded px — **migrate them to tokens whenever you
touch them**, using the snapping rule above.
