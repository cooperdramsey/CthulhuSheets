using CthulhuSheets.Data;

namespace CthulhuSheets.Tests;

/// <summary>
/// Cross-validation / drift guard for the static game data. A failure here is a real
/// data bug (a mistyped skill name, an invalid formula characteristic, a bad credit-rating
/// range), not a flaky test — fix the data, do not relax the assertion.
/// </summary>
public class GameDataConsistencyTests
{
    private static readonly HashSet<string> ValidCharacteristics =
        new(StringComparer.Ordinal) { "STR", "CON", "SIZ", "DEX", "APP", "INT", "POW", "EDU" };

    private static readonly HashSet<string> DefaultSkillNames =
        DefaultSkills.All.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void EveryOccupationSkill_ExistsInDefaultSkillList()
    {
        var misses = new List<string>();

        foreach (var occ in Occupations.All)
            foreach (var skill in occ.Skills)
                if (!DefaultSkillNames.Contains(skill))
                    misses.Add($"{occ.Name} → \"{skill}\"");

        Assert.True(misses.Count == 0,
            "Occupation skills not found in DefaultSkills.All:\n" + string.Join("\n", misses));
    }

    [Fact]
    public void EverySkillPointFormula_UsesAValidCharacteristic()
    {
        var misses = new List<string>();

        foreach (var occ in Occupations.All)
            foreach (var formula in occ.SkillPointFormulas.Concat(occ.SkillPointFormulaOptions))
                if (!ValidCharacteristics.Contains(formula.Characteristic))
                    misses.Add($"{occ.Name} → \"{formula.Characteristic}\"");

        Assert.True(misses.Count == 0,
            "Formula characteristics that would silently evaluate to 0:\n" + string.Join("\n", misses));
    }

    [Fact]
    public void CreditRatingRanges_AreOrderedAndInBounds()
    {
        var problems = new List<string>();

        foreach (var occ in Occupations.All)
        {
            if (occ.CreditRatingMin > occ.CreditRatingMax)
                problems.Add($"{occ.Name}: min {occ.CreditRatingMin} > max {occ.CreditRatingMax}");
            if (occ.CreditRatingMin is < 0 or > 99)
                problems.Add($"{occ.Name}: min {occ.CreditRatingMin} out of [0,99]");
            if (occ.CreditRatingMax is < 0 or > 99)
                problems.Add($"{occ.Name}: max {occ.CreditRatingMax} out of [0,99]");
        }

        Assert.True(problems.Count == 0,
            "Credit Rating range problems:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void EachOccupation_HasNoDuplicateSkills_AndAtMostEight()
    {
        var problems = new List<string>();

        foreach (var occ in Occupations.All)
        {
            var distinct = occ.Skills.Select(s => s.ToLowerInvariant()).Distinct().Count();
            if (distinct != occ.Skills.Length)
                problems.Add($"{occ.Name}: duplicate skill(s) in [{string.Join(", ", occ.Skills)}]");
            if (occ.Skills.Length > 8)
                problems.Add($"{occ.Name}: {occ.Skills.Length} skills (> 8)");
        }

        Assert.True(problems.Count == 0,
            "Occupation skill-list problems:\n" + string.Join("\n", problems));
    }
}
