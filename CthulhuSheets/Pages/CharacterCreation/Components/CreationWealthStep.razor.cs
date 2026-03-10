namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationWealthStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private int CreditRating =>
        Investigator.Skills
            .FirstOrDefault(s => s.Name.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase))
            ?.Regular ?? 0;

    private WealthTier CurrentTier => GetTier(CreditRating);

    protected override void OnParametersSet()
    {
        var tier = CurrentTier;
        Investigator.Wealth.SpendingLevel = tier.SpendingLevel;
        Investigator.Wealth.Cash = tier.Cash;
    }

    private void AddAsset() => Investigator.Wealth.Assets.Add(string.Empty);

    private void RemoveAsset(int index) => Investigator.Wealth.Assets.RemoveAt(index);

    private static WealthTier GetTier(int creditRating) => creditRating switch
    {
        0 => AllTiers[0],
        <= 9 => AllTiers[1],
        <= 49 => AllTiers[2],
        <= 89 => AllTiers[3],
        <= 98 => AllTiers[4],
        _ => AllTiers[5],
    };

    private static string FormatCrRange(WealthTier tier) =>
        tier.CrMin == tier.CrMax ? $"{tier.CrMin}" : $"{tier.CrMin}–{tier.CrMax}";

    private static string FormatDollar(int amount) => $"${amount:N0}";

    private static readonly WealthTier[] AllTiers =
    [
        new("Penniless",  0,  0,     0,      0,         0),
        new("Poor",       1,  9,     2,     10,       500),
        new("Average",   10, 49,    10,     50,     5_000),
        new("Wealthy",   50, 89,    50,    250,    50_000),
        new("Rich",      90, 98,   250,  2_500,   500_000),
        new("Super Rich", 99, 99, 5_000, 50_000, 5_000_000),
    ];

    private record WealthTier(string Label, int CrMin, int CrMax, int SpendingLevel, int Cash, int Assets);
}
