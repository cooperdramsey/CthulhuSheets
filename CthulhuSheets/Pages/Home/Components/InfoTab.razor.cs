namespace CthulhuSheets.Pages.Home.Components;

public partial class InfoTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private async Task AddFellowInvestigator()
    {
        Investigator.FellowInvestigators.Add(new FellowInvestigator());
        await PersistAsync();
    }

    private async Task RemoveFellowInvestigator(FellowInvestigator fellow)
    {
        Investigator.FellowInvestigators.Remove(fellow);
        await PersistAsync();
    }
}
