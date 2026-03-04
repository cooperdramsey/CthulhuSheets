using System.Text.Json.Serialization;

namespace CthulhuSheets.Models;

public class Skill
{
    private int? _regular;

    public string Name { get; set; } = string.Empty;
    public int BaseValue { get; set; }

    public int? Regular
    {
        get => _regular;
        set
        {
            _regular = value;
            Half = value.HasValue ? value.Value / 2 : null;
            Fifth = value.HasValue ? value.Value / 5 : null;
        }
    }

    [JsonIgnore]
    public int? Half { get; private set; }

    [JsonIgnore]
    public int? Fifth { get; private set; }
}
