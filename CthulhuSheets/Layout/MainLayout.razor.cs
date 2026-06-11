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
            await using var stream = file.OpenReadStream(maxAllowedSize: 1_048_576);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var investigator = await JsonSerializer.DeserializeAsync<Investigator>(stream, options);

            if (investigator is not null)
            {
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
        var filename = $"{InvestigatorService.Current.Name?.Replace(' ', '-') ?? "investigator"}.json";

        await JS.InvokeVoidAsync("downloadFile", filename, json);
    }

    private void HandleOpenRoster() => Navigation.NavigateTo("roster");

    private void HandleCreateNewCharacter() => Navigation.NavigateTo("create");
}
