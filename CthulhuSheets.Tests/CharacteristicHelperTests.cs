using CthulhuSheets.Helpers;
using CthulhuSheets.Models;
using CthulhuSheets.Services;

namespace CthulhuSheets.Tests;

public class CharacteristicHelperTests
{
    // STR/SIZ split so STR+SIZ equals the target sum (values may exceed 90; RecomputeDerived does not cap).
    private static Investigator WithStrSiz(int str, int siz)
    {
        var inv = new Investigator();
        inv.Strength.Regular = str;
        inv.Size.Regular = siz;
        return inv;
    }

    [Theory]
    // Every band boundary pair (low edge of each band, and the first value of the next band).
    [InlineData(64, "-2", -2)]
    [InlineData(65, "-1", -1)]
    [InlineData(84, "-1", -1)]
    [InlineData(85, "0", 0)]
    [InlineData(124, "0", 0)]
    [InlineData(125, "1d4", 1)]
    [InlineData(164, "1d4", 1)]
    [InlineData(165, "1d6", 2)]
    [InlineData(204, "1d6", 2)]
    [InlineData(205, "2d6", 3)]
    [InlineData(284, "2d6", 3)]
    [InlineData(285, "3d6", 4)]
    [InlineData(364, "3d6", 4)]
    [InlineData(365, "4d6", 5)]
    [InlineData(444, "4d6", 5)]
    [InlineData(445, "5d6", 6)]
    [InlineData(500, "5d6", 6)] // deep in the top band
    public void RecomputeDerived_DamageBonusAndBuild_MatchBandEdges(int strPlusSiz, string expectedDb, int expectedBuild)
    {
        // Split the target sum across STR and SIZ (both non-null so the DB/Build branch runs).
        var str = strPlusSiz / 2;
        var siz = strPlusSiz - str;
        var inv = WithStrSiz(str, siz);

        CharacteristicHelper.RecomputeDerived(inv);

        Assert.Equal(expectedDb, inv.DamageBonus);
        Assert.Equal(expectedBuild, inv.Build);
    }

    [Fact]
    public void RecomputeDerived_HpMpSan_UseBookFormulas()
    {
        var inv = new Investigator();
        inv.Size.Regular = 60;
        inv.Constitution.Regular = 55;
        inv.Power.Regular = 65;

        CharacteristicHelper.RecomputeDerived(inv);

        Assert.Equal(11, inv.HitPoints.Max);        // (60 + 55) / 10 = 11 (floor)
        Assert.Equal(13, inv.MagicPoints.Max);      // 65 / 5 = 13
        Assert.Equal(65, inv.Sanity.Starting);      // starting SAN = POW
    }

    [Theory]
    // MOV base: both STR & DEX > SIZ → 9; both < SIZ → 7; mixed → 8.
    [InlineData(60, 60, 50, 9)]
    [InlineData(40, 40, 50, 7)]
    [InlineData(60, 40, 50, 8)]
    public void RecomputeDerived_MovBase(int str, int dex, int siz, int expectedMov)
    {
        var inv = new Investigator();
        inv.Strength.Regular = str;
        inv.Dexterity.Regular = dex;
        inv.Size.Regular = siz;

        CharacteristicHelper.RecomputeDerived(inv);

        Assert.Equal(expectedMov, inv.MovementRate);
    }

    [Fact]
    public void RecomputeDerived_MovAgePenalty_Applies()
    {
        var inv = new Investigator();
        inv.Strength.Regular = 60;
        inv.Dexterity.Regular = 40;
        inv.Size.Regular = 50; // mixed → base 8
        inv.Age = 55;          // 50s → −2

        CharacteristicHelper.RecomputeDerived(inv);

        Assert.Equal(6, inv.MovementRate);
    }

    [Fact]
    public void PointBuyAndQuickFire_ConstantsMatchRules()
    {
        Assert.Equal(460, CharacteristicHelper.PointBuyTotal);
        Assert.Equal(15, CharacteristicHelper.PointBuyMin);
        Assert.Equal(90, CharacteristicHelper.PointBuyMax);
        Assert.Equal(99, CharacteristicHelper.HumanPotentialMax);
        Assert.Equal(new[] { 80, 70, 60, 60, 50, 50, 50, 40 }, CharacteristicHelper.QuickFireArray);
    }

    [Fact]
    public void RollLuck_IsAlwaysInRangeAndDivisibleByFive()
    {
        var dice = new DiceRollService(new Random(12345));

        for (var i = 0; i < 500; i++)
        {
            var luck = CharacteristicHelper.RollLuck(dice);
            Assert.InRange(luck, 15, 90);
            Assert.Equal(0, luck % 5);
        }
    }

    [Fact]
    public void RollPlacePool_ReturnsEightValuesInCorrectPerDieRanges()
    {
        var dice = new DiceRollService(new Random(54321));

        for (var i = 0; i < 500; i++)
        {
            var pool = CharacteristicHelper.RollPlacePool(dice);

            Assert.Equal(8, pool.Count);
            for (var j = 0; j < 5; j++) // five 3d6 × 5 → [15, 90]
            {
                Assert.InRange(pool[j], 15, 90);
                Assert.Equal(0, pool[j] % 5);
            }
            for (var j = 5; j < 8; j++) // three (2d6+6) × 5 → [40, 90]
            {
                Assert.InRange(pool[j], 40, 90);
                Assert.Equal(0, pool[j] % 5);
            }
        }
    }

    [Fact]
    public void TryImproveEducation_NeverExceeds99()
    {
        var dice = new DiceRollService(new Random(999));
        var edu = new Characteristic { Name = "EDU", Regular = 99 };

        // current == 99: a d100 can only beat it on a roll of 100, and Min(99, 99+d10) caps at 99.
        for (var i = 0; i < 500; i++)
        {
            CharacteristicHelper.TryImproveEducation(edu, dice);
            Assert.Equal(99, edu.Regular);
        }
    }

    [Fact]
    public void TryImproveEducation_ImprovesOnlyWhenRollExceedsCurrent_AndIsConsistent()
    {
        var dice = new DiceRollService(new Random(2024));
        var sawImprove = false;
        var sawNoImprove = false;

        for (var i = 0; i < 500; i++)
        {
            var edu = new Characteristic { Name = "EDU", Regular = 1 };
            var before = edu.Regular!.Value;

            var (improved, roll, added) = CharacteristicHelper.TryImproveEducation(edu, dice);
            var after = edu.Regular!.Value;

            if (improved)
            {
                sawImprove = true;
                Assert.True(roll > before);
                Assert.InRange(after, before + 1, 99);
                Assert.Equal(after - before, added);
            }
            else
            {
                sawNoImprove = true;
                Assert.True(roll <= before);
                Assert.Equal(before, after);
                Assert.Equal(0, added);
            }
        }

        Assert.True(sawImprove, "expected at least one improving outcome across iterations");
        Assert.True(sawNoImprove, "expected at least one non-improving outcome across iterations");
    }
}
