using System.Text.Json;

namespace CthulhuSheets.Helpers;

// Single source of truth for the app's JSON contract. Every persisted, imported,
// and exported investigator/roster goes through these options, so the on-disk /
// in-storage casing policy can't drift between code paths (a drift silently
// breaks save round-tripping). JsonSerializerOptions is thread-safe once
// configured and is meant to be cached and reused — hence static readonly.
public static class CthulhuJson
{
    // Canonical read/write config: camelCase property names, case-insensitive on
    // read. Used for all at-rest storage and for import.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Same contract, but indented for human-readable downloaded files.
    public static readonly JsonSerializerOptions Export = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
