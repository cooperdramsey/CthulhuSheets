namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationOccupationSkillsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private string _skillFilter = string.Empty;
    private bool _defaultsLoaded;

    private IEnumerable<Skill> FilteredSkills =>
        (string.IsNullOrWhiteSpace(_skillFilter)
            ? Investigator.Skills
            : Investigator.Skills.Where(s => s.Name.Contains(_skillFilter, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(s => s.Name);

    protected override void OnParametersSet()
    {
        if (!_defaultsLoaded && Investigator.Skills.Count == 0)
        {
            PopulateDefaults();
            _defaultsLoaded = true;
        }
    }

    private void AddSkill()
    {
        Investigator.Skills.Add(new Skill());
    }

    private void RemoveSkill(Skill skill)
    {
        Investigator.Skills.Remove(skill);
    }

    private void LoadDefaultSkills()
    {
        PopulateDefaults();
    }

    private void PopulateDefaults()
    {
        var existing = Investigator.Skills
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
    }

    // TODO combine this with default skills from investigator to centralize.
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
