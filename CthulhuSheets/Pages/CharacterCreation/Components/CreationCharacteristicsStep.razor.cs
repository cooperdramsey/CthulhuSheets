using CthulhuSheets.Helpers;
using CthulhuSheets.Models;

namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationCharacteristicsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private DiceRollService Dice { get; set; } = default!;

    private record CharacteristicDef(Characteristic Stat, bool Is3d6, string Formula);

    private record AgeBracket(
        string Label,
        int EduChecks,
        int EduFlat,
        int PhysicalDeduction,
        string[] PhysicalTargets,
        int AppReduction,
        bool DoubleLuck);

    private CharacteristicDef[] _characteristicDefs = [];

    // ── Age state ────────────────────────────────────
    private int? _selectedAge;
    private AgeBracket? _currentBracket;
    private bool _ageApplied;
    private bool _deductionsPending;
    private int _deductionPool;
    private string[] _deductionTargets = [];
    private Dictionary<string, int> _deductions = new();
    private List<string> _ageLog = [];
    private Dictionary<string, int?> _baseValues = new();

    private bool AllStatsRolled => _characteristicDefs.All(d => d.Stat.Regular.HasValue);
    private bool DerivedReady => _ageApplied && !_deductionsPending;
    private int DeductionsRemaining => _deductionPool - _deductions.Values.Sum();

    protected override void OnParametersSet()
    {
        _characteristicDefs =
        [
            new(Investigator.Strength,     Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Constitution, Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Dexterity,    Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Appearance,   Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Power,        Is3d6: true,  Formula: "3D6 × 5"),
            new(Investigator.Size,         Is3d6: false, Formula: "(2D6+6) × 5"),
            new(Investigator.Intelligence, Is3d6: false, Formula: "(2D6+6) × 5"),
            new(Investigator.Education,    Is3d6: false, Formula: "(2D6+6) × 5"),
        ];
        _selectedAge = Investigator.Age;
    }

    // ── Rolling ──────────────────────────────────────

    private void RollAll()
    {
        foreach (var def in _characteristicDefs)
            Roll(def);
    }

    private void RollSingle(CharacteristicDef def) => Roll(def);

    private void Roll(CharacteristicDef def)
    {
        int raw = def.Is3d6
            ? Dice.Roll(6) + Dice.Roll(6) + Dice.Roll(6)
            : Dice.Roll(6) + Dice.Roll(6) + 6;
        def.Stat.Regular = raw * 5;
    }

    // ── Age helpers ──────────────────────────────────

    private void OnAgeChanged(int? value)
    {
        _selectedAge = value;
        _currentBracket = value.HasValue ? GetBracket(value.Value) : null;
    }

    private static AgeBracket? GetBracket(int age) => age switch
    {
        >= 15 and <= 19 => new("15–19", EduChecks: 0, EduFlat: -5, PhysicalDeduction: 5, ["STR", "SIZ"], AppReduction: 0, DoubleLuck: true),
        >= 20 and <= 39 => new("20–39", EduChecks: 1, EduFlat: 0, PhysicalDeduction: 0, [], AppReduction: 0, DoubleLuck: false),
        >= 40 and <= 49 => new("40–49", EduChecks: 2, EduFlat: 0, PhysicalDeduction: 5, ["STR", "CON", "DEX"], AppReduction: 5, DoubleLuck: false),
        >= 50 and <= 59 => new("50–59", EduChecks: 3, EduFlat: 0, PhysicalDeduction: 10, ["STR", "CON", "DEX"], AppReduction: 10, DoubleLuck: false),
        >= 60 and <= 69 => new("60–69", EduChecks: 4, EduFlat: 0, PhysicalDeduction: 20, ["STR", "CON", "DEX"], AppReduction: 15, DoubleLuck: false),
        >= 70 and <= 79 => new("70–79", EduChecks: 4, EduFlat: 0, PhysicalDeduction: 40, ["STR", "CON", "DEX"], AppReduction: 20, DoubleLuck: false),
        >= 80 and <= 90 => new("80–90", EduChecks: 4, EduFlat: 0, PhysicalDeduction: 80, ["STR", "CON", "DEX"], AppReduction: 25, DoubleLuck: false),
        _ => null
    };

    private Characteristic GetCharacteristicByName(string name) => name switch
    {
        "STR" => Investigator.Strength,
        "CON" => Investigator.Constitution,
        "SIZ" => Investigator.Size,
        "DEX" => Investigator.Dexterity,
        "APP" => Investigator.Appearance,
        "INT" => Investigator.Intelligence,
        "POW" => Investigator.Power,
        "EDU" => Investigator.Education,
        _ => throw new ArgumentException($"Unknown characteristic: {name}")
    };

    // ── Apply / reset ────────────────────────────────

    private void StoreBaseValues()
    {
        _baseValues = new()
        {
            ["STR"] = Investigator.Strength.Regular,
            ["CON"] = Investigator.Constitution.Regular,
            ["SIZ"] = Investigator.Size.Regular,
            ["DEX"] = Investigator.Dexterity.Regular,
            ["APP"] = Investigator.Appearance.Regular,
            ["INT"] = Investigator.Intelligence.Regular,
            ["POW"] = Investigator.Power.Regular,
            ["EDU"] = Investigator.Education.Regular,
        };
    }

    private void RestoreBaseValues()
    {
        foreach (var (name, value) in _baseValues)
            GetCharacteristicByName(name).Regular = value;
    }

    private void ApplyAgeModifiers()
    {
        if (_currentBracket is null || _ageApplied) return;

        StoreBaseValues();
        _ageLog.Clear();
        Investigator.Age = _selectedAge;
        var bracket = _currentBracket;

        if (bracket.EduFlat != 0)
        {
            var before = Investigator.Education.Regular ?? 0;
            Investigator.Education.Regular = Math.Max(0, before + bracket.EduFlat);
            _ageLog.Add($"EDU reduced by {Math.Abs(bracket.EduFlat)}: {before} → {Investigator.Education.Regular}");
        }

        for (int i = 0; i < bracket.EduChecks; i++)
        {
            var before = Investigator.Education.Regular ?? 0;
            var (improved, roll, added) = CharacteristicHelper.TryImproveEducation(Investigator.Education, Dice);
            if (improved)
                _ageLog.Add($"EDU check {i + 1}: rolled {roll} vs {before} → +{added}, EDU now {Investigator.Education.Regular}");
            else
                _ageLog.Add($"EDU check {i + 1}: rolled {roll} vs {before} → no improvement");
        }

        if (bracket.AppReduction > 0)
        {
            var before = Investigator.Appearance.Regular ?? 0;
            Investigator.Appearance.Regular = Math.Max(0, before - bracket.AppReduction);
            _ageLog.Add($"APP reduced by {bracket.AppReduction}: {before} → {Investigator.Appearance.Regular}");
        }

        int luck = CharacteristicHelper.RollLuck(Dice);

        if (bracket.DoubleLuck)
        {
            int luck2 = CharacteristicHelper.RollLuck(Dice);
            int best = Math.Max(luck, luck2);
            _ageLog.Add($"Luck: rolled {luck} and {luck2}, using higher value {best}");
            luck = best;
        }
        else
        {
            _ageLog.Add($"Luck: rolled {luck}");
        }

        Investigator.Luck = new Luck { Current = luck, Starting = luck };

        _ageApplied = true;

        if (bracket.PhysicalDeduction > 0 && bracket.PhysicalTargets.Length > 0)
        {
            _deductionPool = bracket.PhysicalDeduction;
            _deductionTargets = bracket.PhysicalTargets;
            _deductions = bracket.PhysicalTargets.ToDictionary(t => t, _ => 0);
            _deductionsPending = true;
        }
        else
        {
            ComputeDerivedAttributes();
        }
    }

    // ── Deductions ───────────────────────────────────

    private void OnDeductionChanged(string target, int value)
    {
        _deductions[target] = Math.Max(0, value);
    }

    private void ConfirmDeductions()
    {
        if (DeductionsRemaining != 0) return;

        foreach (var (name, amount) in _deductions)
        {
            if (amount <= 0) continue;
            var stat = GetCharacteristicByName(name);
            var before = stat.Regular ?? 0;
            stat.Regular = Math.Max(0, before - amount);
            _ageLog.Add($"{name} reduced by {amount}: {before} → {stat.Regular}");
        }

        _deductionsPending = false;
        ComputeDerivedAttributes();
    }

    private void ResetAgeModifiers()
    {
        RestoreBaseValues();
        Investigator.Age = null;
        Investigator.Luck = new Luck();
        Investigator.Sanity = new Sanity();
        Investigator.MagicPoints = new MagicPoints();
        Investigator.HitPoints = new HitPoints();
        _ageApplied = false;
        _deductionsPending = false;
        _ageLog.Clear();
        _currentBracket = _selectedAge.HasValue ? GetBracket(_selectedAge.Value) : null;
    }

    // ── Derived attributes ────────────────────────────

    private void ComputeDerivedAttributes()
    {
        var pow = Investigator.Power.Regular;
        var siz = Investigator.Size.Regular;
        var con = Investigator.Constitution.Regular;

        if (pow.HasValue)
        {
            Investigator.Sanity = new Sanity
            {
                Starting = pow.Value,
                Current = pow.Value,
                Max = 99
            };

            Investigator.MagicPoints = new MagicPoints
            {
                Current = pow.Value / 5,
                Max = pow.Value / 5
            };
        }

        if (siz.HasValue && con.HasValue)
        {
            var hp = (siz.Value + con.Value) / 10;
            Investigator.HitPoints = new HitPoints
            {
                Current = hp,
                Max = hp
            };
        }
    }
}
