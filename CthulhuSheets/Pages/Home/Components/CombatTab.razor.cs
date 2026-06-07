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
        Investigator.Skills
            .FirstOrDefault(s => s.Name.Equals("Dodge", StringComparison.OrdinalIgnoreCase))
            ?.EffectiveRegular
        ?? Investigator.Dexterity.Half;

    private int? DodgeRegular => DodgeValue;
    private int? DodgeHard => DodgeValue / 2;
    private int? DodgeExtreme => DodgeValue / 5;

    private int? _dodgeRoll;
    private readonly Dictionary<Weapon, int> _weaponRolls = new();

    private void RollDodge(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _dodgeRoll = result.Total;
    }

    private void RollWeapon(Weapon weapon, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _weaponRolls[weapon] = result.Total;
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
