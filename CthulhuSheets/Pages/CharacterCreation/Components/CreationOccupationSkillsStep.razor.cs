namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationOccupationSkillsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
