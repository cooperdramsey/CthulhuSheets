# Slim InvestigatorService: Extract Migration, Collapse Duplicates — Implementation Plan

> Item #9 from [docs/refactoring-analysis.md](../docs/refactoring-analysis.md). Tier 3.

## Goal

`InvestigatorService` currently owns store selection, active-character state, roster
maintenance, persistence, **and** ~80 lines of one-time localStorage→IndexedDB migration.
Extract the migration into a dedicated `StorageMigrator` so the service's real job (managing
the current character + roster) is readable at a glance; collapse the duplicate
`AddAsync`/`ImportAsync` methods; and delete the dead `LoadAsync`.

## Requirements (as given)

From the analysis, item #9:

> `InvestigatorService` currently owns store selection, active-character state, roster
> maintenance, persistence, and ~80 lines of one-time localStorage→IndexedDB migration. Moving
> the three `Migrate*` methods into a dedicated `StorageMigrator` makes the service's real job
> readable at a glance. Also: `AddAsync` and `ImportAsync` are identical except for Guid
> handling (collapse into one), and `LoadAsync` has zero callers — delete it.

## Decisions (resolved via clarification)

> **User unavailable this session — questions recorded, resolved with `[DEFAULT]`.**

1. **Shape of `StorageMigrator`.**
   **[DEFAULT] A class taking the two concrete stores** (`IndexedDbCharacterStore`,
   `LocalStorageCharacterStore`) and exposing one entry point, e.g.
   `Task<MigrationOutcome> MigrateIfNeededAsync()`, that encapsulates the current
   `MigrateFromLocalStorageAsync` → `MigrateRosterAsync`/`MigrateLegacyCharacterAsync` chain.
   It reports success/failure so the service can raise `OnStorageError` with the same message.
   The migrator does **not** hold `Current`/`Roster` state — it only moves data between stores.

2. **Where does store *selection* live** (the `TryInitializeAsync`→pick IndexedDb-or-local
   logic in `InitializeAsync`)?
   **[DEFAULT] Keep store selection in `InvestigatorService.InitializeAsync`**, but have it
   call `StorageMigrator.MigrateIfNeededAsync()` after selecting the store. Store selection is
   part of the service's lifecycle; only the migration *mechanics* move out. **Question for
   user:** extract selection too (into a `StoreProvider`)? Planned to keep selection in the
   service — it's small and lifecycle-bound; over-extracting adds indirection.

3. **How to signal migration errors** now that the migrator is separate?
   **[DEFAULT] The migrator returns an outcome; the service maps failure to
   `OnStorageError?.Invoke(sameMessage)`.** Keep the exact current user-facing message
   ("Character data migration to IndexedDB failed — your characters are safe and will retry
   next launch."). The event stays on the service (the UI subscribes there).

4. **Collapsing `AddAsync`/`ImportAsync`.**
   **[DEFAULT] One private core `SaveNewAsync(Investigator c, bool assignFreshId)`**; `AddAsync`
   calls it with `assignFreshId: true` (always new Guid), `ImportAsync` with the current
   "assign only if empty" rule. Both then do the identical write→upsert→activate→save→events
   sequence. Preserve the exact current semantics: `AddAsync` always sets a new Guid;
   `ImportAsync` sets one only if `Id == Guid.Empty`.

5. **`LoadAsync` deletion — confirm truly dead.**
   **[DEFAULT] Delete it.** Verified zero callers in the review (`LoadAsync` only forwards to
   `ImportAsync`). Removing it is safe. If a future caller wanted it, `ImportAsync` is the
   public entry.

6. **Behavior change?**
   **[DEFAULT] None.** Migration outcomes, error messages, Guid handling, and event firing are
   all preserved exactly. This is a cohesion refactor.

## Alternatives considered

- **Leave migration inline.** Rejected — it's the bulk of the file and runs once ever;
  isolating it makes the always-relevant code (current char/roster) the focus.
- **Extract everything (selection + migration + persistence) into separate services.** Rejected
  as over-engineering for the file's size; migration is the cohesive chunk worth isolating.
  Store selection stays (decision #2).
- **Keep `AddAsync`/`ImportAsync` separate.** Rejected — they're identical but for Guid
  handling; one core method with a flag removes the copy while preserving both entry points'
  semantics.

## Assumptions

- `LoadAsync` has zero callers (verified). Deleting it breaks nothing.
- The migration logic can move verbatim into `StorageMigrator`, still calling the two concrete
  stores' methods (`GetRosterAsync`, `SaveCharacterJsonAsync`, `RemoveAllCthulhuKeysAsync`,
  `GetLegacyCharacterJsonAsync`, the verification re-read). It uses the shared JSON options
  (plan #8's `CthulhuJson.Options` if landed; else its own, matched to current).
- The migration's verification-and-cleanup ordering (write to IndexedDb, re-read to verify,
  only then wipe localStorage) is safety-critical and must be preserved exactly.

## Rules touched

**None.** Persistence/lifecycle plumbing; no game mechanic. (The migration moves *character
JSON*, but does not interpret any rule.)

## Affected code

New:
- `CthulhuSheets/Services/Storage/StorageMigrator.cs` — the three `Migrate*` methods, an entry
  point, and an outcome type. Registered in DI (`Program.cs`) as scoped.

Changed:
- `CthulhuSheets/Services/InvestigatorService.cs` — remove the three `Migrate*` methods; call
  the migrator from `InitializeAsync`; collapse `AddAsync`/`ImportAsync` into a shared core;
  delete `LoadAsync`. Keep `Current`/`Roster` state, events, `RestoreActiveAsync`,
  `SelectAsync`, `PersistAsync`, `DeleteAsync`, `GetCharacterAsync`, roster helpers.
- `CthulhuSheets/Program.cs` — register `StorageMigrator`.

**Persisted-model:** unchanged. **Persisted-data:** the migration path must remain
behavior-identical (it's a one-time data move that existing users may still hit). Treat its
preservation as a compatibility concern (see steps).

## Implementation steps

1. **Create `StorageMigrator`** with the three methods moved verbatim (adjust field access:
   they now take/hold the two stores and the JSON options). Add `MigrateIfNeededAsync()`
   wrapping the current `MigrateFromLocalStorageAsync` guard logic, returning an outcome
   (success / failed-with-message). **Verify:** compiles; the moved code is line-for-line the
   same logic (same guards, same verification re-read, same localStorage wipe ordering).

2. **Register `StorageMigrator` in `Program.cs`** (scoped, after the two stores). **Verify:**
   DI resolves.

3. **Repoint `InvestigatorService.InitializeAsync`** to call the migrator after store
   selection; map a failed outcome to `OnStorageError` with the identical message. Remove the
   three `Migrate*` methods from the service. **Verify:** on a profile with legacy localStorage
   data, migration still runs and succeeds (and, if forced to fail, surfaces the same
   snackbar). On a clean profile, no migration runs.

4. **Collapse `AddAsync`/`ImportAsync`** into a shared private core with the Guid flag; keep
   both public methods delegating. **Verify:** creating a new character (AddAsync path) assigns
   a fresh Guid and activates it; importing a file with an existing Id keeps it, with
   `Guid.Empty` gets a new one — identical to before.

5. **Delete `LoadAsync`.** **Verify:** solution builds with no unresolved references.

6. **Regression pass.** Exercise: fresh install (no data), install with legacy single-character
   localStorage, install with a localStorage roster, create/import/select/delete a character.
   **Verify:** all behave exactly as before; migration is one-time and idempotent (re-running
   `InitializeAsync` doesn't re-migrate or duplicate).

## Testing / verification

- Migration scenarios (legacy single char; localStorage roster; already-migrated; clean) all
  behave as before, including the failure→snackbar path and the "safe, retries next launch"
  guarantee.
- Create/import/select/delete flows unchanged; events (`OnChanged`/`OnRosterChanged`) fire the
  same.
- `git diff` shows the migration code *moved*, not modified; Guid handling for
  Add/Import preserved.
- Build clean after `LoadAsync` removal.

## Open risks

- **The migration is safety-critical and hard to test locally** (needs specific legacy
  localStorage states). The mitigation is to move it *verbatim* — no logic edits — and to
  manually reproduce the legacy states in a browser profile (seed localStorage keys by hand)
  for the regression pass. Do not "improve" the migration while moving it.
- **Ordering of verify-then-wipe** must be preserved exactly; a reordering could wipe
  localStorage before confirming the IndexedDB write, risking data loss. Call this out in
  review of the moved code.
- **One-time nature:** ensure the extracted entry point keeps the same guard
  (`idbRoster has entries → skip`) so it stays idempotent.
