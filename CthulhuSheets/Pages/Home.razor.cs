namespace CthulhuSheets.Pages;

public partial class Home : IDisposable
{
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    protected override void OnInitialized()
    {
        InvestigatorService.OnChanged += StateHasChanged;
    }

    public void Dispose() => InvestigatorService.OnChanged -= StateHasChanged;
}
