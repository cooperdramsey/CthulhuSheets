using CthulhuSheets.Data;

namespace CthulhuSheets.Pages.Home.Components;

public partial class CombatTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    // Dodge is a normal skill: use its (possibly improved) value, falling back to
    // its ½DEX base if the skill isn't on the sheet.
    private int? DodgeValue =>
        Investigator.FindSkill(WellKnownSkills.Dodge)?.EffectiveRegular
        ?? Investigator.Dexterity.Half;

    private int? DodgeRegular => DodgeValue;
    private int? DodgeHard => DodgeValue / 2;
    private int? DodgeExtreme => DodgeValue / 5;

    private int? _dodgeRoll;
    private readonly Dictionary<Weapon, int> _weaponRolls = new();
    private bool _weaponsEditMode;

    private void ToggleWeaponsEditMode() => _weaponsEditMode = !_weaponsEditMode;

    private async Task RollDodge(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _dodgeRoll = result.Total;

        // Ticks the Dodge experience check via the shared predicate so it stays
        // identical to every other roll path (ch_5 development phase).
        var dodgeSkill = Investigator.FindSkill(WellKnownSkills.Dodge);
        if (dodgeSkill is not null && SkillRules.ShouldMarkExperienceCheck(dodgeSkill, result.Total, modifier))
        {
            dodgeSkill.HasExperienceCheck = true;
            await PersistAsync();
        }
    }

    private async Task RollWeapon(Weapon weapon, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _weaponRolls[weapon] = result.Total;

        // A successful roll on a linked weapon ticks that skill's experience
        // check, matching the Skills tab (ch_5 development phase); manual/
        // unlinked weapons have no skill object and never tick.
        var linkedSkill = LinkedSkill(weapon);
        if (linkedSkill is not null && SkillRules.ShouldMarkExperienceCheck(linkedSkill, result.Total, modifier))
        {
            linkedSkill.HasExperienceCheck = true;
            await PersistAsync();
        }
    }

    // ── Weapon ↔ skill linkage ───────────────────────
    // A linked weapon reads its skill value live from the sheet skill so the two
    // can never drift; an unlinked weapon uses its manually entered value.

    private Skill? LinkedSkill(Weapon weapon) =>
        weapon.SkillName is null
            ? null
            : Investigator.Skills.FirstOrDefault(
                s => s.Name.Equals(weapon.SkillName, StringComparison.OrdinalIgnoreCase));

    private int? WeaponSkillValue(Weapon weapon) =>
        LinkedSkill(weapon)?.EffectiveRegular ?? weapon.SkillRegular;

    private int? WeaponSkillHalf(Weapon weapon) => WeaponSkillValue(weapon) / 2;
    private int? WeaponSkillFifth(Weapon weapon) => WeaponSkillValue(weapon) / 5;

    private async Task LinkWeaponSkill(Weapon weapon, string? skillName)
    {
        weapon.SkillName = skillName;
        await PersistAsync();
    }

    private async Task AddWeapon()
    {
        Investigator.Weapons.Add(new Weapon());
        await PersistAsync();
    }

    private async Task RemoveWeapon(Weapon weapon)
    {
        _weaponRolls.Remove(weapon);
        Investigator.Weapons.Remove(weapon);
        await PersistAsync();
    }
}
