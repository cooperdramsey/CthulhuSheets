namespace CthulhuSheets.Models;

public class Occupation
{
    public string Name { get; init; } = string.Empty;
    public string[] Skills { get; init; } = [];
    public int CreditRatingMin { get; init; }
    public int CreditRatingMax { get; init; }
    public string[] SuggestedContacts { get; init; } = [];
    public SkillPointFormula[] SkillPointFormulas { get; init; } = [];

    public int ComputeSkillPoints(Investigator investigator)
    {
        var total = 0;
        foreach (var formula in SkillPointFormulas)
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
            total += baseValue * formula.Multiplier;
        }
        return total;
    }
}

public record SkillPointFormula(string Characteristic, int Multiplier);
