namespace CthulhuSheets.Pages.Home.Components;

public partial class DiceFab : IDisposable
{
    [Inject] private DiceRollService DiceRollService { get; set; } = default!;

    private static readonly int[] DiceSides = [2, 4, 6, 10, 12, 20, 100];

    private bool _menuOpen;
    private readonly Dictionary<int, int> _selectedCounts = [];

    protected override void OnInitialized()
    {
        DiceRollService.OnRollHistoryChanged += StateHasChanged;
    }

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
        DiceRollService.RollMany(_selectedCounts.Select(kvp => (sides: kvp.Key, count: kvp.Value)));
        _selectedCounts.Clear();
        _menuOpen = false;
    }

    public void Dispose()
    {
        DiceRollService.OnRollHistoryChanged -= StateHasChanged;
    }
}
