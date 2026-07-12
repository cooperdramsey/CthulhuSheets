using System.Text.Json.Nodes;
using CthulhuSheets.Services.Storage;

namespace CthulhuSheets.Services;

public class InvestigatorService(
    IndexedDbCharacterStore indexedDb,
    LocalStorageCharacterStore localStorage,
    StorageMigrator migrator)
{
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

            // One-time localStorage -> IndexedDB migration runs only when
            // IndexedDB is the active store.
            var outcome = await migrator.MigrateIfNeededAsync();
            if (outcome.Failed)
                OnStorageError?.Invoke(outcome.Message!);
        }
        else
        {
            _store = localStorage;
            await localStorage.TryInitializeAsync();
        }

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
            Current = JsonSerializer.Deserialize<Investigator>(json, CthulhuJson.Options);
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

    // Creating a new character always assigns a fresh id.
    public Task AddAsync(Investigator c) => SaveNewAsync(c, assignFreshId: true);

    // Importing keeps the file's id, assigning one only if it's empty.
    public Task ImportAsync(Investigator c) => SaveNewAsync(c, assignFreshId: false);

    private async Task SaveNewAsync(Investigator c, bool assignFreshId)
    {
        if (assignFreshId || c.Id == Guid.Empty)
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
            Current = JsonSerializer.Deserialize<Investigator>(json, CthulhuJson.Options);
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
        var json = JsonSerializer.Serialize(c, CthulhuJson.Options);
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
                await _store.SaveCharacterJsonAsync(id, JsonSerializer.Serialize(investigator, CthulhuJson.Options));
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
