namespace CthulhuSheets.Models;

public class Roster
{
    public Guid? ActiveId { get; set; }
    public List<RosterEntry> Entries { get; set; } = [];
}
