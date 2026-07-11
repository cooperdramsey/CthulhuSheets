using CthulhuSheets.Models;

namespace CthulhuSheets.Data;

/// <summary>
/// Canonical list of standard CoC 7e skills and their printed base values.
/// Single source of truth shared by character creation and the play-time sheet
/// so the two cannot drift. Base values follow Chapter 4: Skills. Skills whose
/// base is derived from a characteristic (Dodge = ½DEX, Language (Own) = EDU)
/// are stored as 0 here and computed per-investigator via <see cref="ComputeBase"/>.
/// </summary>
public static class DefaultSkills
{
    public static readonly (string Name, int BaseValue)[] All =
    [
        ("Accounting",               5),
        ("Anthropology",             1),
        ("Appraise",                 5),
        ("Archaeology",              1),
        ("Art/Craft",                5),
        ("Charm",                   15),
        ("Climb",                   20),
        ("Computer Use",             5),
        ("Credit Rating",            0),
        ("Cthulhu Mythos",           0),
        ("Disguise",                 5),
        ("Dodge",                    0),
        ("Drive Auto",              20),
        ("Electrical Repair",       10),
        ("Fast Talk",                5),
        ("Fighting (Brawl)",        25),
        ("Firearms (Handgun)",      20),
        ("Firearms (Rifle/Shotgun)", 25),
        ("First Aid",               30),
        ("History",                  5),
        ("Intimidate",              15),
        ("Jump",                    20),
        ("Language (Other)",         1),
        ("Language (Own)",           0),
        ("Law",                      5),
        ("Library Use",             20),
        ("Listen",                  20),
        ("Locksmith",                1),
        ("Mechanical Repair",       10),
        ("Medicine",                 1),
        ("Natural World",           10),
        ("Navigate",                10),
        ("Occult",                   5),
        ("Operate Heavy Machinery",  1),
        ("Persuade",                10),
        ("Pilot",                    1),
        ("Psychoanalysis",           1),
        ("Psychology",              10),
        ("Ride",                     5),
        ("Science",                  1),
        ("Sleight of Hand",         10),
        ("Spot Hidden",             25),
        ("Stealth",                 20),
        ("Survival",                10),
        ("Swim",                    20),
        ("Throw",                   20),
        ("Track",                   10),
    ];

    /// <summary>
    /// Resolves a skill's base value for a specific investigator, substituting the
    /// characteristic-derived bases for Dodge (½DEX) and Language (Own) (EDU).
    /// </summary>
    public static int ComputeBase(string name, int printedBase, Investigator investigator) => name switch
    {
        WellKnownSkills.Dodge       => investigator.Dexterity.Half ?? 0,
        WellKnownSkills.LanguageOwn => investigator.Education.Regular ?? 0,
        _                           => printedBase
    };
}
