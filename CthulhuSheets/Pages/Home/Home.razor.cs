namespace CthulhuSheets.Pages.Home;

public partial class Home : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        InvestigatorService.OnChanged += HandleChanged;
        if (InvestigatorService.Current is null)
            Navigation.NavigateTo("roster");
    }

    private void HandleChanged()
    {
        if (InvestigatorService.Current is null)
            Navigation.NavigateTo("roster");
        else
            StateHasChanged();
    }

    public void Dispose() => InvestigatorService.OnChanged -= HandleChanged;
}
