namespace CthulhuSheets.Models;

public class Investigator
{
    // Basic Info
    public string? Name { get; set; }
    public string? Birthplace { get; set; }
    public string? Pronouns { get; set; }
    public string? Occupation { get; set; }
    public string? Residence { get; set; }
    public int? Age { get; set; }
    public string? PortraitDataUrl { get; set; }

    // Characteristics
    public Characteristic Strength { get; set; } = new() { Name = "STR" };
    public Characteristic Constitution { get; set; } = new() { Name = "CON" };
    public Characteristic Size { get; set; } = new() { Name = "SIZ" };
    public Characteristic Dexterity { get; set; } = new() { Name = "DEX" };
    public Characteristic Appearance { get; set; } = new() { Name = "APP" };
    public Characteristic Intelligence { get; set; } = new() { Name = "INT" };
    public Characteristic Power { get; set; } = new() { Name = "POW" };
    public Characteristic Education { get; set; } = new() { Name = "EDU" };

    // Pools
    public HitPoints HitPoints { get; set; } = new();
    public MagicPoints MagicPoints { get; set; } = new();
    public Luck Luck { get; set; } = new();
    public Sanity Sanity { get; set; } = new();

    // Conditions
    public bool TemporaryInsanity { get; set; }
    public bool IndefiniteInsanity { get; set; }
    public bool MajorWound { get; set; }
    public bool Unconscious { get; set; }
    public bool Dying { get; set; }

    // Other Stats
    public int? MovementRate { get; set; }
    public int? Build { get; set; }
    public int? DamageBonus { get; set; }

    // Skills
    public List<Skill> Skills { get; set; } = [];

    // Combat
    public List<Weapon> Weapons { get; set; } = [];

    // Background
    public string? MyStory { get; set; }
    public string? PersonalDescription { get; set; }
    public string? IdeologyBeliefs { get; set; }
    public string? SignificantPeople { get; set; }
    public string? MeaningfulLocations { get; set; }
    public string? TreasuredPossessions { get; set; }
    public string? Traits { get; set; }
    public string? InjuriesScars { get; set; }
    public string? PhobiasManias { get; set; }
    public string? ArcaneTomesSpells { get; set; }
    public string? EncountersWithTheMythos { get; set; }

    // Gear and Possessions
    public List<string> GearAndPossessions { get; set; } = [];

    // Wealth
    public Wealth Wealth { get; set; } = new();

    // Fellow Investigators
    public List<FellowInvestigator> FellowInvestigators { get; set; } = [];
}
