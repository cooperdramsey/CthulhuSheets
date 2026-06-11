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
            OnChanged?.Invoke();
        }
        catch (JsonException)
        {
            RemoveEntry(id);
            Roster.ActiveId = null;
            await _store.DeleteCharacterAsync(id);
            await _store.SaveRosterAsync(Roster);
            OnStorageError?.Invoke("A saved character couldn't be read and was removed from the roster.");
        }
    }

    // --- Character operations ---

    public async Task AddAsync(Investigator c)
    {
        c.Id = Guid.NewGuid();
        await WriteCharacterAsync(c);
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
            Roster.ActiveId = id;
            await _store.SaveRosterAsync(Roster);
            OnChanged?.Invoke();
        }
        catch (JsonException)
        {
            RemoveEntry(id);
            Roster.ActiveId = null;
            await _store.DeleteCharacterAsync(id);
            await _store.SaveRosterAsync(Roster);
            OnStorageError?.Invoke("That character couldn't be read and was removed.");
            OnRosterChanged?.Invoke();
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

    public async Task<Investigator?> GetCharacterAsync(Guid id)
    {
        var json = await _store.GetCharacterJsonAsync(id);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Investigator>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    public async Task LoadAsync(Investigator investigator) => await ImportAsync(investigator);

    // --- Private helpers ---

    private async Task LoadRosterAsync()
    {
        Roster = await _store.GetRosterAsync() ?? new Roster();
    }

    private async Task WriteCharacterAsync(Investigator c)
    {
        var json = JsonSerializer.Serialize(c, JsonOptions);
        await _store.SaveCharacterJsonAsync(c.Id, json);
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
