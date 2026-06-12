namespace CthulhuSheets.Pages.Home.Components;

public partial class SheetSidebar
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;

    private bool _editingOtherStats;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private int? _luckRoll;
    private int? _sanityRoll;

    private void RollLuck(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _luckRoll = result.Total;
    }

    private void RollSanity(int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _sanityRoll = result.Total;
    }

    private bool? LuckSuccess =>
        _luckRoll.HasValue && Investigator.Luck.Current.HasValue
            ? _luckRoll.Value <= Investigator.Luck.Current.Value
            : null;

    private bool? SanitySuccess =>
        _sanityRoll.HasValue && Investigator.Sanity.Current.HasValue
            ? _sanityRoll.Value <= Investigator.Sanity.Current.Value
            : null;

    // ── Sanity thresholds (derived; see ch_8) ────────
    private int CthulhuMythos =>
        Investigator.Skills
            .FirstOrDefault(s => s.Name.Equals("Cthulhu Mythos", StringComparison.OrdinalIgnoreCase))
            ?.EffectiveRegular ?? 0;

    // Maximum Sanity = 99 − Cthulhu Mythos. Sanity can never be restored above this.
    private int SanMax => Math.Max(0, 99 - CthulhuMythos);

    // Indefinite insanity triggers on losing ⅕ or more of current Sanity in one day.
    private int? IndefiniteLoss =>
        Investigator.Sanity.Current.HasValue ? Investigator.Sanity.Current.Value / 5 : null;
}
