namespace CthulhuSheets.Pages.Home.Components;

public partial class InvestigatorSheet
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private static readonly string[] Tabs = ["Stats", "Skills", "Combat", "Items", "Wealth", "Info"];

    private string _activeTab = Tabs[0];
    private bool _statsEditMode;
}
