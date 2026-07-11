using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class ModelDerivedValueTests
{
    [Theory]
    [InlineData(50, 25, 10)]
    [InlineData(55, 27, 11)] // rounding-down case
    [InlineData(1, 0, 0)]
    [InlineData(99, 49, 19)]
    public void Characteristic_HalfAndFifth_AreFloorDivision(int regular, int half, int fifth)
    {
        var c = new Characteristic { Name = "STR", Regular = regular };

        Assert.Equal(half, c.Half);
        Assert.Equal(fifth, c.Fifth);
    }

    [Fact]
    public void Characteristic_HalfAndFifth_AreNull_WhenRegularNull()
    {
        var c = new Characteristic { Name = "STR" };

        Assert.Null(c.Regular);
        Assert.Null(c.Half);
        Assert.Null(c.Fifth);
    }

    [Fact]
    public void Skill_EffectiveRegular_FallsBackToBaseValue_WhenRegularNull()
    {
        var s = new Skill { Name = "Spot Hidden", BaseValue = 55, Regular = null };

        Assert.Equal(55, s.EffectiveRegular);
        Assert.Equal(27, s.Half);
        Assert.Equal(11, s.Fifth);
    }

    [Fact]
    public void Skill_EffectiveRegular_UsesRegular_WhenSet()
    {
        var s = new Skill { Name = "Spot Hidden", BaseValue = 55, Regular = 80 };

        Assert.Equal(80, s.EffectiveRegular);
        Assert.Equal(40, s.Half);
        Assert.Equal(16, s.Fifth);
    }
}
