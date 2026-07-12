using CthulhuSheets.Helpers;

namespace CthulhuSheets.Services.Storage;

// Outcome of the one-time localStorage -> IndexedDB migration. On failure the
// caller surfaces the message to the user; the data is left intact and the
// migration retries on the next launch.
public readonly record struct MigrationOutcome(bool Failed, string? Message)
{
    public static readonly MigrationOutcome Success = new(false, null);

    public static MigrationOutcome Fail() => new(true,
        "Character data migration to IndexedDB failed — your characters are safe and will retry next launch.");
}

// One-time migration of character data from the legacy localStorage layout to
// IndexedDB. Moves data between the two stores only — it holds no
// current-character/roster state. The verify-then-wipe ordering (write to
// IndexedDB, re-read to confirm, only then clear localStorage) is
// safety-critical and must be preserved: wiping before confirming the write
// would risk data loss.
public class StorageMigrator(
    IndexedDbCharacterStore indexedDb,
    LocalStorageCharacterStore localStorage)
{
    // Migrates if IndexedDB is empty and localStorage holds legacy data.
    // Idempotent: if IndexedDB already has a roster with entries, does nothing.
    public async Task<MigrationOutcome> MigrateIfNeededAsync()
    {
        var idbRoster = await indexedDb.GetRosterAsync();
        if (idbRoster is not null && idbRoster.Entries.Count > 0) return MigrationOutcome.Success;

        var lsRoster = await localStorage.GetRosterAsync();

        if (lsRoster is not null && lsRoster.Entries.Count > 0)
            return await MigrateRosterAsync(lsRoster);

        var legacyJson = await localStorage.GetLegacyCharacterJsonAsync();
        if (!string.IsNullOrEmpty(legacyJson))
            return await MigrateLegacyCharacterAsync(legacyJson);

        return MigrationOutcome.Success;
    }

    private async Task<MigrationOutcome> MigrateRosterAsync(Roster lsRoster)
    {
        try
        {
            foreach (var entry in lsRoster.Entries)
            {
                var json = await localStorage.GetCharacterJsonAsync(entry.Id);
                if (!string.IsNullOrEmpty(json))
                    await indexedDb.SaveCharacterJsonAsync(entry.Id, json);

                // Portraits written by the new code live in a separate localStorage
                // key, not inside the character JSON — copy them across before the
                // wipe below, else they're lost. (Old inline portraits ride along in
                // the character JSON above and get lazy-split on first load.)
                var portrait = await localStorage.GetPortraitAsync(entry.Id);
                if (!string.IsNullOrEmpty(portrait))
                    await indexedDb.SavePortraitAsync(entry.Id, portrait);
            }
            await indexedDb.SaveRosterAsync(lsRoster);

            var verifiedRoster = await indexedDb.GetRosterAsync();
            if (verifiedRoster is null || verifiedRoster.Entries.Count != lsRoster.Entries.Count)
                throw new InvalidOperationException("IndexedDB migration verification failed.");

            await localStorage.RemoveAllCthulhuKeysAsync();
            return MigrationOutcome.Success;
        }
        catch (Exception)
        {
            return MigrationOutcome.Fail();
        }
    }

    private async Task<MigrationOutcome> MigrateLegacyCharacterAsync(string legacyJson)
    {
        try
        {
            var investigator = JsonSerializer.Deserialize<Investigator>(legacyJson, CthulhuJson.Options);
            if (investigator is null) return MigrationOutcome.Success;

            investigator.Id = Guid.NewGuid();
            // PortraitDataUrl is [JsonIgnore], so deserialize dropped any inline
            // portrait — recover it from the raw legacy JSON and persist it to the
            // separate portrait record, else the migration wipe destroys it.
            var portrait = InvestigatorService.ExtractInlinePortrait(legacyJson);
            var json = JsonSerializer.Serialize(investigator, CthulhuJson.Options);

            var entry = new RosterEntry
            {
                Id = investigator.Id,
                Name = investigator.Name,
                Occupation = investigator.Occupation,
                LastModified = DateTimeOffset.UtcNow
            };

            var roster = new Roster
            {
                ActiveId = investigator.Id,
                Entries = [entry]
            };

            await indexedDb.SaveCharacterJsonAsync(investigator.Id, json);
            await indexedDb.SavePortraitAsync(investigator.Id, portrait);
            await indexedDb.SaveRosterAsync(roster);

            var verifiedRoster = await indexedDb.GetRosterAsync();
            if (verifiedRoster is null || verifiedRoster.Entries.Count == 0)
                throw new InvalidOperationException("Legacy migration verification failed.");

            await localStorage.RemoveAllCthulhuKeysAsync();
            return MigrationOutcome.Success;
        }
        catch (Exception)
        {
            return MigrationOutcome.Fail();
        }
    }
}
