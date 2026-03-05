namespace CthulhuSheets.Pages.Home.Components;

public partial class WealthTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private async Task AddAsset()
    {
        Investigator.Wealth.Assets.Add(string.Empty);
        await PersistAsync();
    }

    private async Task RemoveAsset(int index)
    {
        Investigator.Wealth.Assets.RemoveAt(index);
        await PersistAsync();
    }
}
