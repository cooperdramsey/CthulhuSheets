namespace CthulhuSheets.Helpers;

/// <summary>
/// Calculates the automatic-fire values used at the table (Call of Cthulhu 7e,
/// Combat: Automatic Fire).
/// </summary>
public static class AutomaticFireRules
{
    /// <summary>
    /// A volley may contain up to one tenth of the relevant SMG/MG skill,
    /// rounded down, with a minimum of three bullets.
    /// </summary>
    public static int GetMaximumVolleySize(int skillValue) =>
        Math.Max(3, Math.Max(0, skillValue) / 10);

    /// <summary>
    /// A regular success hits half the bullets fired, rounded down, with at
    /// least one hit when a bullet was fired.
    /// </summary>
    public static int GetRegularSuccessHits(int bulletsFired) =>
        bulletsFired <= 0 ? 0 : Math.Max(1, bulletsFired / 2);

    /// <summary>
    /// Later volleys add penalty dice, up to two.
    /// </summary>
    public static int GetPenaltyDice(int volleyNumber) =>
        Math.Min(2, Math.Max(0, volleyNumber - 1));

    /// <summary>
    /// After the two-penalty-die limit, each further volley raises the
    /// difficulty by one step.
    /// </summary>
    public static int GetDifficultyIncreases(int volleyNumber) =>
        Math.Max(0, volleyNumber - 3);
}
