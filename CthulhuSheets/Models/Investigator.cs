namespace CthulhuSheets.Models;

public class Investigator
{
    public Guid Id { get; set; }

    // Basic Info
    [Required(ErrorMessage = "Name is required")]
    public string? Name { get; set; }
    [Required(ErrorMessage = "Birthplace is required")]
    public string? Birthplace { get; set; }
    [Required(ErrorMessage = "Pronouns are required")]
    public string? Pronouns { get; set; }
    public string? Occupation { get; set; }
    [Required(ErrorMessage = "Residence is required")]
    public string? Residence { get; set; }
    public int? Age { get; set; }
    [JsonIgnore]
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

    // Canonical accessors over the eight characteristics (see ch_3). These read
    // the fixed properties above and store nothing new, so they don't affect
    // serialization — [JsonIgnore] on the enumerable mirrors the Half/Fifth/
    // EffectiveRegular computed-member idiom already used on the models.
    [JsonIgnore]
    public IEnumerable<Characteristic> Characteristics =>
        [Strength, Constitution, Size, Dexterity, Appearance, Intelligence, Power, Education];

    // Resolves a characteristic by its abbreviation ("STR"…"EDU"), case-insensitively.
    // Returns null for an unknown abbrev — callers decide whether to coalesce (→0)
    // or throw, preserving their existing behavior.
    public Characteristic? GetCharacteristic(string abbrev) => abbrev?.ToUpperInvariant() switch
    {
        "STR" => Strength,
        "CON" => Constitution,
        "SIZ" => Size,
        "DEX" => Dexterity,
        "APP" => Appearance,
        "INT" => Intelligence,
        "POW" => Power,
        "EDU" => Education,
        _ => null
    };

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
    public string? DamageBonus { get; set; }

    // Skills
    public List<Skill> Skills { get; set; } = [];

    // Combat
    public List<Weapon> Weapons { get; set; } = [];

    // Background
    public string? MyStory { get; set; }
    public string? KeyConnection { get; set; }
    public string? PersonalDescription { get; set; }
    public string? IdeologyBeliefs { get; set; }
    public string? SignificantPeople { get; set; }
    public string? Contacts { get; set; }
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

    // Per-character UI preferences (sort order, etc.). Defaults to new() so saves
    // written before this field existed deserialize to the defaults (Alphabetical).
    public CharacterPreferences Preferences { get; set; } = new();
}
