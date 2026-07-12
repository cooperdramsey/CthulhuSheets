using CthulhuSheets.Helpers;
using CthulhuSheets.Models;
using CthulhuSheets.Services;

namespace CthulhuSheets.Tests;

public class DevelopmentPhaseTests
{
    // A Random that returns a scripted sequence, so each DiceRollService.Roll(sides)
    // (which calls Next(1, sides+1)) yields a known value in order.
    private sealed class ScriptedRandom(params int[] values) : Random
    {
        private readonly Queue<int> _values = new(values);
        public override int Next(int minValue, int maxValue) => _values.Dequeue();
    }

    private static DiceRollService Dice(params int[] rolls) => new(new ScriptedRandom(rolls));

    private static Skill Ticked(string name, int regular) =>
        new() { Name = name, Regular = regular, HasExperienceCheck = true };

    [Fact]
    public void Run_SuccessfulRoll_AddsGainAndClearsTick()
    {
        var inv = new Investigator();
        inv.Skills.Add(Ticked("Spot Hidden", 50));
        // D100 = 70 (> 50 → success), then D10 gain = 8.
        var results = DevelopmentPhase.Run(inv, Dice(70, 8));

        var skill = inv.Skills[0];
        Assert.Equal(58, skill.Regular);
        Assert.False(skill.HasExperienceCheck);

        var r = Assert.Single(results);
        Assert.True(r.Improved);
        Assert.Equal(70, r.Roll);
        Assert.Equal(50, r.OldValue);
        Assert.Equal(58, r.NewValue);
        Assert.Equal(0, r.SanityGained);
    }

    [Fact]
    public void Run_FailedRoll_NoChangeButClearsTick()
    {
        var inv = new Investigator();
        inv.Skills.Add(Ticked("Spot Hidden", 80));
        // D100 = 40 (not > 80 and not > 95 → failure). No gain die consumed.
        var results = DevelopmentPhase.Run(inv, Dice(40));

        Assert.Equal(80, inv.Skills[0].Regular);
        Assert.False(inv.Skills[0].HasExperienceCheck);
        Assert.False(Assert.Single(results).Improved);
    }

    [Fact]
    public void Run_RollOver95_SucceedsEvenWhenBelowCurrent()
    {
        var inv = new Investigator();
        inv.Skills.Add(Ticked("Library Use", 99));
        // D100 = 97 (not > 99, but > 95 → success), D10 gain = 3.
        DevelopmentPhase.Run(inv, Dice(97, 3));

        Assert.Equal(102, inv.Skills[0].Regular); // may exceed 100
    }

    [Fact]
    public void Run_CrossingInto90_GrantsTwoD6Sanity_CappedAtMaxSanity()
    {
        var inv = new Investigator();
        inv.Sanity.Current = 60;
        // No Cthulhu Mythos → MaxSanity = 99, so the full bonus applies.
        inv.Skills.Add(Ticked("Occult", 85));
        // D100 = 90 (> 85 → success), D10 gain = 6 → 91 (crosses 90),
        // then 2D6 for Sanity = 4 + 5 = 9.
        var results = DevelopmentPhase.Run(inv, Dice(90, 6, 4, 5));

        Assert.Equal(91, inv.Skills[0].Regular);
        Assert.Equal(69, inv.Sanity.Current);          // 60 + 9
        Assert.Equal(9, Assert.Single(results).SanityGained);
    }

    [Fact]
    public void Run_SanityBonus_ClampedToMaxSanity_WhenMythosLowersCeiling()
    {
        var inv = new Investigator();
        inv.Sanity.Current = 60;
        inv.Skills.Add(new Skill { Name = "Cthulhu Mythos", Regular = 35 }); // MaxSanity = 99 − 35 = 64
        inv.Skills.Add(Ticked("Occult", 85));
        // D100 = 90 success, gain 6 → 91 (crosses 90), 2D6 = 6 + 6 = 12,
        // but 60 + 12 = 72 is clamped to 64, so effective gain = 4.
        var results = DevelopmentPhase.Run(inv, Dice(90, 6, 6, 6));

        Assert.Equal(64, inv.Sanity.Current);
        Assert.Equal(4, results.Single(r => r.SkillName == "Occult").SanityGained);
    }

    [Fact]
    public void Run_SkipsNonImprovableAndUntickedSkills()
    {
        var inv = new Investigator();
        inv.Skills.Add(Ticked("Credit Rating", 40));   // non-improvable
        inv.Skills.Add(Ticked("Cthulhu Mythos", 10));  // non-improvable
        inv.Skills.Add(new Skill { Name = "Dodge", Regular = 30, HasExperienceCheck = false }); // not ticked
        // Only the non-improvable/unticked skills exist → nothing rolls.
        var results = DevelopmentPhase.Run(inv, Dice());

        Assert.Empty(results);
        Assert.Equal(40, inv.Skills[0].Regular);
        Assert.True(inv.Skills[0].HasExperienceCheck);  // untouched (not processed)
    }
}
