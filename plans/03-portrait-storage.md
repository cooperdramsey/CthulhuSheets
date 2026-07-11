# Portrait Storage — Separate Record + Fix Import/Export Asymmetry — Implementation Plan

> Item #3 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 1.

## Goal

Stop the base64 portrait data URL from being embedded inside the `Investigator` object, where
it (a) is rewritten to storage on **every** field change via `PersistAsync`, (b) forces the
roster page to deserialize every full character just to render thumbnails, and (c) has already
created a **data-loss bug**: portraits can be saved (5 MB upload cap) that can never be
re-imported (1 MB import cap). Move the portrait to its own storage record keyed by character
id, keep it out of the hot per-field save path, and make export/import bundle it losslessly.
Ship a one-line fix for the import-cap trap immediately, independent of the larger refactor.

## Requirements (as given)

From the analysis, item #3:

> - Import/export asymmetry (latent bug): `PortraitDialog` accepts files up to 5 MB but
>   `MainLayout.HandleFileSelected` caps import at 1 MB. A character exported with a large
>   portrait cannot be re-imported.
> - Every save rewrites the portrait. Each `@bind-Value:after="PersistAsync"` reserializes the
>   entire investigator, portrait included, into IndexedDB.
> - The roster page loads every character in full just to show thumbnails.
> The clean fix is a separate portrait record keyed by character id, with export/import
> bundling it. Minimum viable fix: raise the import cap to match the 5 MB upload limit.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Ship the one-line import-cap fix separately from the architectural change?**
   **[DEFAULT] Yes — two deliverables.** Phase A: raise `MainLayout.HandleFileSelected`'s
   `maxAllowedSize` from `1_048_576` to `5 * 1024 * 1024` so the app's own export can be
   re-imported (also add a matching `JsonException`/oversize user-facing message). This is
   one line + a constant and removes the data-loss trap today. Phase B: the separate-record
   refactor. They're independent; Phase A can merge immediately.

2. **Storage shape for the separate portrait record.**
   **[DEFAULT] A dedicated IndexedDB table `portraits`** (id → dataUrl), mirroring the
   existing `characters`/`meta` table pattern in `IndexedDbCharacterStore`. On localStorage,
   a `cthulhu-portrait-{id}` key mirroring `cthulhu-character-{id}`. This keeps the portrait
   physically separate so writing a character no longer rewrites the image, and the roster
   can fetch just portraits.

3. **Does `Investigator.PortraitDataUrl` stay on the model?**
   **[DEFAULT] Remove it from the persisted `Investigator` and route portrait through the
   store instead** — BUT keep backward/JSON-import compatibility: on load/import, if an
   incoming character JSON still carries `portraitDataUrl`, split it out into the portrait
   record. On export, re-inline it into the JSON so exported files remain self-contained and
   openable by older app versions. **This is the crux compatibility decision** — see the
   dedicated saved-character step. **Question for user:** is a self-contained export
   (portrait inlined in the JSON) required, or is a bundle/zip acceptable? Planned assuming
   **inline-on-export, separate-at-rest** (no format change to the exported file).

4. **Portrait size ceiling / downscaling.**
   **[DEFAULT] Keep the current 5 MB upload ceiling; do not add client-side image
   downscaling in this item.** Downscaling (canvas resize before base64) is a real
   improvement but is its own feature with UX implications; note it as a follow-up. This item
   is about *where* the portrait is stored and fixing the asymmetry, not about shrinking it.

5. **What does the roster load now?**
   **[DEFAULT] Only the portrait records**, via a new `GetPortraitAsync(id)` on the store,
   instead of `GetCharacterAsync(id)` per entry. Big win: the roster stops deserializing full
   sheets.

6. **Migration of portraits already embedded in saved characters.**
   **[DEFAULT] Lazy split on first load** (no bulk migration pass): when a character is loaded
   or selected and its JSON still has an inline portrait, extract it to the portrait record
   and re-save the character without the inline field. Simpler and safer than a bulk pass, and
   converges naturally. The existing localStorage→IndexedDB migration path is untouched.

## Alternatives considered

- **Just raise the import cap (Phase A only), stop there.** Rejected as the *whole* answer —
  it fixes the data-loss bug but leaves the per-save-rewrites-the-image and
  roster-loads-everything costs. But it's correct as an independent first deliverable, so the
  plan keeps it as Phase A.
- **Store portraits as Blobs in IndexedDB (not base64 strings).** Rejected for now — Blob
  storage is more efficient but `Magic.IndexedDb`'s model here is string-JSON records, and
  the store's hard-won stability (the read-before-write workaround, see
  `IndexedDbCharacterStore` comments) is around string records. Keep the string dataUrl shape
  to stay on the proven path; revisit if size becomes a problem.
- **Keep `PortraitDataUrl` on the model but skip it during hot saves via a custom
  serializer.** Rejected — fragile (easy to reintroduce), and doesn't help the roster. A
  physically separate record is cleaner and solves all three problems at once.
- **Bundle export as a zip (portrait as a separate file).** Rejected as default — breaks the
  current "one JSON file" simplicity and older-version openability. Inline-on-export keeps
  files self-contained. (Flagged as a user question.)

## Assumptions

- Inline-on-export / separate-at-rest is acceptable (decision #3) so exported files stay
  self-contained and remain importable by any app version.
- The `Magic.IndexedDb` `portraits` table can be declared alongside `characters`/`meta`
  using the same `MagicTableTool`/`IMagicTable` pattern already in
  `IndexedDbCharacterStore.cs`.
- Lazy migration (decision #6) is acceptable vs. a one-time bulk pass.
- No downscaling this item (decision #4).

## Rules touched

**None.** Portraits are not a game mechanic — there is no Call of Cthulhu rule governing
investigator portraits. This item is pure architecture/persistence; `references/rules_condensed/`
is not implicated. (Recorded explicitly so the rules-review pass can confirm "no rules in
scope" rather than hunt for a formula.)

## Affected code

Phase A (ship first):
- `CthulhuSheets/Layout/MainLayout.razor.cs` — raise import `maxAllowedSize` to 5 MB;
  factor the limit into a shared constant with `PortraitDialog`; improve the oversize message.

Phase B (separate record):
- `CthulhuSheets/Services/Storage/ICharacterStore.cs` — add
  `Task<string?> GetPortraitAsync(Guid id)`, `Task SavePortraitAsync(Guid id, string? dataUrl)`,
  `Task DeletePortraitAsync(Guid id)`.
- `CthulhuSheets/Services/Storage/IndexedDbCharacterStore.cs` — new `PortraitRecord` table
  (`portraits`), implement the three methods through the `_gate` + `UpdateRangeAsync`
  (bulkPut / no read-before-write) pattern already used for characters.
- `CthulhuSheets/Services/Storage/LocalStorageCharacterStore.cs` — implement via
  `cthulhu-portrait-{id}` keys; include the prefix in `RemoveAllCthulhuKeysAsync` scope
  (already `cthulhu-`, so covered) and in delete.
- `CthulhuSheets/Services/InvestigatorService.cs` — own the split/merge: on
  write, save the portrait separately and strip it from the character JSON; on read, keep the
  character's in-memory `PortraitDataUrl` populated from the portrait record so the UI is
  unchanged; on delete, delete the portrait too; the lazy migration of inline portraits.
- `CthulhuSheets/Models/Investigator.cs` — `PortraitDataUrl` becomes `[JsonIgnore]` for the
  *at-rest* character JSON but is re-inlined for export (achieve via separate serialize
  options or an export DTO — decide in steps).
- `CthulhuSheets/Pages/Roster/Roster.razor.cs` — fetch portraits via `GetPortraitAsync`
  rather than `GetCharacterAsync`.
- `CthulhuSheets/Shared/PortraitUpload.razor.cs` / `SheetSidebar` / `CreationProfileStep` —
  the `@bind-PortraitDataUrl` UI can stay pointed at the in-memory `Investigator.PortraitDataUrl`;
  the change is that committing it calls a portrait-specific save, not a full `PersistAsync`.

**Saved-character compatibility (mandatory step — this item changes the persisted shape):**
- Existing saves have the portrait **inline** in the character JSON. After the change,
  `Investigator.PortraitDataUrl` is `[JsonIgnore]` at rest, so a naive load would **drop the
  portrait**. The plan's load path must: deserialize the character, then *also* read the
  inline `portraitDataUrl` (via a tolerant read — a separate model or a `JsonExtensionData`
  catch, or deserialize into a DTO that still has the field) and move it into the portrait
  record + in-memory property. This is the highest-risk part and gets its own steps + test.

## Implementation steps

### Phase A — data-loss fix (independent, ship immediately)

1. **Unify the size limit and raise the import cap.** Add a shared `const long
   MaxPortraitBytes = 5 * 1024 * 1024;` (e.g. on `PortraitDialog` or a small `Portraits`
   constants type) and use it in both `PortraitDialog.HandleFileSelected` and
   `MainLayout.HandleFileSelected`. Update the import catch to show a clear "file too large
   (max 5 MB)" message on oversize. **Verify:** export a character with a ~3–4 MB portrait,
   re-import it successfully (previously failed). Rule impact: none.

### Phase B — separate portrait record

2. **Extend `ICharacterStore` with portrait methods** and implement in **both** stores
   (`IndexedDbCharacterStore` new `portraits` table via the existing gated bulkPut pattern;
   `LocalStorageCharacterStore` via `cthulhu-portrait-{id}`). **Verify:** unit/manual round-trip
   save→get→delete of a portrait string in each store.

3. **Route writes through the split in `InvestigatorService`.** In `WriteCharacterAsync`:
   serialize the character **without** the portrait, save that JSON, and separately
   `SavePortraitAsync(id, Current.PortraitDataUrl)`. Keep `Current.PortraitDataUrl` populated
   in memory. **Verify:** editing a non-portrait field writes only the (now portrait-free)
   character JSON; the portrait record is written only when the portrait changes (portrait
   commit calls a dedicated save path — see step 5).

4. **Route reads through the merge.** In `SelectAsync`/`RestoreActiveAsync`/`GetCharacterAsync`:
   after deserializing the character, populate its `PortraitDataUrl` from
   `GetPortraitAsync(id)`; **and** if the deserialized JSON still carried an inline portrait
   (old save), move it into the portrait record and re-save the stripped character (lazy
   migration, decision #6). **Verify:** an old-format saved character (portrait inline) loads
   with its portrait intact, and its stored character JSON no longer contains the inline field
   afterward.

5. **Make `PortraitDataUrl` `[JsonIgnore]` at rest but preserved for export.** Mark the
   property so the at-rest character serializer omits it; for **export**
   (`MainLayout.HandleFileDownload`) use an export path that re-inlines the current portrait
   into the JSON (an export DTO, or a serialize step that adds the field back), so exported
   files stay self-contained. For **import** (`HandleFileSelected` /
   `Roster.LoadSampleAsync`), read the inline portrait from the incoming JSON and hand it to
   `ImportAsync`, which stores it in the portrait record. **Verify:** export → the JSON file
   contains the portrait; import that file on a fresh browser profile → portrait restored;
   the at-rest IndexedDB character record does **not** contain the portrait.

6. **Portrait commit path.** `PortraitUpload.OnChanged`/the sidebar/profile bindings call a
   portrait-specific save (`InvestigatorService.SavePortraitAsync`-style) that writes only the
   portrait record and updates `Current.PortraitDataUrl` — not a full `PersistAsync`.
   **Verify:** changing the portrait writes only the portrait record; changing a stat writes
   only the character record.

7. **Delete cascade.** `DeleteAsync` also calls `DeletePortraitAsync(id)`. **Verify:**
   deleting a character removes both records; no orphaned portrait remains.

8. **Roster loads portraits only.** `Roster.RefreshAsync` uses `GetPortraitAsync(entry.Id)`
   instead of `GetCharacterAsync`. **Verify:** roster thumbnails render; no full-character
   deserialization happens on the roster (confirm by reasoning/logging).

9. **Compatibility test** (in plan #1's test project if present, else a manual checklist):
   round-trip an old-format character (portrait inline) through load → confirm portrait
   preserved and character JSON stripped; round-trip export → import → confirm portrait
   survives; confirm a portrait-less character behaves fine (null portrait).

## Testing / verification

- **Phase A:** the export→import round-trip with a >1 MB portrait now succeeds.
- **Phase B:** the three round-trips in step 9 (old-format load, export/import, delete
  cascade) all pass, in **both** IndexedDB and localStorage fallback modes.
- Confirm editing a stat no longer rewrites the portrait (the character JSON at rest contains
  no base64), and the roster no longer loads full characters.
- Regression: create a new character with a portrait, reload the app, confirm the portrait
  displays on sheet + roster.

## Open risks

- **The at-rest `[JsonIgnore]` + inline-on-export split is the sharp edge.** If step 5 is
  done wrong, either exports lose portraits or at-rest saves keep bloating. The export DTO
  approach (a dedicated serialize that adds the portrait back) is the most explicit; prefer it
  over toggling `[JsonIgnore]` dynamically.
- **Two-store parity.** Every portrait method must work identically in IndexedDB and
  localStorage; the localStorage fallback is the safety net and can't be left half-implemented.
- **`Magic.IndexedDb` new-table declaration** must follow the exact `MagicTableTool` pattern
  and respect the `_gate` serialization + no-read-before-write rule documented in
  `IndexedDbCharacterStore` — a new table that reads-before-write could reintroduce the
  `Arg_NoDefCTor`/`PrematureCommitError` class of bug the comments warn about.
- **Lazy migration writes on load.** Loading an old character now triggers a re-save; ensure
  that re-save can't fail silently and lose data (wrap per the existing storage-error
  handling; on failure, leave the inline portrait in place and retry next load).
- **Sample character** (`wwwroot/samples/Dr.-Eleanor-Whitmore.json`) — confirm its portrait
  (if any) still loads through the import path after the change.
