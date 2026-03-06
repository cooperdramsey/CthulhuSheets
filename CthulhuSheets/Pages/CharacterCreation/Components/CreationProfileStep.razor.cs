namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationProfileStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private EditContext _editContext = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Investigator);
    }

    public bool Validate() => _editContext.Validate();
}
