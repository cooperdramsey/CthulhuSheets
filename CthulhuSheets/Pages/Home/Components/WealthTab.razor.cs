using CthulhuSheets.Data;

namespace CthulhuSheets.Pages.Home.Components;

public partial class WealthTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private Skill? CreditRating => Investigator.FindSkill(WellKnownSkills.CreditRating);

    private string? CreditRatingLabel => CreditRating is null ? null : CreditRating.EffectiveRegular switch
    {
        0          => "Penniless",
        <= 9       => "Poor",
        <= 49      => "Average",
        <= 89      => "Wealthy",
        <= 98      => "Rich",
        _          => "Super Rich"
    };

    private int? _creditRatingRoll;

    private void RollCreditRating(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
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
