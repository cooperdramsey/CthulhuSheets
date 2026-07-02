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

    // Cash and Assets scale with the investigator's exact Credit Rating
    // (Table II, 1920s): e.g. Average cash = CR×2, assets = CR×50.
    private decimal CurrentCash => ComputeCash(CurrentTier, CreditRating);
    private decimal CurrentAssets => ComputeAssets(CurrentTier, CreditRating);

    protected override void OnParametersSet()
    {
        Investigator.Wealth.SpendingLevel = CurrentTier.SpendingLevel;
        Investigator.Wealth.Cash = CurrentCash;
    }

    private void AddAsset() => Investigator.Wealth.Assets.Add(string.Empty);

    private void RemoveAsset(int index) => Investigator.Wealth.Assets.RemoveAt(index);

    private static WealthTier GetTier(int creditRating) => creditRating switch
    {
        <= 0 => AllTiers[0],
        <= 9 => AllTiers[1],
        <= 49 => AllTiers[2],
        <= 89 => AllTiers[3],
        <= 98 => AllTiers[4],
        _ => AllTiers[5],
    };

    private static decimal ComputeCash(WealthTier tier, int creditRating) =>
        tier.FixedCash ?? creditRating * tier.CashMultiplier;

    private static decimal ComputeAssets(WealthTier tier, int creditRating) =>
        tier.FixedAssets ?? creditRating * tier.AssetMultiplier;

    private static string FormatCrRange(WealthTier tier) =>
        tier.CrMin == tier.CrMax ? $"{tier.CrMin}" : $"{tier.CrMin}–{tier.CrMax}";

    private static string FormatDollar(decimal amount) =>
        amount == decimal.Truncate(amount) ? $"${amount:N0}" : $"${amount:N2}";

    // Cash/Assets columns scale with Credit Rating, so the table shows each band's range.
    private static string FormatCashCell(WealthTier tier) =>
        tier.FixedCash is decimal fixedCash
            ? FormatDollar(fixedCash)
            : $"{FormatDollar(tier.CrMin * tier.CashMultiplier)}–{FormatDollar(tier.CrMax * tier.CashMultiplier)}";

    private static string FormatAssetCell(WealthTier tier) =>
        tier.FixedAssets is decimal fixedAssets
            ? (fixedAssets == 0 ? "None" : FormatDollar(fixedAssets))
            : $"{FormatDollar(tier.CrMin * tier.AssetMultiplier)}–{FormatDollar(tier.CrMax * tier.AssetMultiplier)}";

    // 1920s Table II. Cash/Assets derive from CR × multiplier within each band;
    // the Penniless and Super Rich bands use fixed amounts instead.
    private static readonly WealthTier[] AllTiers =
    [
        //   Label          Min Max  Spend   CashX AssetX  FixedCash  FixedAssets
        new("Penniless",      0,  0,  0.50m,     0,     0,     0.50m,           0),
        new("Poor",           1,  9,     2,      1,    10),
        new("Average",       10, 49,    10,      2,    50),
        new("Wealthy",       50, 89,    50,      5,   500),
        new("Rich",          90, 98,   250,     20,  2000),
        new("Super Rich",    99, 99,  5000,      0,     0,    50_000,   5_000_000),
    ];

    private record WealthTier(
        string Label,
        int CrMin,
        int CrMax,
        decimal SpendingLevel,
        int CashMultiplier,
        int AssetMultiplier,
        decimal? FixedCash = null,
        decimal? FixedAssets = null);
}
