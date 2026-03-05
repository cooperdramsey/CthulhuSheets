namespace CthulhuSheets.Components;

public partial class SheetSidebar
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Parameter]
    public bool StatsEditMode { get; set; }

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private readonly string _portraitInputId = $"portrait-upload-{Guid.NewGuid():N}";

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Investigator.PortraitDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        await InvestigatorService.PersistAsync();
    }
}
