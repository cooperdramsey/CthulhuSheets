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
    /// Rolls d100 with optional bonus/penalty dice (-2 to +2), per CoC 7e:
    /// roll one units die and (1 + extra) tens dice that share that units digit.
    /// Bonus dice keep the lowest final result; penalty dice keep the highest.
    /// A tens of 00 with units 0 reads as 100.
    /// </summary>
    public DiceGroup RollPercentile(int bonusPenalty = 0)
    {
        bonusPenalty = Math.Clamp(bonusPenalty, -2, 2);

        var units = _random.Next(0, 10);          // 0–9, shared across all tens dice
        var tensDiceCount = Math.Abs(bonusPenalty) + 1;
        var candidates = Enumerable.Range(0, tensDiceCount)
            .Select(_ => Combine(_random.Next(0, 10), units))
            .ToList();

        int result;
        string expression;

        if (bonusPenalty == 0)
        {
            result = candidates[0];
            expression = "1d100";
        }
        else if (bonusPenalty > 0)
        {
            result = candidates.Min();
            var tag = bonusPenalty == 1 ? "+bonus" : "+2bonus";
            expression = $"1d100 {tag} ({string.Join(", ", candidates)})";
        }
        else
        {
            result = candidates.Max();
            var tag = bonusPenalty == -1 ? "-penalty" : "-2penalty";
            expression = $"1d100 {tag} ({string.Join(", ", candidates)})";
        }

        return AddToHistory(result, expression);

        static int Combine(int tensDigit, int unitsDigit)
        {
            var value = tensDigit * 10 + unitsDigit;
            return value == 0 ? 100 : value;
        }
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
