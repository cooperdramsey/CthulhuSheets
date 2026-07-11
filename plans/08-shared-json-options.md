# One Shared JsonSerializerOptions — Implementation Plan

> Item #8 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 2.

## Goal

Replace the five-plus independently-constructed `JsonSerializerOptions` instances with a
single shared, canonical configuration. Serializer-settings drift is a data-corruption class
of bug — if one code path serializes with camelCase and another reads case-sensitively, saves
silently stop round-tripping. Consolidating to one `CthulhuJson.Options` (plus a `Export`
variant with `WriteIndented`) makes the on-disk/in-storage JSON contract single-sourced.

## Requirements (as given)

From the analysis, item #8:

> The camelCase/case-insensitive options are instantiated in five places: `InvestigatorService`,
> `IndexedDbCharacterStore`, `LocalStorageCharacterStore`, `Roster.razor.cs`, and two ad-hoc
> variants in `MainLayout` (the import one omits the camelCase policy and only works because
> case-insensitivity covers it). Serializer-settings drift is a data-corruption class of bug; a
> static `CthulhuJson.Options` (+ an `Export` variant with `WriteIndented`) ends it.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **What is the canonical config?**
   **[DEFAULT] `PropertyNameCaseInsensitive = true` + `PropertyNamingPolicy =
   JsonNamingPolicy.CamelCase`** — the settings used by the four "real" stores/services. This
   is what every *persisted* value already uses, so adopting it everywhere is a no-op for
   at-rest data. The two `MainLayout` variants (import omits camelCase; export adds
   `WriteIndented`) get normalized onto this base.

2. **How many shared instances?**
   **[DEFAULT] Two static readonly instances on a `CthulhuJson` static class:**
   `Options` (the canonical read/write config) and `Export` (same + `WriteIndented = true` for
   human-readable downloaded files). `JsonSerializerOptions` is thread-safe once configured and
   is intended to be cached/reused, so static readonly is correct and also a minor perf win
   (avoids rebuilding the metadata cache per call).

3. **Where does `CthulhuJson` live?**
   **[DEFAULT] `CthulhuSheets/Helpers/CthulhuJson.cs`**, matching the `Helpers/` convention.

4. **Does normalizing `MainLayout`'s import options change behavior?**
   **[DEFAULT] No observable change, and it closes a latent gap.** The import currently uses
   only `PropertyNameCaseInsensitive = true`. Since incoming files are exported *by this app*
   with camelCase, case-insensitivity happens to cover it — but a value that only differs by
   casing policy on *serialization* isn't exercised on read, so switching import to the shared
   `Options` (which adds the naming policy) is safe and removes the "works by accident"
   fragility. Verify with an export→import round-trip.

5. **Interaction with plan #3 (portraits) export DTO.**
   **[DEFAULT] Compatible.** If #3 introduces an export path that re-inlines the portrait, it
   should serialize with `CthulhuJson.Export`. Note the dependency but don't block on it — #8
   can land first and #3 uses the shared options when it lands.

## Alternatives considered

- **A DI-registered `IOptions<JsonSerializerOptions>` / injected serializer.** Rejected —
  overkill; a static readonly config is the standard .NET pattern for a fixed app-wide JSON
  contract and needs no wiring.
- **One instance for everything (no `Export` variant).** Rejected — the downloaded file should
  stay human-readable (`WriteIndented`), while at-rest storage should stay compact. Two
  instances is the minimum that serves both without per-call option construction.
- **Leave as-is.** Rejected — five hand-maintained copies of a data-contract config is exactly
  the drift risk the item names; one already differs (the import variant).

## Assumptions

- The canonical config (camelCase + case-insensitive) matches what every existing *persisted*
  value uses, so adopting it everywhere leaves at-rest and exported data byte-compatible
  (modulo `WriteIndented` whitespace on exports, which is cosmetic).
- No code path intentionally relies on a *different* JSON config (e.g. PascalCase at rest).
  Verified in the review: the four stores/services all use the same base; only `MainLayout`'s
  import trims it, harmlessly.
- `JsonSerializerOptions` reuse as static readonly is safe (it is, once not mutated after first
  use).

## Rules touched

**None.** Serialization configuration only — no Call of Cthulhu mechanic. (Recorded so the
rules-review pass confirms "no rules in scope.")

## Affected code

New:
- `CthulhuSheets/Helpers/CthulhuJson.cs` — `public static readonly JsonSerializerOptions
  Options` (camelCase + case-insensitive) and `Export` (same + `WriteIndented`).

Changed (replace local option instances with the shared ones):
- `Services/InvestigatorService.cs` — remove the private `JsonOptions`, use `CthulhuJson.Options`.
- `Services/Storage/IndexedDbCharacterStore.cs` — same.
- `Services/Storage/LocalStorageCharacterStore.cs` — same.
- `Pages/Roster/Roster.razor.cs` — same.
- `Layout/MainLayout.razor.cs` — import uses `CthulhuJson.Options`; export uses
  `CthulhuJson.Export`.

**No persisted-model changes.** The at-rest JSON contract is unchanged (same casing/policy);
this only removes duplicate config objects. Existing saves round-trip identically.

## Implementation steps

1. **Add `CthulhuJson`.** Define the two static readonly options. **Verify:** compiles.

2. **Repoint the four stores/services** (`InvestigatorService`, `IndexedDbCharacterStore`,
   `LocalStorageCharacterStore`, `Roster`) to `CthulhuJson.Options`; delete their private
   `JsonOptions` fields. **Verify:** load an existing character and the roster — both
   deserialize exactly as before (no field zeroed, no casing miss).

3. **Repoint `MainLayout`.** Import → `CthulhuJson.Options`; download → `CthulhuJson.Export`.
   **Verify:** export a character (file is indented/readable), re-import it successfully; the
   imported character matches the original field-for-field.

4. **Grep sweep** for any remaining `new JsonSerializerOptions` in the app; repoint or justify.
   **Verify:** the only `JsonSerializerOptions` construction is inside `CthulhuJson`.

## Testing / verification

- Load an existing saved character and the roster after the change — identical deserialization
  (spot-check several fields incl. nested `Wealth`/`Skills`/`Weapons`).
- Export→import round-trip preserves every field; exported file is human-readable (indented).
- `git grep 'new JsonSerializerOptions'` returns only `CthulhuJson.cs`.
- If plan #1's test project exists, add a round-trip test: serialize an `Investigator` with
  `CthulhuJson.Options` and deserialize back to an equal object.

## Open risks

- **A hidden config divergence** would surface as a field failing to round-trip; the step-2/3
  load and round-trip checks are the guard. The known divergence (import omitting camelCase)
  is the one being fixed and is safe because incoming files are self-produced.
- **Static readonly reuse** must not be mutated post-init; keep the options construction inside
  `CthulhuJson`'s initializer only.
- **Ordering vs. plan #3:** if #3 lands first with its own export DTO, ensure it uses
  `CthulhuJson.Export`; if #8 lands first, #3 picks up the shared options. Either order works;
  just don't reintroduce a local options object in #3.
