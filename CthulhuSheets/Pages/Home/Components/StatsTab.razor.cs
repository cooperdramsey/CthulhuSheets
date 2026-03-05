namespace CthulhuSheets.Pages.Home.Components;

public partial class StatsTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Parameter]
    public bool EditMode { get; set; }

    [Parameter]
    public EventCallback OnToggleEditMode { get; set; }

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

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

    private void RollStat(Characteristic stat)
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _lastRolls[stat.Name] = result.Total;
    }

    private string? CheckIcon(string statName, int? threshold)
    {
        if (!threshold.HasValue || !_lastRolls.TryGetValue(statName, out var roll)) return null;
        return roll <= threshold.Value ? Icons.Material.Filled.Check : Icons.Material.Filled.Close;
    }

    private Color CheckColor(string statName, int? threshold)
    {
        if (!threshold.HasValue || !_lastRolls.TryGetValue(statName, out var roll)) return Color.Default;
        return roll <= threshold.Value ? Color.Success : Color.Error;
    }
}
