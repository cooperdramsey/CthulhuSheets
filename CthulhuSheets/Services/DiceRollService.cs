namespace CthulhuSheets.Services;

public record DiceGroup(int Total, string Expression, DateTime RolledAt);

public class DiceRollService
{
    private const int MaxGroupHistory = 5;

    private readonly List<DiceGroup> _groupHistory = [];
    private readonly Random _random = new();

    public IReadOnlyList<DiceGroup> GroupHistory => _groupHistory.AsReadOnly();

    public event Action? OnRollHistoryChanged;

    public DiceGroup RollMany(IEnumerable<(int sides, int count)> requests)
    {
        var requestList = requests.ToList();
        var total = 0;

        foreach (var (sides, count) in requestList)
            for (var i = 0; i < count; i++)
                total += _random.Next(1, sides + 1);

        var expression = string.Join(" + ", requestList.Select(r => $"{r.count}d{r.sides}"));
        var group = new DiceGroup(total, expression, DateTime.Now);

        _groupHistory.Add(group);
        if (_groupHistory.Count > MaxGroupHistory)
            _groupHistory.RemoveAt(0);

        OnRollHistoryChanged?.Invoke();
        return group;
    }

    public void ClearHistory()
    {
        _groupHistory.Clear();
        OnRollHistoryChanged?.Invoke();
    }
}
