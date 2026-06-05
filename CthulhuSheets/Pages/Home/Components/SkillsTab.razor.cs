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

    private async Task RollSkill(Skill skill, int modifier = 0)
    {
        var result = DiceRollService.RollPercentile(modifier);
        _lastSkillRolls[skill] = result.Total;

        if (result.Total <= skill.EffectiveRegular && !skill.HasExperienceCheck)
        {
            skill.HasExperienceCheck = true;
            await PersistAsync();
        }
    }

    private async Task ImproveSkills()
    {
        _improvementResults.Clear();
        var checkedSkills = Investigator.Skills.Where(s => s.HasExperienceCheck).ToList();

        foreach (var skill in checkedSkills)
        {
            var current = skill.EffectiveRegular;
            var roll = DiceRollService.Roll(100);

            if (roll > current)
            {
                var gain = DiceRollService.Roll(10);
                var newVal = Math.Min(99, current + gain);
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
        foreach (var (name, baseVal) in DefaultSkills)
        {
            if (existing.Contains(name)) continue;
            var computedBase = name switch
            {
                "Dodge"          => Investigator.Dexterity.Half ?? 0,
                "Language (Own)" => Investigator.Education.Regular ?? 0,
                _                => baseVal
            };
            Investigator.Skills.Add(new Skill { Name = name, BaseValue = computedBase });
        }
        await PersistAsync();
    }

    private static readonly (string Name, int BaseValue)[] DefaultSkills =
    [
        ("Accounting",               5),
        ("Anthropology",             1),
        ("Appraise",                 5),
        ("Archaeology",              1),
        ("Art/Craft",                5),
        ("Charm",                   15),
        ("Climb",                   20),
        ("Computer Use",             5),
        ("Credit Rating",           25),
        ("Cthulhu Mythos",           0),
        ("Disguise",                 5),
        ("Dodge",                    0),
        ("Drive Auto",              20),
        ("Electrical Repair",       10),
        ("Fast Talk",                5),
        ("Fighting (Brawl)",        25),
        ("Firearms (Handgun)",      20),
        ("Firearms (Rifle/Shotgun)", 25),
        ("First Aid",               30),
        ("History",                  5),
        ("Intimidate",              15),
        ("Jump",                    20),
        ("Language (Other)",         1),
        ("Language (Own)",           0),
        ("Law",                      5),
        ("Library Use",             20),
        ("Listen",                  20),
        ("Locksmith",                1),
        ("Mechanical Repair",       10),
        ("Medicine",                 1),
        ("Natural World",           10),
        ("Navigate",                10),
        ("Occult",                   5),
        ("Operate Heavy Machinery",  1),
        ("Persuade",                10),
        ("Pilot",                    1),
        ("Psychoanalysis",           1),
        ("Psychology",              10),
        ("Ride",                     5),
        ("Science",                  1),
        ("Sleight of Hand",         10),
        ("Spot Hidden",             25),
        ("Stealth",                 20),
        ("Survival",                10),
        ("Swim",                    20),
        ("Throw",                   20),
        ("Track",                   10),
    ];
}
