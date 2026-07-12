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

public class PortraitRecord : MagicTableTool<PortraitRecord>, IMagicTable<PortraitRecord.Dbs>
{
    public sealed class Dbs { public readonly IndexedDbSet Cthulhu = CthulhuDbContext.Cthulhu; }
    public Dbs Databases { get; } = new();

    public string Id { get; set; } = string.Empty;
    public string DataUrl { get; set; } = string.Empty;

    public IMagicCompoundKey GetKeys() => CreatePrimaryKey(x => x.Id, false);
    public string GetTableName() => "portraits";
    public IndexedDbSet GetDefaultDatabase() => CthulhuDbContext.Cthulhu;
    public List<IMagicCompoundIndex>? GetCompoundIndexes() => null;
}

// --- Store implementation ---

public class IndexedDbCharacterStore(IMagicIndexedDb db, IJSRuntime js) : ICharacterStore
{
    private const string RosterMetaKey = "roster";

    // Magic.IndexedDb (v2.0.2) is not safe for overlapping queries: two calls
    // racing on a cold PropertyMappingCache corrupt its type cache and throw
    // Arg_NoDefCTor while deserializing results. Blazor WASM is single-threaded
    // but async DB calls still interleave at await points, so serialize every
    // store access through this gate to guarantee one operation at a time.
    private readonly SemaphoreSlim _gate = new(1, 1);

    private async Task<T> LockedAsync<T>(Func<Task<T>> op)
    {
        await _gate.WaitAsync();
        try { return await op(); }
        finally { _gate.Release(); }
    }

    private async Task LockedAsync(Func<Task> op)
    {
        await _gate.WaitAsync();
        try { await op(); }
        finally { _gate.Release(); }
    }

    public Task<bool> TryInitializeAsync() => LockedAsync(async () =>
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
    });

    public Task<Roster?> GetRosterAsync() => LockedAsync<Roster?>(async () =>
    {
        try
        {
            var q = await db.Query<MetaRecord>();
            // Materialize with Where(...).ToListAsync(), not the streaming
            // FirstOrDefaultAsync cursor — see GetCharacterJsonAsync.
            var records = await q.Where(x => x.Key == RosterMetaKey).ToListAsync();
            var record = records.FirstOrDefault();
            if (record is null) return null;
            return JsonSerializer.Deserialize<Roster>(record.Json, CthulhuJson.Options);
        }
        catch
        {
            return null;
        }
    });

    public Task SaveRosterAsync(Roster roster) => LockedAsync(async () =>
    {
        var json = JsonSerializer.Serialize(roster, CthulhuJson.Options);
        var q = await db.Query<MetaRecord>();
        // Upsert via bulkPut (UpdateRangeAsync) — never a read-before-write. See
        // SaveCharacterJsonAsync for why the cursor read is avoided.
        await q.UpdateRangeAsync(new[] { new MetaRecord { Key = RosterMetaKey, Json = json } });
    });

    public Task<string?> GetCharacterJsonAsync(Guid id) => LockedAsync<string?>(async () =>
    {
        try
        {
            var key = id.ToString("N");
            var q = await db.Query<CharacterRecord>();
            // Materialize with Where(...).ToListAsync() (bulk fetch) instead of the
            // streaming FirstOrDefaultAsync cursor. A cursor read holds the IndexedDB
            // transaction open across the JS<->.NET interop boundary; on a large
            // record Dexie aborts it with PrematureCommitError (surfacing as
            // "Error handling streamed JS"), which this catch would swallow to null
            // and silently drop the character. ToListAsync returns the whole match
            // set in one shot with no open cursor. See SaveCharacterJsonAsync.
            var records = await q.Where(x => x.Id == key).ToListAsync();
            return records.FirstOrDefault()?.Json;
        }
        catch
        {
            return null;
        }
    });

    public Task SaveCharacterJsonAsync(Guid id, string json) => LockedAsync(async () =>
    {
        var key = id.ToString("N");
        var q = await db.Query<CharacterRecord>();
        // Upsert via bulkPut (UpdateRangeAsync) so we never issue a streaming
        // read-before-write. The previous cursor-based FirstOrDefaultAsync held an
        // IndexedDB transaction open across the JS<->.NET interop boundary, which
        // Dexie aborts with PrematureCommitError (surfacing as Arg_NoDefCTor while
        // deserializing the streamed result). bulkPut is a single insert-or-replace
        // with no read — storage robustness only, not a data-model change.
        await q.UpdateRangeAsync(new[] { new CharacterRecord { Id = key, Json = json } });
    });

    public Task DeleteCharacterAsync(Guid id) => LockedAsync(async () =>
    {
        var key = id.ToString("N");
        var q = await db.Query<CharacterRecord>();
        // Delete by key with no read-before-write (see SaveCharacterJsonAsync).
        // DeleteAsync formats the key from the record and calls table.delete(key),
        // which is a harmless no-op if the key is absent — so no existence check.
        await q.DeleteAsync(new CharacterRecord { Id = key });
    });

    public Task<string?> GetPortraitAsync(Guid id) => LockedAsync<string?>(async () =>
    {
        try
        {
            var key = id.ToString("N");
            var q = await db.Query<PortraitRecord>();
            // Materialize with Where(...).ToListAsync(), not the streaming
            // FirstOrDefaultAsync cursor — see GetCharacterJsonAsync. Portrait
            // records are large (base64 data URLs), so the cursor-commit race is
            // guaranteed here.
            var records = await q.Where(x => x.Id == key).ToListAsync();
            return records.FirstOrDefault()?.DataUrl;
        }
        catch
        {
            return null;
        }
    });

    public Task SavePortraitAsync(Guid id, string? dataUrl) => LockedAsync(async () =>
    {
        var key = id.ToString("N");
        var q = await db.Query<PortraitRecord>();
        if (string.IsNullOrEmpty(dataUrl))
        {
            // Delete by key with no read-before-write (see SaveCharacterJsonAsync).
            await q.DeleteAsync(new PortraitRecord { Id = key });
            return;
        }

        // Upsert via bulkPut (UpdateRangeAsync) — never a read-before-write (see
        // SaveCharacterJsonAsync).
        await q.UpdateRangeAsync(new[] { new PortraitRecord { Id = key, DataUrl = dataUrl } });
    });

    public Task DeletePortraitAsync(Guid id) => LockedAsync(async () =>
    {
        var key = id.ToString("N");
        var q = await db.Query<PortraitRecord>();
        // Delete by key with no read-before-write (see SaveCharacterJsonAsync).
        await q.DeleteAsync(new PortraitRecord { Id = key });
    });

    public async Task RequestPersistAsync()
    {
        try { await js.InvokeAsync<bool>("requestPersistentStorage"); }
        catch { /* silent */ }
    }
}
