using CthulhuSheets.Pages.CharacterCreation.Components;

namespace CthulhuSheets.Pages.CharacterCreation;

public partial class CharacterCreation
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private static readonly string[] _steps =
        ["Profile", "Characteristics", "Occupation & Skills", "Backstory & Connections", "Wealth", "Equipment"];

    private int _currentStep;
    private readonly Investigator _draft = new();

    private CreationProfileStep? _profileStep;
    private CreationCharacteristicsStep? _characteristicsStep;

    private void GoBack()
    {
        if (_currentStep > 0) _currentStep--;
    }

    private void GoNext()
    {
        if (_currentStep == 0 && !(_profileStep?.Validate() ?? false))
            return;

        if (_currentStep == 1 && !(_characteristicsStep?.Validate() ?? false))
            return;

        if (_currentStep < _steps.Length - 1) _currentStep++;
    }

    private async Task SaveAsync()
    {
        await InvestigatorService.LoadAsync(_draft);
        var name = _draft.Name is { Length: > 0 } n ? n : "Investigator";
        Snackbar.Add($"{name} created!", Severity.Success);
        Navigation.NavigateTo("/");
    }
}
