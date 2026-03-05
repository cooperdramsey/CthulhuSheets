namespace CthulhuSheets.Pages.Home.Components;

public partial class SheetSidebar
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Parameter]
    public bool StatsEditMode { get; set; }

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private readonly string _portraitInputId = $"portrait-upload-{Guid.NewGuid():N}";

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        await InvestigatorService.SetPortraitFromFileAsync(Investigator, e.File);
        await InvestigatorService.PersistAsync();
    }
}
