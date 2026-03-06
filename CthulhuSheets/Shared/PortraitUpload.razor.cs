namespace CthulhuSheets.Shared;

public partial class PortraitUpload
{
    [Parameter]
    public string? PortraitDataUrl { get; set; }

    [Parameter]
    public EventCallback<string?> PortraitDataUrlChanged { get; set; }

    [Parameter]
    public int PortraitSize { get; set; } = 180;

    [Parameter]
    public bool ShowRemoveButton { get; set; } = true;

    [Parameter]
    public string PlaceholderIcon { get; set; } = Icons.Material.Filled.AddAPhoto;

    [Parameter]
    public string? PlaceholderText { get; set; } = "Upload portrait";

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private readonly string _inputId = $"portrait-{Guid.NewGuid():N}";

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        using var stream = e.File.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var dataUrl = $"data:{e.File.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

        PortraitDataUrl = dataUrl;
        await PortraitDataUrlChanged.InvokeAsync(dataUrl);
        await OnChanged.InvokeAsync();
    }

    private async Task RemovePortrait()
    {
        PortraitDataUrl = null;
        await PortraitDataUrlChanged.InvokeAsync(null);
        await OnChanged.InvokeAsync();
    }
}
