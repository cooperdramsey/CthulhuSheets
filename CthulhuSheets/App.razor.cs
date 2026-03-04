namespace CthulhuSheets;

public partial class App
{
    private bool _isDarkMode;
    private MudThemeProvider _mudThemeProvider = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            StateHasChanged();
        }
    }
}
