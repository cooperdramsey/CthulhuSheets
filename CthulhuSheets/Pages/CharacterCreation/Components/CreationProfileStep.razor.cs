namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationProfileStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private EditContext _editContext = default!;
    private readonly string _portraitInputId = $"portrait-{Guid.NewGuid():N}";

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Investigator);
    }

    public bool Validate() => _editContext.Validate();

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        await InvestigatorService.SetPortraitFromFileAsync(Investigator, e.File);
    }

    private void RemovePortrait() => Investigator.PortraitDataUrl = null;
}
