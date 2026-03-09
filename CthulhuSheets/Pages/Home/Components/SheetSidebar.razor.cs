namespace CthulhuSheets.Pages.Home.Components;

public partial class SheetSidebar
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Parameter]
    public bool StatsEditMode { get; set; }

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private int? _luckRoll;

    private void RollLuck()
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _luckRoll = result.Total;
    }

    private bool? LuckSuccess =>
        _luckRoll.HasValue && Investigator.Luck.Current.HasValue
            ? _luckRoll.Value <= Investigator.Luck.Current.Value
            : null;
}
