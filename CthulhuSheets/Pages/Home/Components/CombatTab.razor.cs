namespace CthulhuSheets.Pages.Home.Components;

public partial class CombatTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private int? DodgeRegular => Investigator.Dexterity.Half;
    private int? DodgeHard => Investigator.Dexterity.Half / 2;
    private int? DodgeExtreme => Investigator.Dexterity.Half / 5;

    private int? _dodgeRoll;

    private void RollDodge()
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _dodgeRoll = result.Total;
    }

    private async Task AddWeapon()
    {
        Investigator.Weapons.Add(new Weapon());
        await PersistAsync();
    }

    private async Task RemoveWeapon(Weapon weapon)
    {
        Investigator.Weapons.Remove(weapon);
        await PersistAsync();
    }
}
