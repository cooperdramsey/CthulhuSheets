using CthulhuSheets.Helpers;
using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class SkillRulesTests
{
    private static Skill Improvable(bool alreadyChecked = false) =>
        new() { Name = "Spot Hidden", BaseValue = 25, HasExperienceCheck = alreadyChecked };

    [Fact]
    public void MarksCheck_OnCleanSuccessWithNoBonusDie()
    {
        var skill = Improvable();
        Assert.True(SkillRules.ShouldMarkExperienceCheck(skill, roll: 25, modifier: 0));
    }

    [Fact]
    public void NoCheck_WhenRollJustExceedsSkill()
    {
        var skill = Improvable();
        Assert.False(SkillRules.ShouldMarkExperienceCheck(skill, roll: 26, modifier: 0));
    }

    [Fact]
    public void NoCheck_OnClearFailure()
    {
        var skill = Improvable();
        Assert.False(SkillRules.ShouldMarkExperienceCheck(skill, roll: 99, modifier: 0));
    }

    [Fact]
    public void NoCheck_WhenBonusDieUsed()
    {
        var skill = Improvable();
        Assert.False(SkillRules.ShouldMarkExperienceCheck(skill, roll: 25, modifier: 1));
    }

    [Fact]
    public void NoCheck_WhenAlreadyChecked()
    {
        var skill = Improvable(alreadyChecked: true);
        Assert.False(SkillRules.ShouldMarkExperienceCheck(skill, roll: 25, modifier: 0));
    }

    [Theory]
    [InlineData("Credit Rating")]
    [InlineData("Cthulhu Mythos")]
    [InlineData("cthulhu mythos")] // case-insensitive guard
    [InlineData("CREDIT RATING")]
    public void NoCheck_ForNonImprovableSkills_EvenOnCleanSuccess(string name)
    {
        var skill = new Skill { Name = name, BaseValue = 25 };
        Assert.False(SkillRules.ShouldMarkExperienceCheck(skill, roll: 10, modifier: 0));
    }

    [Fact]
    public void NonImprovableSkills_ContainsExactlyTheTwoGuardedSkills()
    {
        Assert.Equal(2, SkillRules.NonImprovableSkills.Count);
        Assert.Contains("Credit Rating", SkillRules.NonImprovableSkills);
        Assert.Contains("Cthulhu Mythos", SkillRules.NonImprovableSkills);
    }
}
