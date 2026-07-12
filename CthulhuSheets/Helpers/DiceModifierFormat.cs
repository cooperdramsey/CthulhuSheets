namespace CthulhuSheets.Helpers;

public static class DiceModifierFormat
{
    // Shared bonus/penalty die label. The CSS class per site differs (site-specific
    // prefixes), so only the label is centralized here.
    public static string BonusPenaltyLabel(int modifier) => modifier switch
    {
        2  => "+2 Bonus",
        1  => "+1 Bonus",
        0  => "Normal",
        -1 => "-1 Penalty",
        _  => "-2 Penalty"
    };
}
