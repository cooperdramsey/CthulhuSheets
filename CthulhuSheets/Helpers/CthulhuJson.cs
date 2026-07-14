using System.Text.Json;
using System.Text.Json.Serialization;

namespace CthulhuSheets.Helpers;

// Single source of truth for the app's JSON contract. Every persisted, imported,
// and exported investigator/roster goes through these options, so the on-disk /
// in-storage casing policy can't drift between code paths (a drift silently
// breaks save round-tripping). JsonSerializerOptions is thread-safe once
// configured and is meant to be cached and reused — hence static readonly.
// Enums are written as their string names (JsonStringEnumConverter); the
// converter still reads raw numbers too, so pre-existing numeric saves aren't broken.
public static class CthulhuJson
{
    // Canonical read/write config: camelCase property names, case-insensitive on
    // read. Used for all at-rest storage and for import.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    // Same contract, but indented for human-readable downloaded files.
    public static readonly JsonSerializerOptions Export = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
