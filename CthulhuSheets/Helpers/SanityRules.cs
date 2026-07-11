using CthulhuSheets.Data;
using CthulhuSheets.Models;

namespace CthulhuSheets.Helpers;

public static class SanityRules
{
    // Cthulhu Mythos skill value (0 when the skill is absent). ch_4 / ch_8.
    public static int MythosValue(Investigator inv) =>
        inv.FindSkill(WellKnownSkills.CthulhuMythos)?.EffectiveRegular ?? 0;

    // Maximum Sanity = 99 − Cthulhu Mythos, floored at 0. Sanity can never be
    // restored above this (ch_8).
    public static int MaxSanity(Investigator inv) =>
        Math.Max(0, 99 - MythosValue(inv));
}
