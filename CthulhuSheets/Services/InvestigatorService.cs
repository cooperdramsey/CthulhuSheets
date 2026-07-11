using System.Text.Json.Nodes;
using CthulhuSheets.Services.Storage;

namespace CthulhuSheets.Services;

public class InvestigatorService(
    IndexedDbCharacterStore indexedDb,
    LocalStorageCharacterStore localStorage)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ICharacterStore _store = localStorage;

    public Investigator? Current { get; private set; }
    public Roster Roster { get; private set; } = new();

    public event Action? OnChanged;
    public event Action? OnRosterChanged;
    public event Action<string>? OnStorageError;

    public IEnumerable<RosterEntry> GetEntriesOrdered() =>
        Roster.Entries.OrderByDescending(e => e.LastModified);

    // --- Initialization ---

    public async Task InitializeAsync()
    {
        if (await indexedDb.TryInitializeAsync())
        {
            _store = indexedDb;
            await _store.RequestPersistAsync();
        }
        else
        {
            _store = localStorage;
            await localStorage.TryInitializeAsync();
        }

        await MigrateFromLocalStorageAsync();
        await LoadRosterAsync();
        OnRosterChanged?.Invoke();
    }

    public async Task RestoreActiveAsync()
    {
        if (Roster.ActiveId is null) return;

        var id = Roster.ActiveId.Value;
        var json = await _store.GetCharacterJsonAsync(id);
        if (string.IsNullOrEmpty(json))
        {
            RemoveEntry(id);
            Roster.ActiveId = null;
            await _store.SaveRosterAsync(Roster);
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
            if (Current is not null) await HydratePortraitAsync(id, Current, json);
            OnChanged?.Invoke();
        }
        catch (JsonException)
        {
            // Never delete the stored JSON on a parse failure — a schema change
            // could make every save unreadable and deleting would destroy them.
            // Deactivate it and leave the data intact for a future app version.
            Roster.ActiveId = null;
            await _store.SaveRosterAsync(Roster);
            OnStorageError?.Invoke(
                "A saved character couldn't be read. Its data was left untouched — it may load again after an app update.");
        }
    }

    // --- Character operations ---

    public async Task AddAsync(Investigator c)
    {
        c.Id = Guid.NewGuid();
        await WriteCharacterAsync(c);
        await _store.SavePortraitAsync(c.Id, c.PortraitDataUrl);
        UpsertEntry(c);
        Roster.ActiveId = c.Id;
        Current = c;
        await _store.SaveRosterAsync(Roster);
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task ImportAsync(Investigator c)
    {
        if (c.Id == Guid.Empty)
            c.Id = Guid.NewGuid();

        await WriteCharacterAsync(c);
        await _store.SavePortraitAsync(c.Id, c.PortraitDataUrl);
        UpsertEntry(c);
        Roster.ActiveId = c.Id;
        Current = c;
        await _store.SaveRosterAsync(Roster);
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task SelectAsync(Guid id)
    {
        var json = await _store.GetCharacterJsonAsync(id);
        if (string.IsNullOrEmpty(json))
        {
            RemoveEntry(id);
            if (Roster.ActiveId == id) Roster.ActiveId = null;
            await _store.SaveRosterAsync(Roster);
            OnRosterChanged?.Invoke();
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
            if (Current is not null) await HydratePortraitAsync(id, Current, json);
            Roster.ActiveId = id;
            await _store.SaveRosterAsync(Roster);
            OnChanged?.Invoke();
        }
        catch (JsonException)
        {
            // Keep the stored JSON — see RestoreActiveAsync.
            OnStorageError?.Invoke(
                "That character couldn't be read. Its data was left untouched — it may load again after an app update.");
        }
    }

    public async Task PersistAsync()
    {
        if (Current is null) return;
        try
        {
            await WriteCharacterAsync(Current);
            UpsertEntry(Current);
            await _store.SaveRosterAsync(Roster);
        }
        catch (JSException)
        {
            OnStorageError?.Invoke("Couldn't save your character — browser storage is full. Try a smaller portrait or export to a file.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _store.DeleteCharacterAsync(id);
        await _store.DeletePortraitAsync(id);
        RemoveEntry(id);
        if (Roster.ActiveId == id)
        {
            Roster.ActiveId = null;
            Current = null;
        }
        await _store.SaveRosterAsync(Roster);
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task LoadAsync(Investigator investigator) => await ImportAsync(investigator);

    public async Task SavePortraitAsync(string? dataUrl)
    {
        if (Current is null) return;
        Current.PortraitDataUrl = dataUrl;
        try
        {
            await _store.SavePortraitAsync(Current.Id, dataUrl);
        }
        catch (JSException)
        {
            OnStorageError?.Invoke("Couldn't save your portrait — browser storage is full. Try a smaller image.");
        }
        OnChanged?.Invoke();
    }

    public Task<string?> GetPortraitAsync(Guid id) => _store.GetPortraitAsync(id);

    // --- Private helpers ---

    private async Task LoadRosterAsync()
    {
        Roster = await _store.GetRosterAsync() ?? new Roster();
    }

    // Writes only the character JSON (portrait is [JsonIgnore] at rest). The
    // portrait record is written separately on create/import and on an explicit
    // portrait change (SavePortraitAsync) — never on a field-edit PersistAsync,
    // so editing a stat no longer rewrites the whole base64 portrait.
    private async Task WriteCharacterAsync(Investigator c)
    {
        var json = JsonSerializer.Serialize(c, JsonOptions);
        await _store.SaveCharacterJsonAsync(c.Id, json);
    }

    // Reads an inline "portraitDataUrl" out of a raw character JSON (pre-Phase-B
    // saves and imported/exported files carry it inline). Returns null if absent
    // or malformed. Shared by the lazy-migration path and the import/sample UI.
    public static string? ExtractInlinePortrait(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            return node?["portraitDataUrl"]?.GetValue<string?>();
        }
        catch { return null; }
    }

    // Lazy-migrates a character JSON that still has the portrait embedded
    // inline (pre-Phase-B save) by splitting it into the portrait store and
    // re-saving the now-stripped character JSON. Never throws — a failure to
    // re-save just means the inline portrait stays in the old JSON and the
    // migration retries on next load.
    private async Task HydratePortraitAsync(Guid id, Investigator investigator, string json)
    {
        var inline = ExtractInlinePortrait(json);
        try
        {
            if (!string.IsNullOrEmpty(inline))
            {
                investigator.PortraitDataUrl = inline;
                await _store.SavePortraitAsync(id, inline);
                await _store.SaveCharacterJsonAsync(id, JsonSerializer.Serialize(investigator, JsonOptions));
            }
            else
            {
                investigator.PortraitDataUrl = await _store.GetPortraitAsync(id);
            }
        }
        catch
        {
            // Portrait hydration/migration is best-effort and must never fail a
            // character load. On a storage error the in-memory portrait is set
            // from the inline value if we had one (so the UI still shows it), and
            // the inline JSON is left intact so the split retries on next load.
            investigator.PortraitDataUrl ??= inline;
        }
    }

    private async Task MigrateFromLocalStorageAsync()
    {
        if (_store is not IndexedDbCharacterStore) return;

        var idbRoster = await indexedDb.GetRosterAsync();
        if (idbRoster is not null && idbRoster.Entries.Count > 0) return;

        var lsRoster = await localStorage.GetRosterAsync();

        if (lsRoster is not null && lsRoster.Entries.Count > 0)
        {
            await MigrateRosterAsync(lsRoster);
            return;
        }

        var legacyJson = await localStorage.GetLegacyCharacterJsonAsync();
        if (!string.IsNullOrEmpty(legacyJson))
            await MigrateLegacyCharacterAsync(legacyJson);
    }

    private async Task MigrateRosterAsync(Roster lsRoster)
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
        }
        catch (Exception)
        {
            OnStorageError?.Invoke("Character data migration to IndexedDB failed — your characters are safe and will retry next launch.");
        }
    }

    private async Task MigrateLegacyCharacterAsync(string legacyJson)
    {
        try
        {
            var investigator = JsonSerializer.Deserialize<Investigator>(legacyJson, JsonOptions);
            if (investigator is null) return;

            investigator.Id = Guid.NewGuid();
            // PortraitDataUrl is [JsonIgnore], so deserialize dropped any inline
            // portrait — recover it from the raw legacy JSON and persist it to the
            // separate portrait record, else the migration wipe destroys it.
            var portrait = ExtractInlinePortrait(legacyJson);
            var json = JsonSerializer.Serialize(investigator, JsonOptions);

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
        }
        catch (Exception)
        {
            OnStorageError?.Invoke("Character data migration to IndexedDB failed — your characters are safe and will retry next launch.");
        }
    }

    private void UpsertEntry(Investigator c)
    {
        var existing = Roster.Entries.FirstOrDefault(e => e.Id == c.Id);
        if (existing is not null)
        {
            existing.Name = c.Name;
            existing.Occupation = c.Occupation;
            existing.LastModified = DateTimeOffset.UtcNow;
        }
        else
        {
            Roster.Entries.Add(new RosterEntry
            {
                Id = c.Id,
                Name = c.Name,
                Occupation = c.Occupation,
                LastModified = DateTimeOffset.UtcNow
            });
        }
    }

    private void RemoveEntry(Guid id) =>
        Roster.Entries.RemoveAll(e => e.Id == id);
}
