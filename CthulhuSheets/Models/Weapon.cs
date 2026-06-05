using System.Text.Json.Serialization;

namespace CthulhuSheets.Models;

public class Weapon
{
    private int? _skillRegular;

    public string? Name { get; set; }
    public string? Damage { get; set; }
    public int? NumberOfAttacks { get; set; }
    public string? Range { get; set; }
    public int? Ammo { get; set; }
    public int? Malfunction { get; set; }

    public int? SkillRegular
    {
        get => _skillRegular;
        set
        {
            _skillRegular = value;
            SkillHalf = value.HasValue ? value.Value / 2 : null;
            SkillFifth = value.HasValue ? value.Value / 5 : null;
        }
    }

    [JsonIgnore]
    public int? SkillHalf { get; private set; }

    [JsonIgnore]
    public int? SkillFifth { get; private set; }
}
