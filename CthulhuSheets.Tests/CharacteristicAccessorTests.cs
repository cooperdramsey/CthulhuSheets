using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class CharacteristicAccessorTests
{
    [Theory]
    [InlineData("STR")]
    [InlineData("str")]
    [InlineData("PoW")]
    public void GetCharacteristic_IsCaseInsensitive(string abbrev)
    {
        var inv = new Investigator();
        Assert.NotNull(inv.GetCharacteristic(abbrev));
    }

    [Fact]
    public void GetCharacteristic_ReturnsCorrectStat()
    {
        var inv = new Investigator();
        Assert.Same(inv.Power, inv.GetCharacteristic("POW"));
        Assert.Same(inv.Education, inv.GetCharacteristic("EDU"));
    }

    [Fact]
    public void GetCharacteristic_UnknownReturnsNull()
    {
        var inv = new Investigator();
        Assert.Null(inv.GetCharacteristic("XYZ"));
    }

    [Fact]
    public void Characteristics_YieldsEightInCanonicalOrder()
    {
        var inv = new Investigator();
        var names = inv.Characteristics.Select(c => c.Name).ToArray();
        Assert.Equal(new[] { "STR", "CON", "SIZ", "DEX", "APP", "INT", "POW", "EDU" }, names);
    }
}
