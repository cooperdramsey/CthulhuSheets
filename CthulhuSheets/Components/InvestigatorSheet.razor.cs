namespace CthulhuSheets.Components;

public partial class InvestigatorSheet
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private InvestigatorService InvestigatorService { get; set; } = default!;
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private static readonly string[] Tabs = ["Stats", "Skills", "Combat", "Items", "Wealth", "Info"];

    private string _activeTab = Tabs[0];
    private readonly string _portraitInputId = $"portrait-upload-{Guid.NewGuid():N}";

    private Task PersistAsync() => InvestigatorService.PersistAsync();

    private IEnumerable<(string Label, Characteristic Stat)> CharacteristicList =>
    [
        ("Strength",     Investigator.Strength),
        ("Constitution", Investigator.Constitution),
        ("Size",         Investigator.Size),
        ("Dexterity",    Investigator.Dexterity),
        ("Appearance",   Investigator.Appearance),
        ("Intelligence", Investigator.Intelligence),
        ("Power",        Investigator.Power),
        ("Education",    Investigator.Education),
    ];

    private async Task HandlePortraitUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Investigator.PortraitDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        await InvestigatorService.PersistAsync();
    }

    private readonly Dictionary<string, int> _lastRolls = new();
    private string _skillFilter = string.Empty;
    private bool _skillsEditMode;
    private readonly Dictionary<Skill, int> _lastSkillRolls = new();

    private void RollStat(Characteristic stat)
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _lastRolls[stat.Name] = result.Total;
    }

    private string? CheckIcon(string statName, int? threshold)
    {
        if (!threshold.HasValue || !_lastRolls.TryGetValue(statName, out var roll)) return null;
        return roll <= threshold.Value ? Icons.Material.Filled.Check : Icons.Material.Filled.Close;
    }

    private Color CheckColor(string statName, int? threshold)
    {
        if (!threshold.HasValue || !_lastRolls.TryGetValue(statName, out var roll)) return Color.Default;
        return roll <= threshold.Value ? Color.Success : Color.Error;
    }

    private async Task AddFellowInvestigator()
    {
        Investigator.FellowInvestigators.Add(new FellowInvestigator());
        await PersistAsync();
    }

    private async Task RemoveFellowInvestigator(FellowInvestigator fellow)
    {
        Investigator.FellowInvestigators.Remove(fellow);
        await PersistAsync();
    }

    private async Task AddAsset()
    {
        Investigator.Wealth.Assets.Add(string.Empty);
        await PersistAsync();
    }

    private async Task RemoveAsset(int index)
    {
        Investigator.Wealth.Assets.RemoveAt(index);
        await PersistAsync();
    }

    private async Task AddGearItem()
    {
        Investigator.GearAndPossessions.Add(string.Empty);
        await PersistAsync();
    }

    private async Task RemoveGearItem(int index)
    {
        Investigator.GearAndPossessions.RemoveAt(index);
        await PersistAsync();
    }

    private int? DodgeRegular => Investigator.Dexterity.Half;
    private int? DodgeHard => Investigator.Dexterity.Half / 2;
    private int? DodgeExtreme => Investigator.Dexterity.Half / 5;

    private async Task AddWeapon()
    {
        Investigator.Weapons.Add(new Weapon());
        await PersistAsync();
    }

    private async Task RemoveWeapon(Weapon weapon)
    {
        Investigator.Weapons.Remove(weapon);
        await PersistAsync();
    }

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

    private void RollSkill(Skill skill)
    {
        var result = DiceRollService.RollMany([(sides: 100, count: 1)]);
        _lastSkillRolls[skill] = result.Total;
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
