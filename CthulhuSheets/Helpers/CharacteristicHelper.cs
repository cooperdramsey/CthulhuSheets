namespace CthulhuSheets.Helpers;

public static class CharacteristicHelper
{
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
}
