namespace CthulhuSheets;

public partial class App
{
    private bool _isDarkMode;
    private MudThemeProvider _mudThemeProvider = default!;

    private readonly MudTheme _theme = new()
    {
        PaletteDark = new PaletteDark
        {
            Background = "#3a3b48"
        }
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            StateHasChanged();
        }
    }
}
