using Magic.IndexedDb;
using Magic.IndexedDb.Interfaces;

namespace CthulhuSheets.Services.Storage;

// --- DB context ---

public class CthulhuDbContext : IMagicRepository
{
    public static readonly IndexedDbSet Cthulhu = new("cthulhu");
}

// --- Record models ---

public class CharacterRecord : MagicTableTool<CharacterRecord>, IMagicTable<CharacterRecord.Dbs>
{
    public sealed class Dbs { public readonly IndexedDbSet Cthulhu = CthulhuDbContext.Cthulhu; }
    public Dbs Databases { get; } = new();

    public string Id { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;

    public IMagicCompoundKey GetKeys() => CreatePrimaryKey(x => x.Id, false);
    public string GetTableName() => "characters";
    public IndexedDbSet GetDefaultDatabase() => CthulhuDbContext.Cthulhu;
    public List<IMagicCompoundIndex>? GetCompoundIndexes() => null;
}

public class MetaRecord : MagicTableTool<MetaRecord>, IMagicTable<MetaRecord.Dbs>
{
    public sealed class Dbs { public readonly IndexedDbSet Cthulhu = CthulhuDbContext.Cthulhu; }
    public Dbs Databases { get; } = new();

    public string Key { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;

    public IMagicCompoundKey GetKeys() => CreatePrimaryKey(x => x.Key, false);
    public string GetTableName() => "meta";
    public IndexedDbSet GetDefaultDatabase() => CthulhuDbContext.Cthulhu;
    public List<IMagicCompoundIndex>? GetCompoundIndexes() => null;
}

// --- Store implementation ---

public class IndexedDbCharacterStore(IMagicIndexedDb db, IJSRuntime js) : ICharacterStore
{
    private const string RosterMetaKey = "roster";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<bool> TryInitializeAsync()
    {
        try
        {
            var q = await db.Query<MetaRecord>();
            await q.FirstOrDefaultAsync(x => x.Key == "__probe__");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Roster?> GetRosterAsync()
    {
        try
        {
            var q = await db.Query<MetaRecord>();
            var record = await q.FirstOrDefaultAsync(x => x.Key == RosterMetaKey);
            if (record is null) return null;
            return JsonSerializer.Deserialize<Roster>(record.Json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveRosterAsync(Roster roster)
    {
        var json = JsonSerializer.Serialize(roster, JsonOptions);
        var q = await db.Query<MetaRecord>();
        var existing = await q.FirstOrDefaultAsync(x => x.Key == RosterMetaKey);
        if (existing is not null)
        {
            existing.Json = json;
            await q.UpdateAsync(existing);
        }
        else
        {
            await q.AddAsync(new MetaRecord { Key = RosterMetaKey, Json = json });
        }
    }

    public async Task<string?> GetCharacterJsonAsync(Guid id)
    {
        try
        {
            var q = await db.Query<CharacterRecord>();
            var record = await q.FirstOrDefaultAsync(x => x.Id == id.ToString("N"));
            return record?.Json;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveCharacterJsonAsync(Guid id, string json)
    {
        var key = id.ToString("N");
        var q = await db.Query<CharacterRecord>();
        var existing = await q.FirstOrDefaultAsync(x => x.Id == key);
        if (existing is not null)
        {
            existing.Json = json;
            await q.UpdateAsync(existing);
        }
        else
        {
            await q.AddAsync(new CharacterRecord { Id = key, Json = json });
        }
    }

    public async Task DeleteCharacterAsync(Guid id)
    {
        var key = id.ToString("N");
        var q = await db.Query<CharacterRecord>();
        var existing = await q.FirstOrDefaultAsync(x => x.Id == key);
        if (existing is not null)
            await q.DeleteAsync(existing);
    }

    public async Task RequestPersistAsync()
    {
        try { await js.InvokeAsync<bool>("requestPersistentStorage"); }
        catch { /* silent */ }
    }
}
