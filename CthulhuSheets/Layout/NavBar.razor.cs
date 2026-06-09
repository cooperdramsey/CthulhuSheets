namespace CthulhuSheets.Layout;

public partial class NavBar
{
    [Parameter] public EventCallback OnCreateNewCharacter { get; set; }
    [Parameter] public EventCallback<IBrowserFile> OnFileSelected { get; set; }
    [Parameter] public EventCallback OnFileDownload { get; set; }
    [Parameter] public bool HasCharacter { get; set; }
    [Parameter] public EventCallback OnClearCharacter { get; set; }

    private async Task HandleFileSelected(IBrowserFile file) =>
        await OnFileSelected.InvokeAsync(file);
}
