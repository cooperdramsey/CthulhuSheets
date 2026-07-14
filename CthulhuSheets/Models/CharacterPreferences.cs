namespace CthulhuSheets.Models;

public enum SkillSortMode
{
    Alphabetical,   // A → Z (default)
    HighestFirst,   // EffectiveRegular descending
    LowestFirst     // EffectiveRegular ascending
}

public class CharacterPreferences
{
    // Default matches today's Skills-tab behavior (A → Z).
    public SkillSortMode SkillSort { get; set; } = SkillSortMode.Alphabetical;
}
