namespace CthulhuSheets.Models;

public class Occupation
{
    public string Name { get; init; } = string.Empty;
    public string[] Skills { get; init; } = [];
    public int CreditRatingMin { get; init; }
    public int CreditRatingMax { get; init; }
    public string[] SuggestedContacts { get; init; } = [];
    public SkillPointFormula[] SkillPointFormulas { get; init; } = [];

    /// <summary>
    /// The "either X × 2 or Y × 2" part of the book's skill-point formula
    /// (e.g. Artist: EDU × 2 + either POW × 2 or DEX × 2). The player picks
    /// exactly one; empty for occupations with a fixed formula.
    /// </summary>
    public SkillPointFormula[] SkillPointFormulaOptions { get; init; } = [];

    public int ComputeSkillPoints(Investigator investigator, SkillPointFormula? chosenOption = null)
    {
        var total = SkillPointFormulas.Sum(f => Evaluate(f, investigator));
        if (chosenOption is not null)
            total += Evaluate(chosenOption, investigator);
        return total;
    }

    public static int Evaluate(SkillPointFormula formula, Investigator investigator)
    {
        var baseValue = formula.Characteristic switch
        {
            "EDU" => investigator.Education.Regular ?? 0,
            "DEX" => investigator.Dexterity.Regular ?? 0,
            "STR" => investigator.Strength.Regular ?? 0,
            "CON" => investigator.Constitution.Regular ?? 0,
            "SIZ" => investigator.Size.Regular ?? 0,
            "APP" => investigator.Appearance.Regular ?? 0,
            "INT" => investigator.Intelligence.Regular ?? 0,
            "POW" => investigator.Power.Regular ?? 0,
            _ => 0
        };
        return baseValue * formula.Multiplier;
    }
}

public record SkillPointFormula(string Characteristic, int Multiplier);
