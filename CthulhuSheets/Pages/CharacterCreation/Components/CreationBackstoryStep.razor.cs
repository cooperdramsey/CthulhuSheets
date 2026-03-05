namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationBackstoryStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
