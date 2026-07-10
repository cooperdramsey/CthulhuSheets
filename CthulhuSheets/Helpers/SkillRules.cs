using CthulhuSheets.Models;

namespace CthulhuSheets.Helpers;

public static class SkillRules
{
    // Credit Rating and Cthulhu Mythos are never ticked or improved via experience
    // (ch_4 Skill List; ch_5 Investigator Development Phase).
    public static readonly HashSet<string> NonImprovableSkills =
        new(StringComparer.OrdinalIgnoreCase) { "Credit Rating", "Cthulhu Mythos" };

    // No experience check when a bonus die was used (ch_5 development phase).
    // Shared by every roll path (Skills tab, combined rolls, weapon rolls) so
    // they all tick identically by construction.
    public static bool ShouldMarkExperienceCheck(Skill skill, int roll, int modifier) =>
        roll <= skill.EffectiveRegular
        && modifier <= 0
        && !skill.HasExperienceCheck
        && !NonImprovableSkills.Contains(skill.Name);
}
