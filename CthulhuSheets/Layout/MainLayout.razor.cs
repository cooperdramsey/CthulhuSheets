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
        InvestigatorService.OnChanged += StateHasChanged;
        InvestigatorService.OnStorageError += ShowStorageError;
        await InvestigatorService.InitializeAsync();
        await InvestigatorService.RestoreActiveAsync();
    }

    public void Dispose()
    {
        InvestigatorService.OnChanged -= StateHasChanged;
        InvestigatorService.OnStorageError -= ShowStorageError;
    }

    private void ShowStorageError(string message)
    {
        Snackbar.Add(message, Severity.Error);
        StateHasChanged();
    }

    private async Task HandleFileSelected(IBrowserFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: Shared.Portraits.MaxBytes);
            using var reader = new StreamReader(stream);
            var raw = await reader.ReadToEndAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var investigator = JsonSerializer.Deserialize<Investigator>(raw, options);

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

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(InvestigatorService.Current, options);
        if (!string.IsNullOrEmpty(InvestigatorService.Current.PortraitDataUrl))
        {
            var node = JsonNode.Parse(json)!.AsObject();
            node["portraitDataUrl"] = InvestigatorService.Current.PortraitDataUrl;
            json = node.ToJsonString(options);
        }
        var filename = $"{InvestigatorService.Current.Name?.Replace(' ', '-') ?? "investigator"}.json";

        await JS.InvokeVoidAsync("downloadFile", filename, json);
    }

    private void HandleOpenRoster() => Navigation.NavigateTo("roster");

    private void HandleCreateNewCharacter() => Navigation.NavigateTo("create");
}
