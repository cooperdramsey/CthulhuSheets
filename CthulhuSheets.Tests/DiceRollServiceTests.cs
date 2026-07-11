using System.Text.RegularExpressions;
using CthulhuSheets.Services;

namespace CthulhuSheets.Tests;

public class DiceRollServiceTests
{
    private const int Iterations = 10000;

    // Extracts the parenthesized candidate list from a bonus/penalty expression,
    // e.g. "1d100 +bonus (34, 74)" → [34, 74].
    private static int[] ParseCandidates(string expression)
    {
        var match = Regex.Match(expression, @"\(([^)]*)\)");
        Assert.True(match.Success, $"no candidate list in expression: {expression}");
        return match.Groups[1].Value
            .Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(100)]
    public void Roll_IsAlwaysWithinOneToSides(int sides)
    {
        var dice = new DiceRollService(new Random(1));

        for (var i = 0; i < Iterations; i++)
        {
            var r = dice.Roll(sides);
            Assert.InRange(r, 1, sides);
        }
    }

    [Fact]
    public void RollPercentile_IsAlwaysOneToHundred()
    {
        var dice = new DiceRollService(new Random(7));

        for (var i = 0; i < Iterations; i++)
        {
            var total = dice.RollPercentile().Total;
            Assert.InRange(total, 1, 100);
        }
    }

    [Theory]
    [InlineData(1)]  // one bonus die  → keep lowest
    [InlineData(2)]  // two bonus dice → keep lowest
    public void RollPercentile_Bonus_KeepsLowestCandidate(int bonus)
    {
        var dice = new DiceRollService(new Random(11));

        for (var i = 0; i < Iterations; i++)
        {
            var group = dice.RollPercentile(bonus);
            var candidates = ParseCandidates(group.Expression);

            Assert.Equal(bonus + 1, candidates.Length);
            Assert.Equal(candidates.Min(), group.Total);
        }
    }

    [Theory]
    [InlineData(-1)] // one penalty die  → keep highest
    [InlineData(-2)] // two penalty dice → keep highest
    public void RollPercentile_Penalty_KeepsHighestCandidate(int penalty)
    {
        var dice = new DiceRollService(new Random(13));

        for (var i = 0; i < Iterations; i++)
        {
            var group = dice.RollPercentile(penalty);
            var candidates = ParseCandidates(group.Expression);

            Assert.Equal(Math.Abs(penalty) + 1, candidates.Length);
            Assert.Equal(candidates.Max(), group.Total);
        }
    }

    [Fact]
    public void RollPercentile_ProducesTheHundredEdge()
    {
        var dice = new DiceRollService(new Random(17));
        var sawHundred = false;

        for (var i = 0; i < Iterations; i++)
        {
            var total = dice.RollPercentile().Total;
            Assert.InRange(total, 1, 100); // never 0
            if (total == 100)
                sawHundred = true;
        }

        Assert.True(sawHundred, "expected a 00-tens/0-units roll to read as 100 across iterations");
    }
}
