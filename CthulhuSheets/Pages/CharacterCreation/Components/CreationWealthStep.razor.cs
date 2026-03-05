namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationWealthStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
