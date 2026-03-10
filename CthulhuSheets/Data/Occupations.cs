namespace CthulhuSheets.Data;

// TODO may find alternative means of storage for this? Maybe static config json?
public static class Occupations
{
    public static readonly Occupation[] All =
    [
        new()
        {
            Name = "Antiquarian",
            Skills = ["Appraise", "Art/Craft", "History", "Library Use", "Language (Other)", "Law", "Navigate", "Spot Hidden"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Dealers", "Historians", "Collectors"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Artist",
            Skills = ["Art/Craft", "History", "Listen", "Language (Other)", "Psychology", "Spot Hidden", "Charm", "Persuade"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Art galleries", "Patrons", "Fellow artists"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Athlete",
            Skills = ["Climb", "Jump", "Fighting (Brawl)", "Ride", "Swim", "Throw", "Dodge", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Sports clubs", "Agents", "Sponsors"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Author",
            Skills = ["Art/Craft", "History", "Library Use", "Language (Own)", "Language (Other)", "Natural World", "Occult", "Psychology"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Publishers", "Critics", "Historians", "Other authors"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Clergy",
            Skills = ["Accounting", "History", "Library Use", "Listen", "Language (Other)", "Persuade", "Psychology", "Charm"],
            CreditRatingMin = 9, CreditRatingMax = 60,
            SuggestedContacts = ["Church hierarchy", "Congregation", "Community leaders"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Criminal",
            Skills = ["Art/Craft", "Disguise", "Fighting (Brawl)", "Firearms (Handgun)", "Intimidate", "Locksmith", "Sleight of Hand", "Spot Hidden"],
            CreditRatingMin = 5, CreditRatingMax = 65,
            SuggestedContacts = ["Fences", "Corrupt police", "Street criminals"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Dilettante",
            Skills = ["Art/Craft", "Firearms (Handgun)", "Language (Other)", "Ride", "Charm", "Persuade", "History", "Navigate"],
            CreditRatingMin = 50, CreditRatingMax = 99,
            SuggestedContacts = ["High society", "Socialites", "Servants"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Doctor of Medicine",
            Skills = ["First Aid", "Medicine", "Language (Other)", "Psychology", "Science", "Persuade", "Library Use", "Spot Hidden"],
            CreditRatingMin = 30, CreditRatingMax = 80,
            SuggestedContacts = ["Hospitals", "Pharmacies", "Medical community"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Drifter",
            Skills = ["Climb", "Jump", "Listen", "Navigate", "Stealth", "Survival", "Dodge", "Fast Talk"],
            CreditRatingMin = 0, CreditRatingMax = 5,
            SuggestedContacts = ["Other hobos", "Lots of random people from lots of places"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Engineer",
            Skills = ["Art/Craft", "Electrical Repair", "Library Use", "Mechanical Repair", "Navigate", "Science", "Spot Hidden", "Climb"],
            CreditRatingMin = 30, CreditRatingMax = 60,
            SuggestedContacts = ["Architects", "Construction firms", "City engineers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Entertainer",
            Skills = ["Art/Craft", "Disguise", "Fast Talk", "Listen", "Persuade", "Psychology", "Charm", "Dodge"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Vaudeville", "Nightclubs", "Talent agents"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Farmer",
            Skills = ["Art/Craft", "Drive Auto", "Mechanical Repair", "Natural World", "Navigate", "Ride", "Survival", "Track"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Local farming community", "Feed store owners"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Journalist",
            Skills = ["Art/Craft", "Fast Talk", "History", "Library Use", "Language (Own)", "Persuade", "Psychology", "Charm"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["News industry", "Informants", "Police contacts"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Lawyer",
            Skills = ["Accounting", "Fast Talk", "Intimidate", "Law", "Library Use", "Persuade", "Psychology", "Charm"],
            CreditRatingMin = 30, CreditRatingMax = 80,
            SuggestedContacts = ["Courthouses", "Judges", "Law firms", "Clients"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Librarian",
            Skills = ["Accounting", "Art/Craft", "History", "Language (Other)", "Language (Own)", "Library Use", "Occult", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 35,
            SuggestedContacts = ["Libraries", "Booksellers", "Academics"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Military Officer",
            Skills = ["Accounting", "Firearms (Handgun)", "Firearms (Rifle/Shotgun)", "Navigate", "First Aid", "Intimidate", "Persuade", "Psychology"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Military", "Government", "Veterans"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Missionary",
            Skills = ["Art/Craft", "First Aid", "Mechanical Repair", "Medicine", "Natural World", "Persuade", "Language (Other)", "Charm"],
            CreditRatingMin = 0, CreditRatingMax = 30,
            SuggestedContacts = ["Church hierarchy", "Foreign cultures"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Musician",
            Skills = ["Art/Craft", "Charm", "Listen", "Persuade", "Psychology", "Sleight of Hand", "Fast Talk", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Unions", "Music halls", "Orchestras", "Band mates"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Parapsychologist",
            Skills = ["Anthropology", "Art/Craft", "History", "Library Use", "Occult", "Language (Other)", "Psychology", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Universities", "Occult societies"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Pilot",
            Skills = ["Electrical Repair", "Mechanical Repair", "Navigate", "Pilot", "Science", "Listen", "Spot Hidden", "Survival"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Airfields", "Airline companies", "Mechanics"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Police Detective",
            Skills = ["Art/Craft", "Disguise", "Firearms (Handgun)", "Law", "Listen", "Persuade", "Psychology", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Law enforcement", "Street level", "Underworld"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Private Investigator",
            Skills = ["Art/Craft", "Disguise", "Fast Talk", "Firearms (Handgun)", "Law", "Library Use", "Psychology", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Law enforcement", "Clients", "Lawyers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Professor",
            Skills = ["Library Use", "Language (Other)", "Language (Own)", "Psychology", "Science", "History", "Persuade", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Universities", "Academic circles", "Students"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Soldier",
            Skills = ["Climb", "Dodge", "Fighting (Brawl)", "Firearms (Rifle/Shotgun)", "First Aid", "Stealth", "Survival", "Swim"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Military", "Veterans", "Suppliers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        }
    ];
}
