namespace CthulhuSheets.Services;

public record DiceRoll(int Sides, int Result, DateTime RolledAt);

public class DiceRollService
{
    private readonly List<DiceRoll> _history = [];
    private readonly Random _random = new();

    public IReadOnlyList<DiceRoll> History => _history.AsReadOnly();

    public event Action? OnRollHistoryChanged;

    public DiceRoll Roll(int sides)
    {
        var roll = new DiceRoll(sides, _random.Next(1, sides + 1), DateTime.Now);
        _history.Insert(0, roll);
        OnRollHistoryChanged?.Invoke();
        return roll;
    }

    public IReadOnlyList<DiceRoll> RollMany(IEnumerable<(int sides, int count)> requests)
    {
        var rolls = new List<DiceRoll>();
        foreach (var (sides, count) in requests)
        {
            for (var i = 0; i < count; i++)
            {
                var roll = new DiceRoll(sides, _random.Next(1, sides + 1), DateTime.Now);
                _history.Insert(0, roll);
                rolls.Add(roll);
            }
        }
        OnRollHistoryChanged?.Invoke();
        return rolls;
    }

    public void ClearHistory()
    {
        _history.Clear();
        OnRollHistoryChanged?.Invoke();
    }
}
