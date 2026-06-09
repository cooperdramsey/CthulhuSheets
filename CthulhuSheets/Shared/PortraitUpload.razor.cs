using MudBlazor;

namespace CthulhuSheets.Shared;

public partial class PortraitUpload
{
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Parameter]
    public string? PortraitDataUrl { get; set; }

    [Parameter]
    public EventCallback<string?> PortraitDataUrlChanged { get; set; }

    [Parameter]
    public int PortraitSize { get; set; } = 180;

    [Parameter]
    public string PlaceholderIcon { get; set; } = Icons.Material.Filled.AddAPhoto;

    [Parameter]
    public string? PlaceholderText { get; set; } = "Upload portrait";

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private async Task OpenDialogAsync()
    {
        var parameters = new DialogParameters<PortraitDialog>
        {
            { x => x.InitialValue, PortraitDataUrl }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

        var dialog = await DialogService.ShowAsync<PortraitDialog>("Set Portrait", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        var data = result.Data as PortraitDialogResult;
        var value = data?.Cleared == true ? null : data?.Value;

        PortraitDataUrl = value;
        await PortraitDataUrlChanged.InvokeAsync(value);
        await OnChanged.InvokeAsync();
    }
}
