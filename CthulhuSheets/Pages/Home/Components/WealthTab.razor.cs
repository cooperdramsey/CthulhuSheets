namespace CthulhuSheets.Pages.Home.Components;

public partial class WealthTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private Skill? CreditRating =>
        Investigator.Skills.FirstOrDefault(s =>
            s.Name.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase));

    private int? _creditRatingRoll;

    private void RollCreditRating()
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _creditRatingRoll = result.Total;
    }

    private async Task AddAsset()
    {
        Investigator.Wealth.Assets.Add(string.Empty);
        await PersistAsync();
    }

    private async Task RemoveAsset(int index)
    {
        Investigator.Wealth.Assets.RemoveAt(index);
        await PersistAsync();
    }
}
