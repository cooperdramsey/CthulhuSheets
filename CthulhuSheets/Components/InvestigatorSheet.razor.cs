namespace CthulhuSheets.Components;

public partial class InvestigatorSheet
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private static readonly string[] Tabs = ["Stats", "Skills", "Assets", "Notes"];

    private string _activeTab = Tabs[0];
    private readonly string _portraitInputId = $"portrait-upload-{Guid.NewGuid():N}";

    private bool IsHealthy =>
        !Investigator.TemporaryInsanity &&
        !Investigator.IndefiniteInsanity &&
        !Investigator.MajorWound &&
        !Investigator.Unconscious &&
        !Investigator.Dying;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private IEnumerable<(string Label, Characteristic Stat)> CharacteristicList =>
    [
        ("Strength",     Investigator.Strength),
        ("Constitution", Investigator.Constitution),
        ("Size",         Investigator.Size),
        ("Dexterity",    Investigator.Dexterity),
        ("Appearance",   Investigator.Appearance),
        ("Intelligence", Investigator.Intelligence),
        ("Power",        Investigator.Power),
        ("Education",    Investigator.Education),
    ];

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Investigator.PortraitDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        await InvestigatorService.PersistAsync();
    }

    private void RollD100() => DiceRollService.RollMany([(sides: 100, count: 1)]);
}
