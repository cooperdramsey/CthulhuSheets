using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace CthulhuSheets.Shared;

public partial class PortraitDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string? InitialValue { get; set; }

    private string? _candidate;
    private string? _urlText;
    private string? _urlError;
    private string? _fileName;
    private string? _fileError;
    private Guid _fileInputKey = Guid.NewGuid();

    protected override void OnInitialized()
    {
        _candidate = InitialValue;
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        _fileError = null;
        try
        {
            using var stream = e.File.OpenReadStream(maxAllowedSize: Portraits.MaxBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var dataUrl = $"data:{e.File.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

            _candidate = dataUrl;
            _fileName = e.File.Name;

            // Mutual exclusivity: clear URL side
            _urlText = string.Empty;
            _urlError = null;
        }
        catch (IOException)
        {
            _fileError = $"File exceeds the {Portraits.MaxBytes / (1024 * 1024)} MB limit.";
            _candidate = null;
            _fileName = null;
        }
    }

    private void HandleUrlChanged(string value)
    {
        _urlText = value;
        var trimmed = value.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            _urlError = null;
            if (_fileName == null) // candidate came from URL, not a file
                _candidate = null;
            return;
        }

        // Mutual exclusivity: clear file side
        _fileName = null;
        _fileError = null;
        _fileInputKey = Guid.NewGuid(); // re-key so re-selecting same file works

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "data"))
        {
            _candidate = trimmed;
            _urlError = null;
        }
        else
        {
            _candidate = null;
            _urlError = "Enter a valid http(s) image URL.";
        }
    }

    private void HandleUrlKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrEmpty(_candidate))
            Save();
    }

    private void Save()
    {
        MudDialog.Close(DialogResult.Ok(new PortraitDialogResult { Value = _candidate }));
    }

    private void Remove()
    {
        MudDialog.Close(DialogResult.Ok(new PortraitDialogResult { Cleared = true }));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
