using CthulhuSheets.Helpers;

namespace CthulhuSheets.Tests;

public class AutomaticFireRulesTests
{
    [Theory]
    [InlineData(0, 3)]
    [InlineData(39, 3)]
    [InlineData(40, 4)]
    [InlineData(75, 7)]
    [InlineData(99, 9)]
    public void MaximumVolleySize_UsesSkillTenthWithMinimumOfThree(int skill, int expected)
    {
        Assert.Equal(expected, AutomaticFireRules.GetMaximumVolleySize(skill));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(9, 4)]
    public void RegularSuccessHits_AreHalfTheVolleyWithMinimumOne(int bullets, int expected)
    {
        Assert.Equal(expected, AutomaticFireRules.GetRegularSuccessHits(bullets));
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 2, 0)]
    [InlineData(4, 2, 1)]
    [InlineData(5, 2, 2)]
    public void LaterVolleys_ApplyCappedPenaltyDiceThenRaiseDifficulty(
        int volleyNumber,
        int expectedPenaltyDice,
        int expectedDifficultyIncreases)
    {
        Assert.Equal(expectedPenaltyDice, AutomaticFireRules.GetPenaltyDice(volleyNumber));
        Assert.Equal(expectedDifficultyIncreases, AutomaticFireRules.GetDifficultyIncreases(volleyNumber));
    }
}
