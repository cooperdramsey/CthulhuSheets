namespace CthulhuSheets.Models;

public class Wealth
{
    // Decimal because the 1920s Penniless band is $0.50 cash / $0.50 spending
    // level (Table II). Existing saves hold whole-number JSON values, which
    // deserialize into decimal without loss.
    public decimal? SpendingLevel { get; set; }
    public decimal? Cash { get; set; }
    public List<string> Assets { get; set; } = [];
}
