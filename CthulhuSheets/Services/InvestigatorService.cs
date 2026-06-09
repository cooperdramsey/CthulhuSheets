namespace CthulhuSheets.Services;

public class InvestigatorService(IJSRuntime js)
{
    private const string StorageKey = "cthulhu-investigator";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Investigator? Current { get; private set; }

    public event Action? OnChanged;
    public event Action<string>? OnStorageError;

    public async Task LoadAsync(Investigator investigator)
    {
        Current = investigator;
        await PersistAsync();
        OnChanged?.Invoke();
    }

    public async Task PersistAsync()
    {
        if (Current is null) return;
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (JSException)
        {
            OnStorageError?.Invoke("Couldn't save your character — browser storage is full. Try a smaller portrait or export to a file.");
        }
    }

    public async Task<bool> TryRestoreAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            var investigator = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
            if (investigator is null) return false;
            Current = investigator;
            OnChanged?.Invoke();
            return true;
        }
        catch (JsonException)
        {
            await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            OnStorageError?.Invoke("Your saved character couldn't be read and was cleared.");
            return false;
        }
    }

    public async Task ClearAsync()
    {
        Current = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        OnChanged?.Invoke();
    }
}
