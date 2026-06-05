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
    private int? _sanityRoll;

    private void RollLuck(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _luckRoll = result.Total;
    }

    private void RollSanity(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _sanityRoll = result.Total;
    }

    private bool? LuckSuccess =>
        _luckRoll.HasValue && Investigator.Luck.Current.HasValue
            ? _luckRoll.Value <= Investigator.Luck.Current.Value
            : null;

    private bool? SanitySuccess =>
        _sanityRoll.HasValue && Investigator.Sanity.Current.HasValue
            ? _sanityRoll.Value <= Investigator.Sanity.Current.Value
            : null;
}
