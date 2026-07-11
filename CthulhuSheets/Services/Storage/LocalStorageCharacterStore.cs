namespace CthulhuSheets.Services.Storage;

public class LocalStorageCharacterStore(IJSRuntime js) : ICharacterStore
{
    private const string RosterKey = "cthulhu-roster";
    private const string LegacyKey = "cthulhu-investigator";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static string CharacterKey(Guid id) => $"cthulhu-character-{id}";
    internal static string PortraitKey(Guid id) => $"cthulhu-portrait-{id}";
    internal const string LegacyStorageKey = LegacyKey;

    public async Task<bool> TryInitializeAsync()
    {
        try
        {
            await js.InvokeAsync<string?>("localStorage.getItem", "cthulhu-probe");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Roster?> GetRosterAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", RosterKey);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Roster>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveRosterAsync(Roster roster)
    {
        var json = JsonSerializer.Serialize(roster, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", RosterKey, json);
    }

    public async Task<string?> GetCharacterJsonAsync(Guid id)
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", CharacterKey(id));
    }

    public async Task SaveCharacterJsonAsync(Guid id, string json)
    {
        await js.InvokeVoidAsync("localStorage.setItem", CharacterKey(id), json);
    }

    public async Task DeleteCharacterAsync(Guid id)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", CharacterKey(id));
    }

    public async Task<string?> GetPortraitAsync(Guid id)
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", PortraitKey(id));
    }

    public async Task SavePortraitAsync(Guid id, string? dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl))
        {
            await js.InvokeVoidAsync("localStorage.removeItem", PortraitKey(id));
            return;
        }

        await js.InvokeVoidAsync("localStorage.setItem", PortraitKey(id), dataUrl);
    }

    public async Task DeletePortraitAsync(Guid id)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", PortraitKey(id));
    }

    public async Task<string?> GetLegacyCharacterJsonAsync()
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", LegacyKey);
    }

    public async Task RemoveAllCthulhuKeysAsync()
    {
        await js.InvokeVoidAsync("cthulhuLocalStorage.removeKeys", "cthulhu-");
    }

    public Task RequestPersistAsync() => Task.CompletedTask;
}
