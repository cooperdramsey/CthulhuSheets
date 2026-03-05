namespace CthulhuSheets.Components;

public partial class CreationWealthStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;
}
