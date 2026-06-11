namespace CthulhuSheets.Services.Storage;

public interface ICharacterStore
{
    Task<bool> TryInitializeAsync();
    Task<Roster?> GetRosterAsync();
    Task SaveRosterAsync(Roster roster);
    Task<string?> GetCharacterJsonAsync(Guid id);
    Task SaveCharacterJsonAsync(Guid id, string json);
    Task DeleteCharacterAsync(Guid id);
    Task RequestPersistAsync();
}
