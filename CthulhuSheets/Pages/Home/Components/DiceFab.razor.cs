namespace CthulhuSheets.Pages.Home.Components;

public partial class DiceFab : IDisposable
{
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private static readonly int[] DiceSides = [2, 4, 6, 10, 12, 20, 100];

    private bool _menuOpen;
    private readonly Dictionary<int, int> _selectedCounts = [];
    private int _bonusPenaltyDice;
    private readonly Dictionary<DiceGroup, CancellationTokenSource> _expiryTokens = [];
    private readonly HashSet<DiceGroup> _fadingGroups = [];

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
        DiceRollService.OnRollHistoryChanged += HandleHistoryChanged;
    }

    private void HandleHistoryChanged()
    {
        foreach (var group in DiceRollService.GroupHistory)
        {
            if (!_expiryTokens.ContainsKey(group))
                StartExpiry(group);
        }
        foreach (var key in _expiryTokens.Keys.ToList())
        {
            if (!DiceRollService.GroupHistory.Contains(key))
            {
                _expiryTokens[key].Cancel();
                _expiryTokens.Remove(key);
                _fadingGroups.Remove(key);
            }
        }
        InvokeAsync(StateHasChanged);
    }

    private void StartExpiry(DiceGroup group)
    {
        var cts = new CancellationTokenSource();
        _expiryTokens[group] = cts;
        _ = ExpireGroupAsync(group, cts.Token);
    }

    private async Task ExpireGroupAsync(DiceGroup group, CancellationToken ct)
    {
        try
        {
            await Task.Delay(9500, ct);
            _fadingGroups.Add(group);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(500, ct);
            DiceRollService.RemoveGroup(group);
        }
        catch (TaskCanceledException) { }
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
        DiceRollService.OnRollHistoryChanged -= HandleHistoryChanged;
        foreach (var cts in _expiryTokens.Values)
            cts.Cancel();
    }
}
