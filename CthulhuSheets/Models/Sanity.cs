namespace CthulhuSheets.Models;

public class Sanity
{
    public int? Starting { get; set; }
    public int? Current { get; set; }

    // Maximum Sanity (99 − Cthulhu Mythos) is derived from the investigator's
    // skills rather than stored here, so it can never drift from the Mythos value.
}
