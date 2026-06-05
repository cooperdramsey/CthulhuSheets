namespace CthulhuSheets.Services;

public record DiceGroup(int Total, string Expression, DateTime RolledAt);

public class DiceRollService
{
    private const int MaxGroupHistory = 5;

    private readonly List<DiceGroup> _groupHistory = [];
    private readonly Random _random = new();

    public IReadOnlyList<DiceGroup> GroupHistory => _groupHistory.AsReadOnly();

    public event Action? OnRollHistoryChanged;

    /// <summary>
    /// Rolls d100 with optional bonus/penalty dice (-2 to +2).
    /// Bonus: roll extra dice, take the lowest result. Penalty: take the highest.
    /// </summary>
    public DiceGroup RollPercentile(int bonusPenalty = 0)
    {
        bonusPenalty = Math.Clamp(bonusPenalty, -2, 2);
        var diceCount = Math.Abs(bonusPenalty) + 1;
        var rolls = Enumerable.Range(0, diceCount).Select(_ => Roll(100)).ToList();

        int result;
        string expression;

        if (bonusPenalty == 0)
        {
            result = rolls[0];
            expression = "1d100";
        }
        else if (bonusPenalty > 0)
        {
            result = rolls.Min();
            var tag = bonusPenalty == 1 ? "+bonus" : "+2bonus";
            expression = $"1d100 {tag} ({string.Join(", ", rolls)})";
        }
        else
        {
            result = rolls.Max();
            var tag = bonusPenalty == -1 ? "-penalty" : "-2penalty";
            expression = $"1d100 {tag} ({string.Join(", ", rolls)})";
        }

        return AddToHistory(result, expression);
    }

    public DiceGroup RollMany(IEnumerable<(int sides, int count)> requests)
    {
        var requestList = requests.ToList();
        var total = 0;

        foreach (var (sides, count) in requestList)
            for (var i = 0; i < count; i++)
                total += Roll(sides);

        var expression = string.Join(" + ", requestList.Select(r => $"{r.count}d{r.sides}"));
        return AddToHistory(total, expression);
    }

    public int Roll(int sides) => _random.Next(1, sides + 1);

    public void RemoveGroup(DiceGroup group)
    {
        _groupHistory.Remove(group);
        OnRollHistoryChanged?.Invoke();
    }

    public void ClearHistory()
    {
        _groupHistory.Clear();
        OnRollHistoryChanged?.Invoke();
    }

    private DiceGroup AddToHistory(int total, string expression)
    {
        var group = new DiceGroup(total, expression, DateTime.Now);
        _groupHistory.Add(group);
        if (_groupHistory.Count > MaxGroupHistory)
            _groupHistory.RemoveAt(0);
        OnRollHistoryChanged?.Invoke();
        return group;
    }
}
