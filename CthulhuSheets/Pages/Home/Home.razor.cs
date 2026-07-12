namespace CthulhuSheets.Pages.Home;

public partial class Home : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        InvestigatorService.OnChanged += HandleChanged;
        // Landing on the roster when there's no active character is intentional:
        // the roster is the app's home/pick-a-character screen. A returning user is
        // deliberately taken there to choose, rather than resumed onto a sheet.
        if (InvestigatorService.Current is null)
            Navigation.NavigateTo("roster");
    }

    // OnChanged can be raised off the render thread, so marshal back via
    // InvokeAsync (matching Roster's pattern) before touching component state.
    private void HandleChanged()
    {
        _ = InvokeAsync(() =>
        {
            if (InvestigatorService.Current is null)
                Navigation.NavigateTo("roster");
            else
                StateHasChanged();
        });
    }

    public void Dispose() => InvestigatorService.OnChanged -= HandleChanged;
}
