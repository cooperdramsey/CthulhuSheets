using CthulhuSheets.Data;

namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationOccupationSkillsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    private string _skillFilter = string.Empty;
    private bool _defaultsLoaded;
    private Occupation? _selectedOccupation;
    private HashSet<string> _occupationSkillNames = new(StringComparer.OrdinalIgnoreCase);

    // Custom occupation fields
    private bool _isCustomOccupation;
    private string _customOccupationName = string.Empty;
    private int _customCreditRatingMin;
    private int _customCreditRatingMax = 50;
    private List<SkillPointFormula> _customFormulas = [new("EDU", 2), new("DEX", 2)];
    private bool _customOccupationConfirmed;
    private static readonly string[] AvailableCharacteristics = ["STR", "CON", "SIZ", "DEX", "APP", "INT", "POW", "EDU"];

    private bool CanConfirmCustomOccupation =>
        !string.IsNullOrWhiteSpace(_customOccupationName) &&
        _occupationSkillNames.Count == 8 &&
        _customCreditRatingMax >= _customCreditRatingMin &&
        _customFormulas.Count > 0;

    private IEnumerable<Skill> FilteredSkills =>
        (string.IsNullOrWhiteSpace(_skillFilter)
            ? Investigator.Skills
            : Investigator.Skills.Where(s => s.Name.Contains(_skillFilter, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(s => s.Name);

    private int OccupationSkillPoints => _selectedOccupation?.ComputeSkillPoints(Investigator) ?? 0;

    private string SkillPointsFormula =>
        _selectedOccupation is null
            ? string.Empty
            : string.Join(" + ", _selectedOccupation.SkillPointFormulas.Select(f => $"{f.Characteristic} × {f.Multiplier}"));

    protected override void OnParametersSet()
    {
        if (!_defaultsLoaded && Investigator.Skills.Count == 0)
        {
            PopulateDefaults();
            _defaultsLoaded = true;
        }

        // Restore selection if occupation was already set on investigator
        if (_selectedOccupation is null && !string.IsNullOrEmpty(Investigator.Occupation))
        {
            var match = Occupations.All.FirstOrDefault(
                o => o.Name.Equals(Investigator.Occupation, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                ApplyOccupation(match);
        }
    }

    private bool IsOccupationSkill(Skill skill) =>
        _occupationSkillNames.Contains(skill.Name);

    // ── Occupation selection ─────────────────────────

    private void OnOccupationSelected(Occupation? occupation)
    {
        if (occupation is null) return;
        ApplyOccupation(occupation);
    }

    private void ApplyOccupation(Occupation occupation)
    {
        _selectedOccupation = occupation;
        Investigator.Occupation = occupation.Name;
        _occupationSkillNames = new HashSet<string>(occupation.Skills, StringComparer.OrdinalIgnoreCase);
    }

    private void ClearOccupation()
    {
        _selectedOccupation = null;
        Investigator.Occupation = null;
        _occupationSkillNames.Clear();
    }

    // ── Custom occupation ────────────────────────────

    private void ToggleCustomOccupation()
    {
        _isCustomOccupation = !_isCustomOccupation;
        _customOccupationConfirmed = false;
        ClearOccupation();
        if (_isCustomOccupation)
        {
            _customOccupationName = string.Empty;
            _customCreditRatingMin = 0;
            _customCreditRatingMax = 50;
            _customFormulas = [new("EDU", 2), new("DEX", 2)];
            SyncCustomOccupation();
        }
    }

    private void SyncCustomOccupation()
    {
        if (!_isCustomOccupation) return;
        _selectedOccupation = new Occupation
        {
            Name = _customOccupationName,
            Skills = [.. _occupationSkillNames],
            CreditRatingMin = _customCreditRatingMin,
            CreditRatingMax = _customCreditRatingMax,
            SuggestedContacts = [],
            SkillPointFormulas = [.. _customFormulas]
        };
        Investigator.Occupation = _customOccupationName;
    }

    private void ToggleOccupationSkill(Skill skill)
    {
        if (!_isCustomOccupation || string.IsNullOrWhiteSpace(skill.Name)) return;

        if (!_occupationSkillNames.Remove(skill.Name))
        {
            if (_occupationSkillNames.Count < 8)
                _occupationSkillNames.Add(skill.Name);
        }
        SyncCustomOccupation();
    }

    private void OnCustomNameChanged(string value)
    {
        _customOccupationName = value;
        SyncCustomOccupation();
    }

    private void OnCustomCreditMinChanged(int value)
    {
        _customCreditRatingMin = value;
        SyncCustomOccupation();
    }

    private void OnCustomCreditMaxChanged(int value)
    {
        _customCreditRatingMax = value;
        SyncCustomOccupation();
    }

    private void OnFormulaCharacteristicChanged(int index, string value)
    {
        _customFormulas[index] = new(value, _customFormulas[index].Multiplier);
        SyncCustomOccupation();
    }

    private void OnFormulaMultiplierChanged(int index, int value)
    {
        _customFormulas[index] = new(_customFormulas[index].Characteristic, value);
        SyncCustomOccupation();
    }

    private void AddFormulaSlot()
    {
        _customFormulas.Add(new("EDU", 2));
        SyncCustomOccupation();
    }

    private void RemoveFormulaSlot(int index)
    {
        if (_customFormulas.Count > 1)
        {
            _customFormulas.RemoveAt(index);
            SyncCustomOccupation();
        }
    }

    private void ConfirmCustomOccupation()
    {
        if (!CanConfirmCustomOccupation) return;
        SyncCustomOccupation();
        _customOccupationConfirmed = true;
    }

    private void EditCustomOccupation()
    {
        _customOccupationConfirmed = false;
    }

    // ── Skill management ─────────────────────────────

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

    private static readonly (string Name, int BaseValue)[] DefaultSkills =
    [
        ("Accounting",               5),
        ("Anthropology",             1),
        ("Appraise",                 5),
        ("Archaeology",              1),
        ("Art/Craft",                5),
        ("Charm",                   15),
        ("Climb",                   20),
        ("Credit Rating",            0),
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
