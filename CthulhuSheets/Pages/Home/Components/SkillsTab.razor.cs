using CthulhuSheets.Data;

namespace CthulhuSheets.Pages.Home.Components;

public partial class SkillsTab
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private enum SortMode { Alpha, RegularDesc, RegularAsc }

    private string _skillFilter = string.Empty;
    private bool _skillsEditMode;
    private readonly Dictionary<Skill, int> _lastSkillRolls = new();
    private SortMode _sortMode = SortMode.Alpha;
    private int? _minRegular = null;
    private bool _combinedSelectMode;
    private readonly HashSet<Skill> _combinedSelection = new();
    private int? _lastCombinedRoll;

    private record ImprovementResult(string SkillName, int Roll, int OldValue, bool Improved, int NewValue, int SanityGained = 0);
    private List<ImprovementResult> _improvementResults = [];

    private IEnumerable<Skill> VisibleSkills
    {
        get
        {
            var skills = string.IsNullOrWhiteSpace(_skillFilter)
                ? Investigator.Skills.AsEnumerable()
                : Investigator.Skills.Where(s => s.Name.Contains(_skillFilter, StringComparison.OrdinalIgnoreCase));

            if (_minRegular.HasValue && _minRegular.Value > 0)
                skills = skills.Where(s => s.EffectiveRegular >= _minRegular.Value);

            return _sortMode switch
            {
                SortMode.RegularDesc => skills.OrderByDescending(s => s.EffectiveRegular).ThenBy(s => s.Name),
                SortMode.RegularAsc  => skills.OrderBy(s => s.EffectiveRegular).ThenBy(s => s.Name),
                _                    => skills.OrderBy(s => s.Name),
            };
        }
    }

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

    private async Task RollSkill(Skill skill, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _lastSkillRolls[skill] = result.Total;

        if (TryMarkExperienceCheck(skill, result.Total, modifier))
            await PersistAsync();
    }

    // Shared by single-skill and combined rolls so both paths tick identically.
    private bool TryMarkExperienceCheck(Skill skill, int roll, int modifier)
    {
        if (SkillRules.ShouldMarkExperienceCheck(skill, roll, modifier))
        {
            skill.HasExperienceCheck = true;
            return true;
        }

        return false;
    }

    private void ToggleEditMode()
    {
        _skillsEditMode = !_skillsEditMode;
        if (_skillsEditMode)
        {
            _combinedSelectMode = false;
            _combinedSelection.Clear();
            _lastCombinedRoll = null;
        }
    }

    private void ToggleCombinedSelectMode()
    {
        _combinedSelectMode = !_combinedSelectMode;
        if (_combinedSelectMode)
        {
            _skillsEditMode = false;
        }
        else
        {
            _combinedSelection.Clear();
            _lastCombinedRoll = null;
        }
    }

    private async Task RollCombined(int modifier)
    {
        if (_combinedSelection.Count < 2) return;

        var result = DiceRollService.RollPercentile(modifier);
        _lastCombinedRoll = result.Total;

        var tickedAny = false;
        foreach (var skill in _combinedSelection.ToList())
        {
            _lastSkillRolls[skill] = result.Total;
            tickedAny |= TryMarkExperienceCheck(skill, result.Total, modifier);
        }

        if (tickedAny)
            await PersistAsync();
    }

    private async Task ImproveSkills()
    {
        _improvementResults.Clear();
        var checkedSkills = Investigator.Skills
            .Where(s => s.HasExperienceCheck && !SkillRules.NonImprovableSkills.Contains(s.Name))
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

                // Reaching 90%+ during the development phase grants +2D6 Sanity
                // (ch_5), never above the 99 − Cthulhu Mythos maximum (ch_8).
                var sanityGained = 0;
                if (current < 90 && newVal >= 90)
                {
                    sanityGained = DiceRollService.Roll(6) + DiceRollService.Roll(6);
                    var sanMax = Math.Max(0, 99 - MythosValue);
                    var newSan = Math.Min(sanMax, (Investigator.Sanity.Current ?? 0) + sanityGained);
                    sanityGained = Math.Max(0, newSan - (Investigator.Sanity.Current ?? 0));
                    Investigator.Sanity.Current = newSan;
                }

                _improvementResults.Add(new(skill.Name, roll, current, true, newVal, sanityGained));
            }
            else
            {
                _improvementResults.Add(new(skill.Name, roll, current, false, current));
            }

            skill.HasExperienceCheck = false;
        }

        await PersistAsync();
    }

    private int MythosValue =>
        Investigator.Skills
            .FirstOrDefault(s => s.Name.Equals("Cthulhu Mythos", StringComparison.OrdinalIgnoreCase))
            ?.EffectiveRegular ?? 0;

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
