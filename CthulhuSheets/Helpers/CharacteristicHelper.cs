namespace CthulhuSheets.Helpers;

public static class CharacteristicHelper
{
    // ── Alternative characteristic-generation method constants ──────────────

    /// <summary>Point Buy (CoC 7e creation option 4): total points to distribute.</summary>
    public const int PointBuyTotal = 460;

    /// <summary>Point Buy: minimum allowed value for any single characteristic.</summary>
    public const int PointBuyMin = 15;

    /// <summary>Point Buy: maximum allowed value for any single characteristic.</summary>
    public const int PointBuyMax = 90;

    /// <summary>Maximum characteristic value reachable via the Human Potential add-on (option 6).</summary>
    public const int HumanPotentialMax = 99;

    /// <summary>
    /// Quick Fire (CoC 7e creation option 5): the fixed set of values assigned
    /// across the eight characteristics (high → low for display convenience).
    /// </summary>
    public static readonly int[] QuickFireArray = [80, 70, 60, 60, 50, 50, 50, 40];

    /// <summary>
    /// Place Rolls (CoC 7e creation option 3): rolls a pool of eight values to be
    /// assigned freely — five 3D6 × 5 and three (2D6+6) × 5, each already multiplied.
    /// </summary>
    public static List<int> RollPlacePool(DiceRollService dice)
    {
        var pool = new List<int>(8);
        for (var i = 0; i < 5; i++)
            pool.Add((dice.Roll(6) + dice.Roll(6) + dice.Roll(6)) * 5);
        for (var i = 0; i < 3; i++)
            pool.Add((dice.Roll(6) + dice.Roll(6) + 6) * 5);
        return pool;
    }

    /// <summary>
    /// CoC 7e EDU improvement check: roll d100, if result &gt; current EDU add 1d10 (max 99).
    /// </summary>
    public static (bool Improved, int Roll, int Added) TryImproveEducation(Characteristic edu, DiceRollService dice)
    {
        var current = edu.Regular ?? 0;
        var roll = dice.Roll(100);
        if (roll > current)
        {
            var improvement = dice.Roll(10);
            var newValue = Math.Min(99, current + improvement);
            var added = newValue - current;
            edu.Regular = newValue;
            return (true, roll, added);
        }
        return (false, roll, 0);
    }

    /// <summary>
    /// Rolls a luck score: 3D6 × 5.
    /// </summary>
    public static int RollLuck(DiceRollService dice)
    {
        return (dice.Roll(6) + dice.Roll(6) + dice.Roll(6)) * 5;
    }

    /// <summary>
    /// Recomputes the characteristic-derived stats (HP max, MP max, starting SAN,
    /// MOV, Build, Damage Bonus) from the investigator's current characteristics.
    /// Maximums/derived values are overwritten; current pool values are preserved
    /// (only clamped down if they now exceed the recomputed maximum, or seeded when
    /// unset). Shared by character creation and the play-time sheet so the formulas
    /// can never drift.
    /// </summary>
    public static void RecomputeDerived(Investigator investigator)
    {
        var pow = investigator.Power.Regular;
        var siz = investigator.Size.Regular;
        var con = investigator.Constitution.Regular;
        var str = investigator.Strength.Regular;
        var dex = investigator.Dexterity.Regular;

        if (pow.HasValue)
        {
            investigator.Sanity.Starting = pow.Value;
            investigator.Sanity.Current ??= pow.Value;

            var mpMax = pow.Value / 5;
            investigator.MagicPoints.Max = mpMax;
            investigator.MagicPoints.Current =
                investigator.MagicPoints.Current is int mp ? Math.Min(mp, mpMax) : mpMax;
        }

        if (siz.HasValue && con.HasValue)
        {
            var hpMax = (siz.Value + con.Value) / 10;
            investigator.HitPoints.Max = hpMax;
            investigator.HitPoints.Current =
                investigator.HitPoints.Current is int hp ? Math.Min(hp, hpMax) : hpMax;
        }

        if (str.HasValue && dex.HasValue && siz.HasValue)
        {
            int mov;
            if (str.Value > siz.Value && dex.Value > siz.Value)
                mov = 9;
            else if (str.Value < siz.Value && dex.Value < siz.Value)
                mov = 7;
            else
                mov = 8;

            mov -= (investigator.Age ?? 0) switch
            {
                >= 80 => 5,
                >= 70 => 4,
                >= 60 => 3,
                >= 50 => 2,
                >= 40 => 1,
                _ => 0
            };

            investigator.MovementRate = mov;
        }

        if (str.HasValue && siz.HasValue)
        {
            (investigator.DamageBonus, investigator.Build) = (str.Value + siz.Value) switch
            {
                <= 64 => ("-2", -2),
                <= 84 => ("-1", -1),
                <= 124 => ("0", 0),
                <= 164 => ("1d4", 1),
                <= 204 => ("1d6", 2),
                <= 284 => ("2d6", 3),
                <= 364 => ("3d6", 4),
                <= 444 => ("4d6", 5),
                _ => ("5d6", 6)
            };
        }
    }
}
