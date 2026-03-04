using System.Text.Json.Serialization;

namespace CthulhuSheets.Models;

public class Sanity
{
    public int? Starting { get; set; }
    public int? Current { get; set; }
    public int? Max { get; set; }

    [JsonIgnore]
    public int? Insane => Current.HasValue ? Current.Value / 5 : null;
}
