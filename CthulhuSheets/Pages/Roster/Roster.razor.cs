using CthulhuSheets.Shared;

namespace CthulhuSheets.Pages.Roster;

public partial class Roster : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private IEnumerable<RosterEntry> _entries = [];
    private readonly Dictionary<Guid, string?> _portraits = [];

    protected override async Task OnInitializedAsync()
    {
        InvestigatorService.OnRosterChanged += HandleRosterChanged;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _entries = InvestigatorService.GetEntriesOrdered().ToList();
        _portraits.Clear();
        foreach (var entry in _entries)
        {
            var character = await InvestigatorService.GetCharacterAsync(entry.Id);
            _portraits[entry.Id] = character?.PortraitDataUrl;
        }
    }

    private void HandleRosterChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await RefreshAsync();
            StateHasChanged();
        });
    }

    private async Task OpenCharacterAsync(Guid id)
    {
        await InvestigatorService.SelectAsync(id);
        Navigation.NavigateTo("");
    }

    private async Task DeleteCharacterAsync(Guid id, string? name)
    {
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Delete investigator?", new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "Delete investigator?" },
            { x => x.Message, $"This permanently removes {name ?? "this investigator"} from this browser. Export a copy first if you want to keep them." },
            { x => x.ConfirmText, "Delete" }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;

        await InvestigatorService.DeleteAsync(id);
        Snackbar.Add($"{name ?? "Investigator"} deleted.", Severity.Info);
    }

    private void NavigateToCreate() => Navigation.NavigateTo("create");

    private async Task LoadSampleAsync()
    {
        var json = await Http.GetStringAsync("samples/Dr.-Eleanor-Whitmore.json");
        var investigator = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
        if (investigator is not null)
        {
            await InvestigatorService.AddAsync(investigator);
            Navigation.NavigateTo("");
        }
    }

    private static string FormatTimestamp(DateTimeOffset ts)
    {
        var diff = DateTimeOffset.UtcNow - ts;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return ts.LocalDateTime.ToString("MMM d, yyyy");
    }

    public void Dispose() => InvestigatorService.OnRosterChanged -= HandleRosterChanged;
}
