using CthulhuSheets.Helpers;
using CthulhuSheets.Models;

namespace CthulhuSheets.Pages.CharacterCreation.Components;

public partial class CreationCharacteristicsStep
{
    [Parameter, EditorRequired]
    public Investigator Investigator { get; set; } = default!;

    [Inject] private DiceRollService Dice { get; set; } = default!;

    private enum CreationMethod { Roll, PlaceRolls, PointBuy, QuickFire }

    private record CharacteristicDef(Characteristic Stat, bool Is3d6, string Formula, string FullName);

    private record AgeBracket(
        string Label,
        int EduChecks,
        int EduFlat,
        int PhysicalDeduction,
        string[] PhysicalTargets,
        int AppReduction,
        bool DoubleLuck);

    private CharacteristicDef[] _characteristicDefs = [];

    private IEnumerable<string> StatNames => _characteristicDefs.Select(d => d.Stat.Name);

    // ── Generation method ────────────────────────────
    private CreationMethod _method = CreationMethod.Roll;

    // ── Assignment dice (Place Rolls / Quick Fire) ───
    private const string PoolZone = "POOL";

    private sealed class PoolDie
    {
        public int Value { get; init; }
        public string Zone { get; set; } = PoolZone;
    }

    private List<PoolDie> _dice = [];
    private PoolDie? _draggedDie;

    private bool IsAssignmentMethod => _method is CreationMethod.PlaceRolls or CreationMethod.QuickFire;

    private PoolDie? AssignedDie(string statName) => _dice.FirstOrDefault(d => d.Zone == statName);
    private IEnumerable<PoolDie> PooledDice => _dice.Where(d => d.Zone == PoolZone).OrderBy(d => d.Value);

    // ── Modify Low Rolls (Roll add-on) ───────────────
    private bool _lowRollPending;
    private int _lowRollPool;
    private string[] _lowRollTargets = [];
    private Dictionary<string, int> _lowRollAlloc = new();

    // ── Human Potential (combinable add-on) ──────────
    private bool _humanPotentialEnabled;
    private bool _humanPotentialApplied;
    private bool _hpRolled;
    private int _hpPool;
    private Dictionary<string, int> _hpAlloc = new();
    private Dictionary<string, int?> _hpBaseValues = new();

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

    // Base values are produced once the active method is satisfied (Point Buy also
    // requires the 460-point budget to balance). Human Potential, when enabled,
    // must then be distributed before age can be applied.
    private bool BaseStatsReady => _method switch
    {
        CreationMethod.PointBuy => AllStatsRolled && PointBuyValid,
        _ => AllStatsRolled
    };

    private bool StatsReady => BaseStatsReady && (!_humanPotentialEnabled || _humanPotentialApplied);

    // While locked, the base-method inputs are frozen (an add-on or age has been
    // applied on top of them and must be reset before the base can change again).
    private bool BaseLocked => _ageApplied || _humanPotentialApplied;

    private bool DerivedReady => _ageApplied && !_deductionsPending;
    private int DeductionsRemaining => _deductionPool - _deductions.Values.Sum();

    public bool Validate() => DerivedReady;

    protected override void OnParametersSet()
    {
        _characteristicDefs =
        [
            new(Investigator.Strength,     Is3d6: true,  Formula: "3D6 × 5",   FullName: "Strength"),
            new(Investigator.Constitution, Is3d6: true,  Formula: "3D6 × 5",   FullName: "Constitution"),
            new(Investigator.Dexterity,    Is3d6: true,  Formula: "3D6 × 5",   FullName: "Dexterity"),
            new(Investigator.Appearance,   Is3d6: true,  Formula: "3D6 × 5",   FullName: "Appearance"),
            new(Investigator.Power,        Is3d6: true,  Formula: "3D6 × 5",   FullName: "Power"),
            new(Investigator.Size,         Is3d6: false, Formula: "(2D6+6) × 5", FullName: "Size"),
            new(Investigator.Intelligence, Is3d6: false, Formula: "(2D6+6) × 5", FullName: "Intelligence"),
            new(Investigator.Education,    Is3d6: false, Formula: "(2D6+6) × 5", FullName: "Education"),
        ];
        _selectedAge = Investigator.Age;
        // Recompute bracket so the age-gate renders correctly if the component is
        // recreated mid-flow (e.g. the user navigates away and back in the stepper).
        _currentBracket = _selectedAge.HasValue ? GetBracket(_selectedAge.Value) : null;
    }

    private static IEnumerable<AgeBracket> AllBrackets =>
        new[] { 15, 20, 40, 50, 60, 70, 80 }.Select(a => GetBracket(a)!);

    // ── Method switching ─────────────────────────────

    private void ChangeMethod(CreationMethod method)
    {
        if (BaseLocked || method == _method) return;

        _method = method;
        ResetHumanPotential();
        CancelModifyLowRolls();
        ClearAllStats();

        // Quick Fire seeds its fixed array into the pool; Place Rolls starts empty
        // until the player rolls; the other methods need no dice.
        _dice = method == CreationMethod.QuickFire
            ? CharacteristicHelper.QuickFireArray.Select(v => new PoolDie { Value = v }).ToList()
            : [];
        _draggedDie = null;
    }

    private void ClearAllStats()
    {
        foreach (var def in _characteristicDefs)
            def.Stat.Regular = null;
    }

    // ── Roll method ──────────────────────────────────

    private void RollAll()
    {
        foreach (var def in _characteristicDefs)
            Roll(def);
        CancelModifyLowRolls();
    }

    private void RollSingle(CharacteristicDef def) => Roll(def);

    private void Roll(CharacteristicDef def)
    {
        int raw = def.Is3d6
            ? Dice.Roll(6) + Dice.Roll(6) + Dice.Roll(6)
            : Dice.Roll(6) + Dice.Roll(6) + 6;
        def.Stat.Regular = raw * 5;
    }

    // Start Over advisory: Keeper may allow a full reroll when 3+ characteristics
    // start below 50.
    private int WeakStatCount => _characteristicDefs.Count(d => d.Stat.Regular is int v && v < 50);
    private bool CanStartOver => _method == CreationMethod.Roll && !BaseLocked && AllStatsRolled && WeakStatCount >= 3;

    // ── Modify Low Rolls (add-on to Roll) ────────────
    // A "low roll" is a pre-×5 dice total under 10 (i.e. Regular / 5 < 10).

    private int LowRollCount => _characteristicDefs.Count(d => d.Stat.Regular is int v && v / 5 < 10);
    private bool CanModifyLowRolls =>
        _method == CreationMethod.Roll && !BaseLocked && AllStatsRolled && LowRollCount >= 3 && !_lowRollPending;
    private int LowRollRemaining => _lowRollPool - _lowRollAlloc.Values.Sum();

    private void StartModifyLowRolls()
    {
        if (!CanModifyLowRolls) return;
        _lowRollPool = Dice.Roll(6);
        _lowRollTargets = _characteristicDefs
            .Where(d => d.Stat.Regular is int v && v / 5 < 10)
            .Select(d => d.Stat.Name)
            .ToArray();
        _lowRollAlloc = _lowRollTargets.ToDictionary(n => n, _ => 0);
        _lowRollPending = true;
    }

    private void OnLowRollAllocChanged(string name, int value)
    {
        var stat = GetCharacteristicByName(name);
        // Each point adds +5 after the ×5; cap at 90 (the 18 × 5 ceiling).
        var maxPoints = (90 - (stat.Regular ?? 0)) / 5;
        _lowRollAlloc[name] = Math.Clamp(value, 0, Math.Max(0, maxPoints));
    }

    private void ConfirmModifyLowRolls()
    {
        if (LowRollRemaining != 0) return;
        foreach (var (name, points) in _lowRollAlloc)
        {
            if (points <= 0) continue;
            var stat = GetCharacteristicByName(name);
            stat.Regular = Math.Min(90, (stat.Regular ?? 0) + points * 5);
        }
        CancelModifyLowRolls();
    }

    private void CancelModifyLowRolls()
    {
        _lowRollPending = false;
        _lowRollPool = 0;
        _lowRollTargets = [];
        _lowRollAlloc = new();
    }

    // ── Place Rolls / Quick Fire assignment (drag & drop) ──

    private void RollPlacePool()
    {
        _dice = CharacteristicHelper.RollPlacePool(Dice)
            .Select(v => new PoolDie { Value = v })
            .ToList();
        _draggedDie = null;
        SyncAssignmentsFromDice();
    }

    private void ResetDiceToPool()
    {
        foreach (var die in _dice)
            die.Zone = PoolZone;
        _draggedDie = null;
        SyncAssignmentsFromDice();
    }

    private void OnDragStart(PoolDie die)
    {
        if (!BaseLocked) _draggedDie = die;
    }

    private void DropOnZone(string zone)
    {
        if (BaseLocked || _draggedDie is null) return;

        var origin = _draggedDie.Zone;

        if (zone != PoolZone)
        {
            var target = _dice.FirstOrDefault(d => d.Zone == zone && !ReferenceEquals(d, _draggedDie));
            if (target is not null)
            {
                // Stat→stat: swap. Pool→stat: bump displaced die to pool.
                target.Zone = (origin != PoolZone && origin != zone) ? origin : PoolZone;
            }
        }

        _draggedDie.Zone = zone;
        _draggedDie = null;
        SyncAssignmentsFromDice();
    }

    private void SyncAssignmentsFromDice()
    {
        foreach (var def in _characteristicDefs)
        {
            var die = _dice.FirstOrDefault(d => d.Zone == def.Stat.Name);
            def.Stat.Regular = die?.Value;
        }
    }

    // ── Point Buy ────────────────────────────────────

    private int PointBuySum => _characteristicDefs.Sum(d => d.Stat.Regular ?? 0);
    private int PointBuyRemaining => CharacteristicHelper.PointBuyTotal - PointBuySum;
    private bool PointBuyValid =>
        PointBuyRemaining == 0 &&
        _characteristicDefs.All(d =>
            d.Stat.Regular is int v && v >= CharacteristicHelper.PointBuyMin && v <= CharacteristicHelper.PointBuyMax);

    private void OnPointBuyChanged(Characteristic stat, int? value) => stat.Regular = value;

    // ── Human Potential (combinable add-on) ──────────

    private int HumanPotentialRemaining => _hpPool - _hpAlloc.Values.Sum();

    private void OnHumanPotentialToggled(bool enabled)
    {
        if (_ageApplied) return;
        _humanPotentialEnabled = enabled;
        if (!enabled) ResetHumanPotential();
    }

    private void RollHumanPotential()
    {
        if (!BaseStatsReady || _humanPotentialApplied) return;
        _hpPool = Dice.Roll(10);
        _hpAlloc = StatNames.ToDictionary(n => n, _ => 0);
        _hpRolled = true;
    }

    private void OnHpAllocChanged(string name, int value)
    {
        var stat = GetCharacteristicByName(name);
        var headroom = CharacteristicHelper.HumanPotentialMax - (stat.Regular ?? 0);
        _hpAlloc[name] = Math.Clamp(value, 0, Math.Max(0, headroom));
    }

    private void ConfirmHumanPotential()
    {
        if (!_hpRolled || HumanPotentialRemaining != 0) return;
        StoreHpBaseValues();
        foreach (var (name, amount) in _hpAlloc)
        {
            if (amount <= 0) continue;
            var stat = GetCharacteristicByName(name);
            stat.Regular = Math.Min(CharacteristicHelper.HumanPotentialMax, (stat.Regular ?? 0) + amount);
        }
        _humanPotentialApplied = true;
    }

    private void ResetHumanPotential()
    {
        if (_humanPotentialApplied)
            RestoreHpBaseValues();
        _humanPotentialApplied = false;
        _hpRolled = false;
        _hpPool = 0;
        _hpAlloc = new();
    }

    private void StoreHpBaseValues() =>
        _hpBaseValues = StatNames.ToDictionary(n => n, n => GetCharacteristicByName(n).Regular);

    private void RestoreHpBaseValues()
    {
        foreach (var (name, value) in _hpBaseValues)
            GetCharacteristicByName(name).Regular = value;
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
        Investigator.MovementRate = null;
        Investigator.Build = null;
        Investigator.DamageBonus = null;
        _ageApplied = false;
        _deductionsPending = false;
        _ageLog.Clear();
        _currentBracket = _selectedAge.HasValue ? GetBracket(_selectedAge.Value) : null;
    }

    // ── Derived attributes ────────────────────────────

    private void ComputeDerivedAttributes()
    {
        // Pools are freshly reset before this runs (current values unset), so the
        // shared helper seeds full pools and computes the derived stats.
        CharacteristicHelper.RecomputeDerived(Investigator);
    }
}
