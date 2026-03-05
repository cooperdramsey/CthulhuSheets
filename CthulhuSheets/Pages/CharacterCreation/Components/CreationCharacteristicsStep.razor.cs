namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationCharacteristicsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
