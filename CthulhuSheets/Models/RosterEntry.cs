namespace CthulhuSheets.Models;

public class RosterEntry
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Occupation { get; set; }
    public DateTimeOffset LastModified { get; set; }
}
