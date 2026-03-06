namespace CthulhuSheets.Pages.Home.Components;

public partial class SheetSidebar
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Parameter]
    public bool StatsEditMode { get; set; }

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();
}
