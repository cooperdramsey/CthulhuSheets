using System.Text.Json.Nodes;

namespace CthulhuSheets.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        InvestigatorService.OnChanged += HandleChanged;
        InvestigatorService.OnStorageError += ShowStorageError;
        await InvestigatorService.InitializeAsync();
        await InvestigatorService.RestoreActiveAsync();
    }

    public void Dispose()
    {
        InvestigatorService.OnChanged -= HandleChanged;
        InvestigatorService.OnStorageError -= ShowStorageError;
    }

    // OnChanged can be raised off the render thread; marshal back via InvokeAsync
    // (matching Roster/Home) before re-rendering.
    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);

    private void ShowStorageError(string message)
    {
        _ = InvokeAsync(() =>
        {
            Snackbar.Add(message, Severity.Error);
            StateHasChanged();
        });
    }

    private async Task HandleFileSelected(IBrowserFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: Shared.Portraits.MaxBytes);
            using var reader = new StreamReader(stream);
            var raw = await reader.ReadToEndAsync();
            var investigator = JsonSerializer.Deserialize<Investigator>(raw, CthulhuJson.Options);

            if (investigator is not null)
            {
                investigator.PortraitDataUrl = InvestigatorService.ExtractInlinePortrait(raw);
                await InvestigatorService.ImportAsync(investigator);
                Snackbar.Add($"Loaded {investigator.Name ?? "investigator"}", Severity.Success);
                Navigation.NavigateTo("");
            }
            else
            {
                Snackbar.Add("File did not contain a valid investigator.", Severity.Warning);
            }
        }
        catch (JsonException)
        {
            Snackbar.Add("Invalid JSON — could not parse character file.", Severity.Error);
        }
        catch (IOException)
        {
            Snackbar.Add($"File too large (max {Shared.Portraits.MaxBytes / (1024 * 1024)} MB).", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load character: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleFileDownload()
    {
        if (InvestigatorService.Current is null)
        {
            Snackbar.Add("No investigator loaded to download.", Severity.Warning);
            return;
        }

        var json = JsonSerializer.Serialize(InvestigatorService.Current, CthulhuJson.Export);
        if (!string.IsNullOrEmpty(InvestigatorService.Current.PortraitDataUrl))
        {
            var node = JsonNode.Parse(json)!.AsObject();
            node["portraitDataUrl"] = InvestigatorService.Current.PortraitDataUrl;
            json = node.ToJsonString(CthulhuJson.Export);
        }
        var filename = $"{InvestigatorService.Current.Name?.Replace(' ', '-') ?? "investigator"}.json";

        await JS.InvokeVoidAsync("downloadFile", filename, json);
    }

    private void HandleOpenRoster() => Navigation.NavigateTo("roster");

    private void HandleCreateNewCharacter() => Navigation.NavigateTo("create");
}
