using CthulhuSheets.Models;

namespace CthulhuSheets.Services;

public class InvestigatorService(IJSRuntime js)
{
    private const string SessionKey = "cthulhu-investigator";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Investigator? Current { get; private set; }

    public event Action? OnChanged;

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
        await js.InvokeVoidAsync("sessionStorage.setItem", SessionKey, json);
    }

    public async Task<bool> TryRestoreAsync()
    {
        var json = await js.InvokeAsync<string?>("sessionStorage.getItem", SessionKey);
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
            return false;
        }
    }

    public async Task ClearAsync()
    {
        Current = null;
        await js.InvokeVoidAsync("sessionStorage.removeItem", SessionKey);
        OnChanged?.Invoke();
    }
}
