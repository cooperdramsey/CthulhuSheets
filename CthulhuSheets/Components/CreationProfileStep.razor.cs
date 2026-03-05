namespace CthulhuSheets.Components;

public partial class CreationProfileStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
