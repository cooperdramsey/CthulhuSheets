using CthulhuSheets.Services;

namespace CthulhuSheets.Helpers;

// Result of one skill's development-phase improvement roll, for display.
public record ImprovementResult(
    string SkillName, int Roll, int OldValue, bool Improved, int NewValue, int SanityGained = 0);

// The Investigator Development Phase (ch_5): for each ticked, improvable skill,
// roll 1D100 and improve on a success (roll > current, or roll > 95), adding
// 1D10 (skills may exceed 100%). Crossing into 90%+ grants +2D6 Sanity, capped
// at the 99 − Cthulhu Mythos maximum (ch_8). Credit Rating and Cthulhu Mythos
// never improve. Extracted verbatim from SkillsTab so it can be unit-tested with
// a seeded DiceRollService.
public static class DevelopmentPhase
{
    public static IReadOnlyList<ImprovementResult> Run(Investigator investigator, DiceRollService dice)
    {
        var results = new List<ImprovementResult>();

        var checkedSkills = investigator.Skills
            .Where(s => s.HasExperienceCheck && !SkillRules.NonImprovableSkills.Contains(s.Name))
            .ToList();

        foreach (var skill in checkedSkills)
        {
            var current = skill.EffectiveRegular;
            var roll = dice.Roll(100);

            // Success if the roll beats the current value, or is over 95 (ch_5).
            // Skills may exceed 100% via development, so no upper cap is applied.
            if (roll > current || roll > 95)
            {
                var gain = dice.Roll(10);
                var newVal = current + gain;
                skill.Regular = newVal;

                // Reaching 90%+ during the development phase grants +2D6 Sanity
                // (ch_5), never above the 99 − Cthulhu Mythos maximum (ch_8).
                var sanityGained = 0;
                if (current < 90 && newVal >= 90)
                {
                    sanityGained = dice.Roll(6) + dice.Roll(6);
                    var sanMax = SanityRules.MaxSanity(investigator);
                    var newSan = Math.Min(sanMax, (investigator.Sanity.Current ?? 0) + sanityGained);
                    sanityGained = Math.Max(0, newSan - (investigator.Sanity.Current ?? 0));
                    investigator.Sanity.Current = newSan;
                }

                results.Add(new(skill.Name, roll, current, true, newVal, sanityGained));
            }
            else
            {
                results.Add(new(skill.Name, roll, current, false, current));
            }

            skill.HasExperienceCheck = false;
        }

        return results;
    }
}
