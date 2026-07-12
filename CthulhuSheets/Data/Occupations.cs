namespace CthulhuSheets.Data;

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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("POW", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Athlete",
            Skills = ["Climb", "Jump", "Fighting (Brawl)", "Ride", "Swim", "Throw", "Dodge", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Sports clubs", "Agents", "Sponsors"],
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("APP", 2), new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("POW", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Private Investigator",
            Skills = ["Art/Craft", "Disguise", "Fast Talk", "Firearms (Handgun)", "Law", "Library Use", "Psychology", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Law enforcement", "Clients", "Lawyers"],
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
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
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Zealot",
            Skills = ["History", "Intimidate", "Persuade", "Psychology", "Stealth", "Occult", "Listen", "Fast Talk"],
            CreditRatingMin = 0, CreditRatingMax = 30,
            SuggestedContacts = ["Religious groups", "Fellow believers", "Community activists"],
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("APP", 2), new("POW", 2)]
        },
        new()
        {
            Name = "Accountant",
            Skills = ["Accounting", "Law", "Library Use", "Listen", "Persuade", "Spot Hidden", "Computer Use", "Credit Rating"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Other accountants"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Acrobat",
            Skills = ["Charm", "Climb", "Dodge", "Jump", "Language (Other)", "Throw", "Ride", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Amateur athletic circles", "Sports writers", "Circuses and carnivals"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Agency Detective",
            Skills = ["Charm", "Fast Talk", "Fighting (Brawl)", "Firearms (Handgun)", "Law", "Library Use", "Psychology", "Stealth"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Local law enforcement", "Clients"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Ambassador",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Language (Other)", "Persuade", "Psychology", "History", "Credit Rating"],
            CreditRatingMin = 60, CreditRatingMax = 95,
            SuggestedContacts = ["Federal government", "News media", "Foreign governments"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Antique Dealer",
            Skills = ["Accounting", "Appraise", "Charm", "Drive Auto", "History", "Library Use", "Navigate", "Persuade"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Local historians", "Antique dealers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Architect",
            Skills = ["Accounting", "Art/Craft", "Charm", "Language (Own)", "Library Use", "Persuade", "Psychology", "Science"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Local building and engineering departments", "Construction firms"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Bank Robber",
            Skills = ["Fighting (Brawl)", "Sleight of Hand", "Drive Auto", "Electrical Repair", "Firearms (Handgun)", "Intimidate", "Locksmith", "Stealth"],
            CreditRatingMin = 5, CreditRatingMax = 60,
            SuggestedContacts = ["Your gang", "Family and friends across the country", "Common folk who help you out"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Barber",
            Skills = ["Accounting", "Charm", "Credit Rating", "Fast Talk", "Persuade", "Psychology", "Fighting (Brawl)", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 40,
            SuggestedContacts = ["Depends on the barber's clientele"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Bartender",
            Skills = ["Accounting", "Science", "Fighting (Brawl)", "Fast Talk", "Firearms (Handgun)", "Persuade", "Psychology", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 40,
            SuggestedContacts = ["Customers", "Possibly gamblers", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Bible Salesman",
            Skills = ["Accounting", "Charm", "Persuade", "Psychology", "Fast Talk", "Drive Auto", "Credit Rating", "Occult"],
            CreditRatingMin = 3, CreditRatingMax = 20,
            SuggestedContacts = ["Religious publishers"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Big Game Hunter",
            Skills = ["Charm", "Firearms (Rifle/Shotgun)", "First Aid", "Stealth", "Natural World", "Language (Other)", "Track", "Listen"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Local government officials", "Game wardens", "Past clients"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Book Dealer",
            Skills = ["Accounting", "Appraise", "Charm", "Language (Own)", "History", "Library Use", "Language (Other)", "Persuade"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Bibliographers", "Book dealers", "Libraries", "Universities"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Bookie",
            Skills = ["Accounting", "Charm", "Sleight of Hand", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Listen"],
            CreditRatingMin = 30, CreditRatingMax = 90,
            SuggestedContacts = ["Organized crime", "Gamblers", "Local police", "Sports figures"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Bounty Hunter",
            Skills = ["Charm", "Climb", "Drive Auto", "Fast Talk", "Firearms (Handgun)", "Fighting (Brawl)", "Stealth", "Track"],
            CreditRatingMin = 9, CreditRatingMax = 40,
            SuggestedContacts = ["Bail bondsmen", "Local police"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Boxer/Wrestler",
            Skills = ["Fighting (Brawl)", "Dodge", "Throw", "Jump", "Intimidate", "Listen", "Climb", "First Aid"],
            CreditRatingMin = 5, CreditRatingMax = 30,
            SuggestedContacts = ["Boxing promoters", "Sports writers", "Organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Burglar",
            Skills = ["Sleight of Hand", "Climb", "Stealth", "Jump", "Listen", "Locksmith", "Spot Hidden", "Appraise"],
            CreditRatingMin = 5, CreditRatingMax = 30,
            SuggestedContacts = ["Fences", "Other burglars"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Bus Driver",
            Skills = ["Accounting", "Navigate", "Drive Auto", "Electrical Repair", "Mechanical Repair", "Persuade", "Psychology", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 40,
            SuggestedContacts = ["Few"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Catholic Priest",
            Skills = ["Accounting", "Charm", "Language (Own)", "Language (Other)", "Library Use", "Occult", "Persuade", "Psychology"],
            CreditRatingMin = 9, CreditRatingMax = 60,
            SuggestedContacts = ["Church hierarchy", "Local congregation", "Community leaders"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Clerk",
            Skills = ["Accounting", "Charm", "Credit Rating", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Library Use"],
            CreditRatingMin = 9, CreditRatingMax = 40,
            SuggestedContacts = ["Other office workers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Cocktail Waitress",
            Skills = ["Accounting", "Fast Talk", "Persuade", "Psychology", "Charm", "Listen", "Spot Hidden", "Dodge"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Customers", "Possibly gamblers", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Columnist",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Library Use", "History"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["News industry", "Others depending on the type of column"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Communist/Radical",
            Skills = ["Fighting (Brawl)", "Fast Talk", "Firearms (Handgun)", "Language (Other)", "Persuade", "Psychology", "History", "Stealth"],
            CreditRatingMin = 0, CreditRatingMax = 30,
            SuggestedContacts = ["Other radicals", "Artists", "Writers", "Unions"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Company Executive",
            Skills = ["Accounting", "Charm", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Law"],
            CreditRatingMin = 50, CreditRatingMax = 95,
            SuggestedContacts = ["Business and finance worlds", "Old college connections", "Fraternal groups", "Government"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Con Man",
            Skills = ["Charm", "Disguise", "Fast Talk", "Listen", "Persuade", "Psychology", "Spot Hidden", "Sleight of Hand"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Other con men"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Crime Boss",
            Skills = ["Accounting", "Charm", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Intimidate", "Law"],
            CreditRatingMin = 50, CreditRatingMax = 95,
            SuggestedContacts = ["News media", "Finance", "Big business", "Organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Deacon/Elder",
            Skills = ["Accounting", "Charm", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Library Use", "History"],
            CreditRatingMin = 9, CreditRatingMax = 60,
            SuggestedContacts = ["Church hierarchy", "The congregation", "Local business and community leaders"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Deep-sea Diver",
            Skills = ["Swim", "Mechanical Repair", "Natural World", "Pilot", "Spot Hidden", "First Aid", "Jump", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Coast Guard", "Smugglers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Dentist",
            Skills = ["Accounting", "Science", "First Aid", "Library Use", "Medicine", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Clientele"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Designer",
            Skills = ["Accounting", "Art/Craft", "Charm", "Electrical Repair", "Mechanical Repair", "Psychology", "Spot Hidden", "Library Use"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Advertising", "Stage", "Furnishings", "Architectural"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "DJ",
            Skills = ["Fast Talk", "Listen", "Persuade", "Psychology", "Charm", "Electrical Repair", "Art/Craft", "Fighting (Brawl)"],
            CreditRatingMin = 5, CreditRatingMax = 20,
            SuggestedContacts = ["Musicians", "Record companies", "Music scene", "Customers"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Editor",
            Skills = ["Accounting", "Charm", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Library Use", "History"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["The news industry", "Local government", "Others"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Elected Official",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Law", "History"],
            CreditRatingMin = 30, CreditRatingMax = 90,
            SuggestedContacts = ["Government", "News media", "Foreign governments", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Explorer",
            Skills = ["Climb", "Firearms (Rifle/Shotgun)", "First Aid", "Jump", "Natural World", "Navigate", "Language (Other)", "Survival"],
            CreditRatingMin = 30, CreditRatingMax = 80,
            SuggestedContacts = ["Major libraries and universities", "Monied patrons", "Other explorers", "Publishers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Federal Agent",
            Skills = ["Drive Auto", "Fast Talk", "Firearms (Handgun)", "Fighting (Brawl)", "Law", "Persuade", "Stealth", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Federal contacts", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Fence",
            Skills = ["Accounting", "Appraise", "Charm", "Sleight of Hand", "Fast Talk", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Street criminals", "Organized crime", "Police", "Suppliers and customers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Field Researcher",
            Skills = ["Accounting", "Climb", "First Aid", "Library Use", "Language (Other)", "Persuade", "Science", "Natural World"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Other scholars in your field", "Grant foundations", "News media", "Patrons"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Film Crew",
            Skills = ["Art/Craft", "Climb", "Drive Auto", "Electrical Repair", "Mechanical Repair", "Spot Hidden", "Listen", "Library Use"],
            CreditRatingMin = 20, CreditRatingMax = 40,
            SuggestedContacts = ["The film industry", "Associated unions"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Film Star",
            Skills = ["Art/Craft", "Charm", "Disguise", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 9, CreditRatingMax = 95,
            SuggestedContacts = ["The film industry", "Critics", "Organized crime", "Actor's guild"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Fireman",
            Skills = ["Fighting (Brawl)", "Climb", "Dodge", "First Aid", "Jump", "Credit Rating", "Throw", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["A few civic amenities"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Foreign Correspondent",
            Skills = ["Charm", "Sleight of Hand", "Fast Talk", "Stealth", "Language (Other)", "Persuade", "Psychology", "History"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["The worldwide news industry", "Foreign governments", "Military"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Forensic Investigator",
            Skills = ["Science", "Law", "Medicine", "Art/Craft", "Spot Hidden", "Library Use", "First Aid", "Psychology"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Law enforcement", "Local labs", "Chemical supply outlets"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Forensic Surgeon",
            Skills = ["Science", "First Aid", "Medicine", "Library Use", "Credit Rating", "Psychology", "Spot Hidden", "Law"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Laboratory facilities", "Law enforcement", "Medical profession"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Forester",
            Skills = ["Fighting (Brawl)", "Charm", "Dodge", "Climb", "Jump", "Natural World", "Survival", "Track"],
            CreditRatingMin = 9, CreditRatingMax = 20,
            SuggestedContacts = ["Few"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Forger/Counterfeiter",
            Skills = ["Accounting", "Art/Craft", "Charm", "Sleight of Hand", "Spot Hidden", "Persuade", "Psychology", "Appraise"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Organized crime", "Street criminals", "Businessmen"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Gambler",
            Skills = ["Accounting", "Charm", "Sleight of Hand", "Fast Talk", "Listen", "Persuade", "Psychology", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Bookies", "Organized crime", "Street scene"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Gangster",
            Skills = ["Charm", "Fighting (Brawl)", "Drive Auto", "Firearms (Handgun)", "Intimidate", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 95,
            SuggestedContacts = ["Organized crime", "Street scene", "Police", "City government", "Unions"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Gardener",
            Skills = ["Charm", "Fighting (Brawl)", "Art/Craft", "Natural World", "Mechanical Repair", "Spot Hidden", "Climb", "Survival"],
            CreditRatingMin = 5, CreditRatingMax = 20,
            SuggestedContacts = ["None"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Golf Pro",
            Skills = ["Charm", "Fighting (Brawl)", "Credit Rating", "Persuade", "Psychology", "Spot Hidden", "Throw", "Listen"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Other pros", "Sports writers", "Wealthy and influential club members"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Gravedigger",
            Skills = ["Charm", "Fighting (Brawl)", "Spot Hidden", "Occult", "Natural World", "Mechanical Repair", "Listen", "Survival"],
            CreditRatingMin = 5, CreditRatingMax = 20,
            SuggestedContacts = ["None"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Hacker",
            Skills = ["Computer Use", "Electrical Repair", "Fast Talk", "Library Use", "Language (Other)", "Science", "Spot Hidden", "Persuade"],
            CreditRatingMin = 10, CreditRatingMax = 70,
            SuggestedContacts = ["Other hackers, usually anonymously"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Hired Goon",
            Skills = ["Fighting (Brawl)", "Sleight of Hand", "Drive Auto", "Firearms (Handgun)", "Intimidate", "Stealth", "Dodge", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Organized crime", "Street scene", "Local cops", "Local ethnic community"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Hit Man",
            Skills = ["Charm", "Fighting (Brawl)", "Sleight of Hand", "Firearms (Handgun)", "Stealth", "Disguise", "Spot Hidden", "Drive Auto"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Few, mostly underworld"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Hooker",
            Skills = ["Charm", "Sleight of Hand", "Fast Talk", "Stealth", "Persuade", "Psychology", "Spot Hidden", "Listen"],
            CreditRatingMin = 5, CreditRatingMax = 70,
            SuggestedContacts = ["Street scene", "Police", "Possibly organized crime", "Personal clientele"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Judge",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Law", "Library Use", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 90,
            SuggestedContacts = ["Legal connections", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Loan Shark",
            Skills = ["Accounting", "Charm", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Intimidate", "Fighting (Brawl)"],
            CreditRatingMin = 30, CreditRatingMax = 90,
            SuggestedContacts = ["Organized crime", "Gamblers", "Police", "Debtors who owe a favour"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Lumberjack",
            Skills = ["Fighting (Brawl)", "Charm", "Dodge", "Climb", "Jump", "Natural World", "Mechanical Repair", "Throw"],
            CreditRatingMin = 9, CreditRatingMax = 20,
            SuggestedContacts = ["Few"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Manager/Coach",
            Skills = ["Accounting", "Charm", "Dodge", "Fast Talk", "First Aid", "Persuade", "Psychology", "Fighting (Brawl)"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Athletic circles", "Sports writers", "Wealthy and influential alumnae"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Medical Technician",
            Skills = ["Science", "Electrical Repair", "Library Use", "Mechanical Repair", "Medicine", "First Aid", "Spot Hidden", "Art/Craft"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Medical and hospital laboratories", "Drug and chemical suppliers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Mental Hospital Attendant",
            Skills = ["Fighting (Brawl)", "Fast Talk", "First Aid", "Listen", "Persuade", "Psychology", "Stealth", "Medicine"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Access to medical records, drugs, and other medical supplies"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Mercenary",
            Skills = ["Fighting (Brawl)", "Climb", "Firearms (Rifle/Shotgun)", "Stealth", "Jump", "Navigate", "Track", "Survival"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Mercenary networks", "Illegal arms dealers", "Small governments", "Multinational corporations"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Miner",
            Skills = ["Science", "Climb", "Jump", "Operate Heavy Machinery", "Spot Hidden", "Mechanical Repair", "Fighting (Brawl)", "Survival"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Union officials and activists"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Mountain Climber",
            Skills = ["Climb", "First Aid", "Jump", "Listen", "Navigate", "Language (Other)", "Throw", "Survival"],
            CreditRatingMin = 20, CreditRatingMax = 60,
            SuggestedContacts = ["Other climbers", "Park rangers", "Foreign governments", "Patrons"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Museum Curator",
            Skills = ["Accounting", "Appraise", "Charm", "Language (Own)", "Library Use", "Persuade", "History", "Archaeology"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Local universities", "Scholars", "Publishers", "Museum patrons"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "News Chopper Pilot",
            Skills = ["Charm", "Navigate", "Climb", "Language (Own)", "Listen", "Science", "Pilot", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["News industry", "City planners", "Mechanics", "Helicopter manufacturers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Nurse",
            Skills = ["Science", "First Aid", "Medicine", "Persuade", "Psychology", "Credit Rating", "Listen", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["Access to drugs, equipment, and medical records"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Occultist",
            Skills = ["Anthropology", "Language (Own)", "History", "Library Use", "Occult", "Language (Other)", "Psychology", "Spot Hidden"],
            CreditRatingMin = 0, CreditRatingMax = 20,
            SuggestedContacts = ["Libraries", "Occult societies", "Other occultists"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Pharmacist",
            Skills = ["Accounting", "Science", "First Aid", "Language (Other)", "Library Use", "Medicine", "Persuade", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Access to drugs", "Local physicians and hospitals", "Local community"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Photographer",
            Skills = ["Accounting", "Science", "Art/Craft", "Persuade", "Psychology", "Spot Hidden", "Library Use", "Listen"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["The advertising, news, and/or film industries", "Camera manufacturers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Photojournalist",
            Skills = ["Climb", "Charm", "Language (Own)", "Fast Talk", "Jump", "Language (Other)", "Persuade", "Art/Craft"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["The news industry", "Film and camera manufacturers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Plastic Surgeon",
            Skills = ["Accounting", "Science", "First Aid", "Language (Other)", "Library Use", "Medicine", "Persuade", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Medical profession", "Hollywood", "Possibly criminal figures"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Police Officer",
            Skills = ["Fighting (Brawl)", "Drive Auto", "Firearms (Handgun)", "Law", "Persuade", "Spot Hidden", "First Aid", "Track"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Law enforcement", "Local shopkeeps", "Street scene", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Preacher",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Stealth", "Persuade", "Psychology", "Occult", "History"],
            CreditRatingMin = 0, CreditRatingMax = 20,
            SuggestedContacts = ["Speaks with God"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Programmer",
            Skills = ["Accounting", "Computer Use", "Electrical Repair", "Fast Talk", "Library Use", "Language (Other)", "Science", "Spot Hidden"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Other programmers", "Hackers", "Customers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Prosecuting Attorney",
            Skills = ["Charm", "Language (Own)", "Fast Talk", "Law", "Library Use", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Legal connections", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Psychologist",
            Skills = ["Accounting", "Charm", "Library Use", "Persuade", "Psychoanalysis", "Psychology", "Science", "Listen"],
            CreditRatingMin = 20, CreditRatingMax = 50,
            SuggestedContacts = ["The psychological community", "Possibly patients"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Punk",
            Skills = ["Fighting (Brawl)", "Throw", "Dodge", "Stealth", "Intimidate", "Listen", "Jump", "Climb"],
            CreditRatingMin = 0, CreditRatingMax = 20,
            SuggestedContacts = ["Street criminals", "Other punks", "The local fence", "The local police"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Rabbi",
            Skills = ["Language (Own)", "Language (Other)", "History", "Library Use", "Occult", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 5, CreditRatingMax = 70,
            SuggestedContacts = ["Jewish scholars", "The local Jewish community"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Racecar Driver",
            Skills = ["Drive Auto", "Electrical Repair", "Mechanical Repair", "Pilot", "Psychology", "Spot Hidden", "Jump", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 70,
            SuggestedContacts = ["Auto and boat manufacturers", "The film industry (stunt driving)"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Ranch Hand/Cowboy",
            Skills = ["Charm", "Art/Craft", "Drive Auto", "Firearms (Rifle/Shotgun)", "First Aid", "Natural World", "Ride", "Track"],
            CreditRatingMin = 5, CreditRatingMax = 30,
            SuggestedContacts = ["Local bank", "Local politicians", "State agricultural department"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Researcher",
            Skills = ["Language (Own)", "Library Use", "Credit Rating", "Science", "History", "Persuade", "Spot Hidden", "Language (Other)"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Other scholars in your field", "Corporation libraries and laboratories"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Sailor",
            Skills = ["Climb", "Fighting (Brawl)", "Firearms (Handgun)", "Jump", "Navigate", "Firearms (Rifle/Shotgun)", "Swim", "Mechanical Repair"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Military", "Veterans' Administration"],
            SkillPointFormulas = [new("EDU", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Salesman",
            Skills = ["Accounting", "Charm", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Drive Auto", "Listen"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Contacts in the salesman's particular area of business or industry"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Secretary",
            Skills = ["Accounting", "Credit Rating", "Computer Use", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Library Use"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["Other office workers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Shifty Accountant/Lawyer",
            Skills = ["Accounting", "Charm", "Language (Own)", "Fast Talk", "Law", "Persuade", "Psychology", "Credit Rating"],
            CreditRatingMin = 30, CreditRatingMax = 70,
            SuggestedContacts = ["Organized crime", "Finance", "DAs and judges"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Small Business Owner",
            Skills = ["Accounting", "Charm", "Credit Rating", "Fast Talk", "Persuade", "Psychology", "Library Use", "Spot Hidden"],
            CreditRatingMin = 30, CreditRatingMax = 80,
            SuggestedContacts = ["Bankers", "Suppliers", "Customers"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Smuggler",
            Skills = ["Charm", "Sleight of Hand", "Fast Talk", "Firearms (Handgun)", "Navigate", "Persuade", "Pilot", "Spot Hidden"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Organized crime", "Coast Guard", "US Customs officials"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Spy",
            Skills = ["Charm", "Disguise", "Fast Talk", "Firearms (Handgun)", "Language (Other)", "Persuade", "Psychology", "Stealth"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["The person the spy reports to", "Cover connections", "Federal government"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Stage Actor",
            Skills = ["Art/Craft", "Disguise", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Charm", "Listen"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Theatre industry", "Newspaper critics", "Actor's guild"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Stage Hand",
            Skills = ["Art/Craft", "Language (Own)", "Fast Talk", "Disguise", "Persuade", "Psychology", "Electrical Repair", "Climb"],
            CreditRatingMin = 0, CreditRatingMax = 20,
            SuggestedContacts = ["Theatre industry", "Actor's guild"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Stock Broker",
            Skills = ["Accounting", "Charm", "Language (Own)", "Fast Talk", "Persuade", "Psychology", "Credit Rating", "Law"],
            CreditRatingMin = 50, CreditRatingMax = 95,
            SuggestedContacts = ["Business and finance worlds", "Hungry investors"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Student/Intern",
            Skills = ["Language (Own)", "Library Use", "Science", "History", "Psychology", "Persuade", "Computer Use", "Language (Other)"],
            CreditRatingMin = 0, CreditRatingMax = 20,
            SuggestedContacts = ["Access to professors, libraries, and other facilities"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Stunt Man",
            Skills = ["Climb", "Dodge", "Drive Auto", "First Aid", "Fighting (Brawl)", "Jump", "Ride", "Swim"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["The film industry", "Explosive and pyrotechnic firms", "Freelance investors"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Surveyor",
            Skills = ["Accounting", "Charm", "Navigate", "Library Use", "Natural World", "Art/Craft", "Spot Hidden", "Science"],
            CreditRatingMin = 9, CreditRatingMax = 50,
            SuggestedContacts = ["State and local records offices"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Swimmer/Diver",
            Skills = ["Swim", "Language (Other)", "Jump", "Climb", "Dodge", "First Aid", "Throw", "Listen"],
            CreditRatingMin = 0, CreditRatingMax = 9,
            SuggestedContacts = ["Amateur athletic world", "Sports writers"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Talent Agent",
            Skills = ["Accounting", "Charm", "Fast Talk", "Law", "Persuade", "Psychology", "Credit Rating", "Listen"],
            CreditRatingMin = 5, CreditRatingMax = 60,
            SuggestedContacts = ["Publishing industry", "Film industry", "Others"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        },
        new()
        {
            Name = "Taxi Driver",
            Skills = ["Accounting", "Charm", "Drive Auto", "Electrical Repair", "Fast Talk", "Mechanical Repair", "Navigate", "Psychology"],
            CreditRatingMin = 5, CreditRatingMax = 20,
            SuggestedContacts = ["Street scene", "The occasional notable customer"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Telephone Operator",
            Skills = ["Language (Own)", "Fast Talk", "Listen", "Persuade", "Psychology", "Electrical Repair", "Spot Hidden", "Library Use"],
            CreditRatingMin = 9, CreditRatingMax = 30,
            SuggestedContacts = ["Other office workers", "Overheard phone conversations"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Tennis Pro",
            Skills = ["Charm", "Fighting (Brawl)", "Dodge", "Jump", "Persuade", "Psychology", "Spot Hidden", "Throw"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Other tennis pros", "Sports writers", "Wealthy and influential club members"],
            SkillPointFormulas = [new("EDU", 2), new("DEX", 2)]
        },
        new()
        {
            Name = "Tribal Member",
            Skills = ["Charm", "Listen", "Natural World", "Occult", "Language (Other)", "Spot Hidden", "Swim", "Throw"],
            CreditRatingMin = 0, CreditRatingMax = 15,
            SuggestedContacts = ["Other tribal members of varied professions"],
            SkillPointFormulas = [new("EDU", 2)],
            SkillPointFormulaOptions = [new("DEX", 2), new("STR", 2)]
        },
        new()
        {
            Name = "Undertaker",
            Skills = ["Accounting", "Charm", "Science", "Persuade", "Psychology", "Credit Rating", "First Aid", "Occult"],
            CreditRatingMin = 20, CreditRatingMax = 70,
            SuggestedContacts = ["Few"],
            SkillPointFormulas = [new("EDU", 4)]
        },
        new()
        {
            Name = "Union Activist",
            Skills = ["Accounting", "Charm", "Fighting (Brawl)", "Language (Own)", "Fast Talk", "Law", "Persuade", "Psychology"],
            CreditRatingMin = 0, CreditRatingMax = 30,
            SuggestedContacts = ["Other labour leaders and activists", "Socialists and subversives", "Possibly organized crime"],
            SkillPointFormulas = [new("EDU", 2), new("APP", 2)]
        }
    ];
}
