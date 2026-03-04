namespace CthulhuSheets.Components;

public partial class InvestigatorSheet
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private static readonly string[] Tabs = ["Stats", "Skills", "Assets", "Notes"];

    private string _activeTab = Tabs[0];

    private bool IsHealthy =>
        !Investigator.TemporaryInsanity &&
        !Investigator.IndefiniteInsanity &&
        !Investigator.MajorWound &&
        !Investigator.Unconscious &&
        !Investigator.Dying;
}
