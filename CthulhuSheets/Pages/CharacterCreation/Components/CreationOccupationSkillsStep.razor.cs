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

    // Allocation state
    private Dictionary<string, int> _allocations = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _personalAllocations = new(StringComparer.OrdinalIgnoreCase);

    // The 75% starting-skill cap is a Keeper option (ch_3), on by default.
    private const int StartingSkillCap = 75;
    private bool _capSkillsAt75 = true;

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

    // ── Allocation properties ────────────────────────

    private bool IsOccupationConfirmed =>
        _selectedOccupation is not null && (!_isCustomOccupation || _customOccupationConfirmed);

    private int PointsAllocated => _allocations.Values.Sum();
    private int PointsRemaining => OccupationSkillPoints - PointsAllocated;

    private int CreditRatingGrandTotal =>
        GetSkillBase("Credit Rating") + _allocations.GetValueOrDefault("Credit Rating") + _personalAllocations.GetValueOrDefault("Credit Rating");

    private int PersonalInterestPoints => (Investigator.Intelligence.Regular ?? 0) * 2;
    private int PersonalPointsAllocated => _personalAllocations.Values.Sum();
    private int PersonalPointsRemaining => PersonalInterestPoints - PersonalPointsAllocated;

    private int GetSkillCurrentValue(string skillName) =>
        GetSkillBase(skillName) + _allocations.GetValueOrDefault(skillName);

    private int GetSkillGrandTotal(string skillName) =>
        GetSkillBase(skillName) + _allocations.GetValueOrDefault(skillName) + _personalAllocations.GetValueOrDefault(skillName);

    private bool IsAllocationValid =>
        PointsRemaining == 0 &&
        PersonalPointsRemaining == 0 &&
        CreditRatingGrandTotal >= (_selectedOccupation?.CreditRatingMin ?? 0) &&
        CreditRatingGrandTotal <= (_selectedOccupation?.CreditRatingMax ?? 99);

    public bool Validate()
    {
        if (!(IsOccupationConfirmed && IsAllocationValid))
            return false;

        FinalizeSkills();
        return true;
    }

    private void FinalizeSkills()
    {
        foreach (var skill in Investigator.Skills)
        {
            var occ = _allocations.GetValueOrDefault(skill.Name);
            var personal = _personalAllocations.GetValueOrDefault(skill.Name);
            skill.Regular = skill.BaseValue + occ + personal;
            skill.IsOccupationSkill = _occupationSkillNames.Contains(skill.Name);
        }
    }

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
                ApplyOccupation(match, prefillContacts: false);
        }
    }

    private bool IsOccupationSkill(Skill skill) =>
        _occupationSkillNames.Contains(skill.Name);

    // ── Occupation selection ─────────────────────────

    private void OnOccupationSelected(Occupation? occupation)
    {
        if (occupation is null) return;
        ApplyOccupation(occupation, prefillContacts: true);
    }

    private void ApplyOccupation(Occupation occupation, bool prefillContacts)
    {
        _selectedOccupation = occupation;
        Investigator.Occupation = occupation.Name;
        _occupationSkillNames = new HashSet<string>(occupation.Skills, StringComparer.OrdinalIgnoreCase);

        // Seed the freeform Contacts field with the occupation's suggested contacts.
        // Only when the user actively picks an occupation, never when restoring a draft,
        // so we don't clobber edits the player has already made.
        if (prefillContacts && occupation.SuggestedContacts.Length > 0)
            Investigator.Contacts = string.Join(", ", occupation.SuggestedContacts);

        InitializeAllocations();
        InitializePersonalAllocations();
    }

    private void ClearOccupation()
    {
        _selectedOccupation = null;
        Investigator.Occupation = null;
        _occupationSkillNames.Clear();
        _allocations.Clear();
        _personalAllocations.Clear();
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
        InitializeAllocations();
        InitializePersonalAllocations();
    }

    private void EditCustomOccupation()
    {
        _customOccupationConfirmed = false;
        _allocations.Clear();
        _personalAllocations.Clear();
    }

    // ── Allocation management ────────────────────────

    private void InitializeAllocations()
    {
        _allocations = new(StringComparer.OrdinalIgnoreCase) { ["Credit Rating"] = 0 };
        foreach (var name in _occupationSkillNames)
        {
            if (name.Equals("Cthulhu Mythos", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase)) continue;
            _allocations[name] = 0;
        }
    }

    private void OnAllocationChanged(string skillName, int value)
    {
        _allocations[skillName] = Math.Max(0, value);
    }

    private void OnCapToggled(bool enabled)
    {
        _capSkillsAt75 = enabled;
        if (!enabled) return;

        // Re-clamp any allocation whose grand total now exceeds the cap.
        foreach (var name in _allocations.Keys.ToList())
        {
            if (name.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase)) continue;
            var ceiling = StartingSkillCap - GetSkillBase(name) - _personalAllocations.GetValueOrDefault(name);
            if (_allocations[name] > ceiling)
                _allocations[name] = Math.Max(0, ceiling);
        }
        foreach (var name in _personalAllocations.Keys.ToList())
        {
            if (name.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase)) continue;
            var ceiling = StartingSkillCap - GetSkillBase(name) - _allocations.GetValueOrDefault(name);
            if (_personalAllocations[name] > ceiling)
                _personalAllocations[name] = Math.Max(0, ceiling);
        }
    }

    private int GetSkillBase(string skillName)
    {
        var skill = Investigator.Skills.FirstOrDefault(
            s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
        return skill?.BaseValue ?? 0;
    }

    private int GetMaxAllocation(string skillName)
    {
        var currentAlloc = _allocations.GetValueOrDefault(skillName);
        var availablePool = currentAlloc + PointsRemaining;
        var personalAlloc = _personalAllocations.GetValueOrDefault(skillName);
        var baseVal = GetSkillBase(skillName);

        if (skillName.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase))
        {
            var crMax = (_selectedOccupation?.CreditRatingMax ?? 99) - baseVal - personalAlloc;
            return Math.Max(0, Math.Min(availablePool, crMax));
        }

        if (!_capSkillsAt75)
            return Math.Max(0, availablePool);

        var skillCap = StartingSkillCap - baseVal - personalAlloc;
        return Math.Max(0, Math.Min(availablePool, skillCap));
    }

    // ── Personal interest allocation ────────────────

    private void InitializePersonalAllocations()
    {
        _personalAllocations = new(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in Investigator.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name)) continue;
            if (skill.Name.Equals("Cthulhu Mythos", StringComparison.OrdinalIgnoreCase)) continue;
            _personalAllocations[skill.Name] = 0;
        }
    }

    private void OnPersonalAllocationChanged(string skillName, int value)
    {
        _personalAllocations[skillName] = Math.Max(0, value);
    }

    private int GetMaxPersonalAllocation(string skillName)
    {
        var currentAlloc = _personalAllocations.GetValueOrDefault(skillName);
        var availablePool = currentAlloc + PersonalPointsRemaining;
        var occAlloc = _allocations.GetValueOrDefault(skillName);
        var baseVal = GetSkillBase(skillName);

        if (skillName.Equals("Credit Rating", StringComparison.OrdinalIgnoreCase))
        {
            var crMax = (_selectedOccupation?.CreditRatingMax ?? 99) - baseVal - occAlloc;
            return Math.Max(0, Math.Min(availablePool, crMax));
        }

        if (!_capSkillsAt75)
            return Math.Max(0, availablePool);

        var skillCap = StartingSkillCap - baseVal - occAlloc;
        return Math.Max(0, Math.Min(availablePool, skillCap));
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

        foreach (var (name, baseVal) in DefaultSkills.All)
        {
            if (existing.Contains(name)) continue;

            var computedBase = DefaultSkills.ComputeBase(name, baseVal, Investigator);
            Investigator.Skills.Add(new Skill { Name = name, BaseValue = computedBase, IsDefault = true });
        }
    }
}
