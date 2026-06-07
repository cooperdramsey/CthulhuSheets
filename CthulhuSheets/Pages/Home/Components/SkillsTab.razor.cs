using CthulhuSheets.Data;

namespace CthulhuSheets.Pages.Home.Components;

public partial class SkillsTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private string _skillFilter = string.Empty;
    private bool _skillsEditMode;
    private readonly Dictionary<Skill, int> _lastSkillRolls = new();

    private record ImprovementResult(string SkillName, int Roll, int OldValue, bool Improved, int NewValue);
    private List<ImprovementResult> _improvementResults = [];

    private IEnumerable<Skill> VisibleSkills =>
        (string.IsNullOrWhiteSpace(_skillFilter)
            ? Investigator.Skills
            : Investigator.Skills.Where(s => s.Name.Contains(_skillFilter, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(s => s.Name);

    private async Task AddSkill()
    {
        Investigator.Skills.Add(new Skill());
        await PersistAsync();
    }

    private async Task RemoveSkill(Skill skill)
    {
        _lastSkillRolls.Remove(skill);
        Investigator.Skills.Remove(skill);
        await PersistAsync();
    }

    // Credit Rating and Cthulhu Mythos are never ticked or improved via experience
    // (ch_4 Skill List; ch_5 Investigator Development Phase).
    private static readonly HashSet<string> NonImprovableSkills =
        new(StringComparer.OrdinalIgnoreCase) { "Credit Rating", "Cthulhu Mythos" };

    private async Task RollSkill(Skill skill, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _lastSkillRolls[skill] = result.Total;

        if (result.Total <= skill.EffectiveRegular
            && !skill.HasExperienceCheck
            && !NonImprovableSkills.Contains(skill.Name))
        {
            skill.HasExperienceCheck = true;
            await PersistAsync();
        }
    }

    private async Task ImproveSkills()
    {
        _improvementResults.Clear();
        var checkedSkills = Investigator.Skills
            .Where(s => s.HasExperienceCheck && !NonImprovableSkills.Contains(s.Name))
            .ToList();

        foreach (var skill in checkedSkills)
        {
            var current = skill.EffectiveRegular;
            var roll = DiceRollService.Roll(100);

            // Success if the roll beats the current value, or is over 95 (ch_5).
            // Skills may exceed 100% via development, so no upper cap is applied.
            if (roll > current || roll > 95)
            {
                var gain = DiceRollService.Roll(10);
                var newVal = current + gain;
                skill.Regular = newVal;
                _improvementResults.Add(new(skill.Name, roll, current, true, newVal));
            }
            else
            {
                _improvementResults.Add(new(skill.Name, roll, current, false, current));
            }

            skill.HasExperienceCheck = false;
        }

        await PersistAsync();
    }

    private async Task LoadDefaultSkills()
    {
        var existing = Investigator.Skills.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, baseVal) in DefaultSkills.All)
        {
            if (existing.Contains(name)) continue;
            var computedBase = DefaultSkills.ComputeBase(name, baseVal, Investigator);
            Investigator.Skills.Add(new Skill { Name = name, BaseValue = computedBase });
        }
        await PersistAsync();
    }
}
