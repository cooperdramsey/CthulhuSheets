namespace CthulhuSheets.Pages.Home.Components;

public partial class StatsTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    // Editing a characteristic auto-recomputes the derived stats (HP/MP max, MOV,
    // Build, Damage Bonus, starting SAN), preserving current pool values.
    private async Task OnCharacteristicChanged()
    {
        CharacteristicHelper.RecomputeDerived(Investigator);
        await PersistAsync();
    }

    private string? _editingStatName;

    private void ToggleEditStat(string statName)
    {
        _editingStatName = _editingStatName == statName ? null : statName;
    }

    private readonly Dictionary<string, int> _lastRolls = new();

    private IEnumerable<(string Label, Characteristic Stat)> CharacteristicList =>
    [
        ("Strength",     Investigator.Strength),
        ("Constitution", Investigator.Constitution),
        ("Size",         Investigator.Size),
        ("Dexterity",    Investigator.Dexterity),
        ("Appearance",   Investigator.Appearance),
        ("Intelligence", Investigator.Intelligence),
        ("Power",        Investigator.Power),
        ("Education",    Investigator.Education),
    ];

    private void RollStat(Characteristic stat, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _lastRolls[stat.Name] = result.Total;
    }

    private int? LastRoll(string statName) => _lastRolls.TryGetValue(statName, out var r) ? r : null;
}
