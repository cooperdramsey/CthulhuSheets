namespace CthulhuSheets.Components;

public partial class InvestigatorSheet
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private static readonly string[] Tabs = ["Stats", "Skills", "Assets", "Notes"];

    private string _activeTab = Tabs[0];
    private readonly string _portraitInputId = $"portrait-upload-{Guid.NewGuid():N}";

    private bool IsHealthy =>
        !Investigator.TemporaryInsanity &&
        !Investigator.IndefiniteInsanity &&
        !Investigator.MajorWound &&
        !Investigator.Unconscious &&
        !Investigator.Dying;

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Investigator.PortraitDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }
}
