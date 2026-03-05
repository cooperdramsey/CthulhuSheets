namespace CthulhuSheets.Components;

public partial class ItemsTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private async Task AddGearItem()
    {
        Investigator.GearAndPossessions.Add(string.Empty);
        await PersistAsync();
    }

    private async Task RemoveGearItem(int index)
    {
        Investigator.GearAndPossessions.RemoveAt(index);
        await PersistAsync();
    }
}
