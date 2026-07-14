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
    private int? _minRegular = null;
    private bool _combinedSelectMode;
    private readonly HashSet<Skill> _combinedSelection = new();
    private int? _lastCombinedRoll;

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

            return Investigator.Preferences.SkillSort switch
            {
                SkillSortMode.HighestFirst => skills.OrderByDescending(s => s.EffectiveRegular).ThenBy(s => s.Name),
                SkillSortMode.LowestFirst  => skills.OrderBy(s => s.EffectiveRegular).ThenBy(s => s.Name),
                _                          => skills.OrderBy(s => s.Name),
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
        _improvementResults = DevelopmentPhase.Run(Investigator, DiceRollService).ToList();
        await PersistAsync();
    }

    private async Task LoadDefaultSkills()
    {
        DefaultSkills.AddMissingTo(Investigator);
        await PersistAsync();
    }

    private async Task SetSort(SkillSortMode mode)
    {
        Investigator.Preferences.SkillSort = mode;
        await PersistAsync();
    }
}
