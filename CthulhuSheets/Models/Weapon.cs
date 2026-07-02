namespace CthulhuSheets.Models;

public class Weapon
{
    public string? Name { get; set; }
    public string? Damage { get; set; }
    public int? NumberOfAttacks { get; set; }
    public string? Range { get; set; }
    public int? Ammo { get; set; }
    public int? Malfunction { get; set; }

    /// <summary>
    /// Name of the sheet skill this weapon rolls against (e.g. "Firearms (Handgun)").
    /// When set, the weapon's skill value is read live from that skill so it can
    /// never drift from the Skills tab; null means <see cref="SkillRegular"/> is
    /// used as a manually entered value.
    /// </summary>
    public string? SkillName { get; set; }

    /// <summary>Manually entered skill value, used when no skill is linked.</summary>
    public int? SkillRegular { get; set; }
}
