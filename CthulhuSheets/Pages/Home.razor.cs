namespace CthulhuSheets.Pages;

public partial class Home : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override void OnInitialized()
    {
        InvestigatorService.OnChanged += StateHasChanged;
    }

    private void NavigateToCreate() => Navigation.NavigateTo("/create");

    private async Task LoadSampleAsync()
    {
        var json = await Http.GetStringAsync("samples/Dr.-Eleanor-Whitmore.json");
        var investigator = JsonSerializer.Deserialize<Investigator>(json, JsonOptions);
        if (investigator is not null)
            await InvestigatorService.LoadAsync(investigator);
    }

    public void Dispose() => InvestigatorService.OnChanged -= StateHasChanged;
}
