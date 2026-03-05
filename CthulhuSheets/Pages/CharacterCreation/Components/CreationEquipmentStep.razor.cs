namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationEquipmentStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
