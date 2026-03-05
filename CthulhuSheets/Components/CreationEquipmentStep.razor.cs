namespace CthulhuSheets.Components;

public partial class CreationEquipmentStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
