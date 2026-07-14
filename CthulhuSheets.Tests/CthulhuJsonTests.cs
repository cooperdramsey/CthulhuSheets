using System.Text.Json;
using CthulhuSheets.Helpers;
using CthulhuSheets.Models;

namespace CthulhuSheets.Tests;

public class CthulhuJsonTests
{
    [Fact]
    public void Options_RoundTripsAnInvestigatorLosslessly()
    {
        var original = new Investigator
        {
            Id = Guid.NewGuid(),
            Name = "Round Trip",
            Occupation = "Antiquarian",
            Age = 42,
        };
        original.Strength.Regular = 55;
        original.Education.Regular = 80;
        original.Skills.Add(new Skill { Name = "Library Use", BaseValue = 20, Regular = 65 });
        original.Wealth.Cash = 50m;

        var json = JsonSerializer.Serialize(original, CthulhuJson.Options);
        var restored = JsonSerializer.Deserialize<Investigator>(json, CthulhuJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored!.Id);
        Assert.Equal("Round Trip", restored.Name);
        Assert.Equal(55, restored.Strength.Regular);
        Assert.Equal(80, restored.Education.Regular);
        Assert.Single(restored.Skills);
        Assert.Equal(65, restored.Skills[0].Regular);
        Assert.Equal(50m, restored.Wealth.Cash);
    }

    [Fact]
    public void Options_WritesCamelCase()
    {
        var json = JsonSerializer.Serialize(new Investigator { Name = "X" }, CthulhuJson.Options);

        Assert.Contains("\"name\":", json);
        Assert.DoesNotContain("\"Name\":", json);
    }

    [Fact]
    public void Options_ReadsCaseInsensitively()
    {
        // A file with PascalCase keys must still deserialize (case-insensitive read).
        var restored = JsonSerializer.Deserialize<Investigator>(
            "{\"Name\":\"Pascal\",\"Age\":30}", CthulhuJson.Options);

        Assert.NotNull(restored);
        Assert.Equal("Pascal", restored!.Name);
        Assert.Equal(30, restored.Age);
    }

    [Fact]
    public void Export_IsIndentedButSameContract()
    {
        var indented = JsonSerializer.Serialize(new Investigator { Name = "X" }, CthulhuJson.Export);

        Assert.Contains("\n", indented);           // WriteIndented
        Assert.Contains("\"name\":", indented);     // still camelCase
    }

    [Fact]
    public void Options_WritesEnumsAsStrings()
    {
        var investigator = new Investigator { Name = "X" };
        investigator.Preferences.SkillSort = SkillSortMode.HighestFirst;

        var json = JsonSerializer.Serialize(investigator, CthulhuJson.Options);

        Assert.Contains("\"skillSort\":\"HighestFirst\"", json);
        Assert.DoesNotContain("\"skillSort\":1", json);
    }

    [Fact]
    public void Options_ReadsNumericEnumForBackCompat()
    {
        var restored = JsonSerializer.Deserialize<Investigator>(
            "{\"preferences\":{\"skillSort\":1}}", CthulhuJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(SkillSortMode.HighestFirst, restored!.Preferences.SkillSort);
    }

    [Fact]
    public void Options_MissingPreferencesDefaultsToAlphabetical()
    {
        var restored = JsonSerializer.Deserialize<Investigator>("{\"name\":\"X\"}", CthulhuJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(SkillSortMode.Alphabetical, restored!.Preferences.SkillSort);
    }
}
