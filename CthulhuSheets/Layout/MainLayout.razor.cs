using CthulhuSheets.Shared;

namespace CthulhuSheets.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        InvestigatorService.OnChanged += StateHasChanged;
        InvestigatorService.OnStorageError += ShowStorageError;
        await InvestigatorService.TryRestoreAsync();
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

    private Task HandleCreateNewCharacter()
    {
        Navigation.NavigateTo("create");
        return Task.CompletedTask;
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
                if (InvestigatorService.Current is not null && !await ConfirmReplaceAsync())
                {
                    Snackbar.Add("Import cancelled.", Severity.Info);
                    return;
                }
                await InvestigatorService.LoadAsync(investigator);
                Snackbar.Add($"Loaded {investigator.Name ?? "investigator"}", Severity.Success);
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

    private async Task HandleClearCharacter()
    {
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Delete saved character?", new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "Delete saved character?" },
            { x => x.Message, "This permanently removes the saved character from this browser. Export a copy first if you want to keep it." },
            { x => x.ConfirmText, "Delete" }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;

        await InvestigatorService.ClearAsync();
        Snackbar.Add("Saved character cleared.", Severity.Info);
        Navigation.NavigateTo("");
    }

    private async Task<bool> ConfirmReplaceAsync()
    {
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Replace saved character?", new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "Replace saved character?" },
            { x => x.Message, "This will replace the currently saved character. Export a copy first if you want to keep it." },
            { x => x.ConfirmText, "Replace" }
        });
        var result = await dialog.Result;
        return result is not null && !result.Canceled;
    }
}
