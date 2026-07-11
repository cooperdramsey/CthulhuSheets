using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class OccupationTests
{
    private static Investigator KnownInvestigator()
    {
        var inv = new Investigator();
        inv.Education.Regular = 60;
        inv.Power.Regular = 50;
        inv.Dexterity.Regular = 40;
        inv.Strength.Regular = 70;
        return inv;
    }

    [Fact]
    public void Evaluate_MapsCharacteristicTimesMultiplier()
    {
        var inv = KnownInvestigator();

        Assert.Equal(240, Occupation.Evaluate(new SkillPointFormula("EDU", 4), inv)); // 60 × 4
        Assert.Equal(100, Occupation.Evaluate(new SkillPointFormula("POW", 2), inv)); // 50 × 2
        Assert.Equal(140, Occupation.Evaluate(new SkillPointFormula("STR", 2), inv)); // 70 × 2
    }

    [Fact]
    public void Evaluate_UnknownCharacteristic_IsZero()
    {
        var inv = KnownInvestigator();
        Assert.Equal(0, Occupation.Evaluate(new SkillPointFormula("XXX", 2), inv));
    }

    [Fact]
    public void ComputeSkillPoints_FixedFormula_EqualsFixedSum_WhenNoOption()
    {
        var inv = KnownInvestigator();
        var occ = new Occupation { SkillPointFormulas = [new("EDU", 4)] };

        Assert.Equal(240, occ.ComputeSkillPoints(inv));
    }

    [Fact]
    public void ComputeSkillPoints_AddsChosenOption_WhenSupplied()
    {
        var inv = KnownInvestigator();
        var occ = new Occupation { SkillPointFormulas = [new("EDU", 4)] };

        // A non-null chosenOption is always added on top of the fixed formulas.
        Assert.Equal(340, occ.ComputeSkillPoints(inv, new SkillPointFormula("POW", 2))); // 240 + 100
    }

    [Fact]
    public void ComputeSkillPoints_OptionFormula_AddsChosenOptionOnly()
    {
        var inv = KnownInvestigator();
        // Mirrors Artist: EDU × 2 + either POW × 2 or DEX × 2.
        var occ = new Occupation
        {
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("POW", 2), new("DEX", 2)]
        };

        Assert.Equal(120, occ.ComputeSkillPoints(inv));                                    // fixed only: 60×2
        Assert.Equal(220, occ.ComputeSkillPoints(inv, new SkillPointFormula("POW", 2)));   // + 50×2
        Assert.Equal(200, occ.ComputeSkillPoints(inv, new SkillPointFormula("DEX", 2)));   // + 40×2
    }
}
