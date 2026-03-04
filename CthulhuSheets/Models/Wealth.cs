namespace CthulhuSheets.Models;

public class Wealth
{
    public int? SpendingLevel { get; set; }
    public int? Cash { get; set; }
    public List<string> Assets { get; set; } = [];
}
