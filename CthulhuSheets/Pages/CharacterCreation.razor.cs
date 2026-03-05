namespace CthulhuSheets.Pages;

public partial class CharacterCreation
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
}
