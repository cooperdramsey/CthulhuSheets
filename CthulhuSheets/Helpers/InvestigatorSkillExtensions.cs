using CthulhuSheets.Models;

namespace CthulhuSheets.Helpers;

public static class InvestigatorSkillExtensions
{
    // Ordinal case-insensitive lookup, matching every existing hand-written call
    // site. Returns null when the skill is absent (same as FirstOrDefault).
    public static Skill? FindSkill(this Investigator inv, string name) =>
        inv.Skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
