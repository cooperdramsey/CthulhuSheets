using CthulhuSheets.Data;
using CthulhuSheets.Helpers;
using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class DefaultSkillsTests
{
    [Fact]
    public void AddMissingTo_EmptyInvestigator_SeedsAllStandardSkillsAsDefault()
    {
        var inv = new Investigator();

        var added = DefaultSkills.AddMissingTo(inv);

        Assert.Equal(DefaultSkills.All.Length, added);
        Assert.Equal(DefaultSkills.All.Length, inv.Skills.Count);
        Assert.All(inv.Skills, s => Assert.True(s.IsDefault));
    }

    [Fact]
    public void AddMissingTo_ComputesCharacteristicDerivedBases()
    {
        var inv = new Investigator();
        inv.Dexterity.Regular = 60;   // Dodge = ½DEX = 30
        inv.Education.Regular = 70;    // Language (Own) = EDU = 70

        DefaultSkills.AddMissingTo(inv);

        Assert.Equal(30, inv.FindSkill(WellKnownSkills.Dodge)!.BaseValue);
        Assert.Equal(70, inv.FindSkill(WellKnownSkills.LanguageOwn)!.BaseValue);
    }

    [Fact]
    public void AddMissingTo_SkipsSkillsAlreadyPresent_CaseInsensitive_NoDuplicates()
    {
        var inv = new Investigator();
        // Pre-add a skill matching a standard name by different casing, with a custom base.
        inv.Skills.Add(new Skill { Name = "dodge", BaseValue = 99, IsDefault = false });

        DefaultSkills.AddMissingTo(inv);

        // Only one "Dodge"/"dodge" — the pre-existing one, untouched.
        var dodges = inv.Skills.Where(s => s.Name.Equals("Dodge", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(dodges);
        Assert.Equal(99, dodges[0].BaseValue);
        Assert.False(dodges[0].IsDefault);
    }

    [Fact]
    public void AddMissingTo_OnFullList_AddsNothing()
    {
        var inv = new Investigator();
        DefaultSkills.AddMissingTo(inv);

        var addedSecondTime = DefaultSkills.AddMissingTo(inv);

        Assert.Equal(0, addedSecondTime);
    }
}
