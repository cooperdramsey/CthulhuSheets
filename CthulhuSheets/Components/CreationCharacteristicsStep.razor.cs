namespace CthulhuSheets.Components;

public partial class CreationCharacteristicsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
