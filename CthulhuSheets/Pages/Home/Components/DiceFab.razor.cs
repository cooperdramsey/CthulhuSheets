namespace CthulhuSheets.Pages.Home.Components;

public partial class DiceFab : IDisposable
{
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private static readonly int[] DiceSides = [2, 4, 6, 10, 12, 20, 100];

    private bool _menuOpen;
    private readonly Dictionary<int, int> _selectedCounts = [];
    private int _bonusPenaltyDice;

    private string BonusPenaltyLabel => _bonusPenaltyDice switch
    {
        2  => "+2 Bonus",
        1  => "+1 Bonus",
        0  => "Normal",
        -1 => "-1 Penalty",
        _  => "-2 Penalty"
    };

    private string BonusPenaltyClass => _bonusPenaltyDice switch
    {
        > 0 => "bonus-penalty-chip--bonus",
        < 0 => "bonus-penalty-chip--penalty",
        _   => "bonus-penalty-chip--normal"
    };

    protected override void OnInitialized()
    {
        DiceRollService.OnRollHistoryChanged += StateHasChanged;
    }

    private void DecrementBonusPenalty() => _bonusPenaltyDice = Math.Max(-2, _bonusPenaltyDice - 1);
    private void IncrementBonusPenalty() => _bonusPenaltyDice = Math.Min(2, _bonusPenaltyDice + 1);

    private void OnFabClick()
    {
        if (!_menuOpen)
        {
            _menuOpen = true;
            return;
        }
        if (_selectedCounts.Count > 0)
            RollSelection();
        else
            _menuOpen = false;
    }

    private void AddDie(int sides) =>
        _selectedCounts[sides] = _selectedCounts.GetValueOrDefault(sides) + 1;

    private void RemoveDie(int sides)
    {
        if (!_selectedCounts.TryGetValue(sides, out var count)) return;
        if (count <= 1) _selectedCounts.Remove(sides);
        else _selectedCounts[sides] = count - 1;
    }

    private void RollSelection()
    {
        // Apply bonus/penalty when selection is purely d100 dice
        if (_bonusPenaltyDice != 0 && _selectedCounts.Count == 1 && _selectedCounts.ContainsKey(100))
        {
            var d100Count = _selectedCounts[100];
            for (var i = 0; i < d100Count; i++)
                DiceRollService.RollPercentile(_bonusPenaltyDice);
        }
        else
        {
            DiceRollService.RollMany(_selectedCounts.Select(kvp => (sides: kvp.Key, count: kvp.Value)));
        }
        _selectedCounts.Clear();
        _menuOpen = false;
    }

    public void Dispose()
    {
        DiceRollService.OnRollHistoryChanged -= StateHasChanged;
    }
}
