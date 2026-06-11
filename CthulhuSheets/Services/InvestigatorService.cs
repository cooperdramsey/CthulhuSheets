namespace CthulhuSheets.Services;

public class InvestigatorService(IJSRuntime js)
{
    private const string RosterKey = "cthulhu-roster";
    private const string LegacyKey = "cthulhu-investigator";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string CharacterKey(Guid id) => $"cthulhu-character-{id}";

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
        await LoadRosterAsync();
        await MigrateLegacyAsync();
        OnRosterChanged?.Invoke();
    }

    public async Task RestoreActiveAsync()
    {
        if (Roster.ActiveId is null) return;

        var id = Roster.ActiveId.Value;
        var json = await js.InvokeAsync<string?>("localStorage.getItem", CharacterKey(id));
        if (string.IsNullOrEmpty(json))
        {
            RemoveEntry(id);
            Roster.ActiveId = null;
            await PersistRosterAsync();
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
            await js.InvokeVoidAsync("localStorage.removeItem", CharacterKey(id));
            await PersistRosterAsync();
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
        await PersistRosterAsync();
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task ImportAsync(Investigator c)
    {
        if (c.Id == Guid.Empty)
        {
            c.Id = Guid.NewGuid();
        }
        else if (!Roster.Entries.Any(e => e.Id == c.Id))
        {
            // keep the id as-is
        }
        // if id already in roster, overwrite in place

        await WriteCharacterAsync(c);
        UpsertEntry(c);
        Roster.ActiveId = c.Id;
        Current = c;
        await PersistRosterAsync();
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task SelectAsync(Guid id)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", CharacterKey(id));
        if (string.IsNullOrEmpty(json))
        {
            RemoveEntry(id);
            if (Roster.ActiveId == id) Roster.ActiveId = null;
            await PersistRosterAsync();
            OnRosterChanged?.Invoke();
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
            Roster.ActiveId = id;
            await PersistRosterAsync();
            OnChanged?.Invoke();
        }
        catch (JsonException)
        {
            RemoveEntry(id);
            Roster.ActiveId = null;
            await js.InvokeVoidAsync("localStorage.removeItem", CharacterKey(id));
            await PersistRosterAsync();
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
            await PersistRosterAsync();
        }
        catch (JSException)
        {
            OnStorageError?.Invoke("Couldn't save your character — browser storage is full. Try a smaller portrait or export to a file.");
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", CharacterKey(id));
        RemoveEntry(id);
        if (Roster.ActiveId == id)
        {
            Roster.ActiveId = null;
            Current = null;
        }
        await PersistRosterAsync();
        OnChanged?.Invoke();
        OnRosterChanged?.Invoke();
    }

    public async Task<Investigator?> GetCharacterAsync(Guid id)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", CharacterKey(id));
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // --- Legacy load (used by MainLayout for file-upload flow that already has an Investigator) ---

    public async Task LoadAsync(Investigator investigator)
    {
        await ImportAsync(investigator);
    }

    // --- Private helpers ---

    private async Task LoadRosterAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", RosterKey);
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            Roster = JsonSerializer.Deserialize<Roster>(json, JsonOptions) ?? new Roster();
        }
        catch (JsonException)
        {
            Roster = new Roster();
        }
    }

    private async Task MigrateLegacyAsync()
    {
        if (Roster.Entries.Count > 0) return;

        var json = await js.InvokeAsync<string?>("localStorage.getItem", LegacyKey);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var investigator = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
            if (investigator is not null)
            {
                investigator.Id = Guid.NewGuid();
                await WriteCharacterAsync(investigator);
                UpsertEntry(investigator);
                Roster.ActiveId = investigator.Id;
                await PersistRosterAsync();
            }
        }
        catch (JsonException) { }

        await js.InvokeVoidAsync("localStorage.removeItem", LegacyKey);
    }

    private async Task WriteCharacterAsync(Investigator c)
    {
        var json = JsonSerializer.Serialize(c, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", CharacterKey(c.Id), json);
    }

    private async Task PersistRosterAsync()
    {
        var json = JsonSerializer.Serialize(Roster, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", RosterKey, json);
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
