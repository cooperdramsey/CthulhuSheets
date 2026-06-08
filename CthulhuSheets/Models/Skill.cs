using System.Text.Json.Serialization;

namespace CthulhuSheets.Models;

public class Skill
{
    public string Name { get; set; } = string.Empty;
    public int BaseValue { get; set; }
    public int? Regular { get; set; }

    [JsonIgnore]
    public int EffectiveRegular => Regular ?? BaseValue;

    [JsonIgnore]
    public int? Half => EffectiveRegular / 2;

    [JsonIgnore]
    public int? Fifth => EffectiveRegular / 5;

    public bool HasExperienceCheck { get; set; }
    public bool IsDefault { get; set; }
}
