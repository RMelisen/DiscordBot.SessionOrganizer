using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Everything a Plynling's stored history says about what it earned before the score existed, gathered by
/// PlynlingService.RebuildScoreAsync. The counts are what the database kept; the rest is estimated
/// (<see cref="PlynlingScoreHistory.Estimated"/>).
/// </summary>
/// <param name="Lived">Time actually lived (PlynlingLife.Age): frozen and dead time never count.</param>
/// <param name="Badges">The keys of the badges it holds.</param>
/// <param name="TraitsGained">Its journal's « trait gained » moments (the childhood one at adoption included, as live).</param>
/// <param name="Recoveries">Its journal's « guéri » moments.</param>
/// <param name="BondsReached">One entry per bond reached with one other Plynling (its journal's moments, distinct by pair).</param>
/// <param name="ShortEndings">Runs it finished by its owner's choice: C# events and short stories.</param>
/// <param name="LongEndings">Long stories it reached an ending of, by its owner's choice.</param>
/// <param name="Collections">Sets its owner completed or turned in while it was alive.</param>
public sealed record PlynlingHistory(
    TimeSpan Lived,
    long Meals, long Pets, long Plays, long Visits, long Drills, long RpgXp,
    IReadOnlyCollection<string> Badges,
    int TraitsGained, int Recoveries,
    IReadOnlyCollection<PlynlingBond> BondsReached,
    int ShortEndings, int LongEndings,
    int Collections);

/// <summary>
/// Rebuilds the score of a Plynling that existed before the score shipped, living or dead, from its history —
/// pure, so the checks can compare it with a life scored as it happens. Exact wherever the database kept the
/// facts; what it never recorded (baths, forage, good mornings, which food, which visits went well) is
/// estimated at the rates of a well-cared day, scaled by how well this one was actually fed.
/// </summary>
public static class PlynlingScoreHistory
{
    // A visit's average: the good-scene bonus about half the time.
    public const double VisitAverage = PlynlingScore.Visit + PlynlingScore.VisitGoodScene * 0.5;

    // What a well-cared day earns from what was never recorded: a bath every other day, a forage, a good
    // morning. The same day the calibration checks simulate.
    public const double UnrecordedDaily = PlynlingScore.Bath * 0.5 + PlynlingScore.Forage + PlynlingScore.GoodMorning;

    // Meals a well-cared Plynling gets a day: hunger lasts three days, so about one.
    public const double MealsPerCaredDay = 1.0;

    public static long Rebuild(PlynlingHistory h)
    {
        double points =
            h.Meals * PlynlingScore.MealAverage
            + h.Pets * PlynlingScore.Pet
            + h.Plays * PlynlingScore.Play
            + h.Visits * VisitAverage
            + h.Drills * PlynlingScore.Drill
            + (PlynlingRpg.Level(h.RpgXp) - 1) * PlynlingScore.LevelUp
            + h.Badges.Sum(k => PlynlingBadges.ByKey(k)?.Reward ?? 0)
            + Stages(h.Lived)
            + h.TraitsGained * PlynlingScore.TraitGained
            + h.Recoveries * PlynlingScore.Recovered
            + h.BondsReached.Sum(PlynlingScore.Bond)
            + h.ShortEndings * PlynlingScore.EventShort
            + h.LongEndings * PlynlingScore.EventLong
            + h.Collections * PlynlingScore.Collection
            + Estimated(h);
        return (long)Math.Round(points);
    }

    /// <summary>
    /// The part never recorded, at a well-cared day's rate times the care ratio: how much of a well-cared
    /// Plynling's meals this one got (capped at 1). A neglected life earns little here, never its age alone.
    /// </summary>
    public static double Estimated(PlynlingHistory h)
    {
        var days = h.Lived.TotalDays;
        if (days <= 0) return 0;
        var care = Math.Min(1.0, h.Meals / (MealsPerCaredDay * days));
        return days * UnrecordedDaily * care;
    }

    // Every stage it grew into, by its age (the same table the journal's « est devenu… » moments read).
    public static int Stages(TimeSpan lived)
    {
        var reached = PlynlingLife.StageAt(lived);
        return Enum.GetValues<PlynlingStage>().Where(s => s <= reached).Sum(PlynlingScore.Stage);
    }

    /// <summary>The bond a journal moment says was reached, or null (a breakup, grief, anything else).</summary>
    public static PlynlingBond? BondOf(JournalKind kind) => kind switch
    {
        JournalKind.BecameFriends => PlynlingBond.Friends,
        JournalKind.BecameBestFriends => PlynlingBond.BestFriends,
        JournalKind.BecameLovers => PlynlingBond.Lovers,
        JournalKind.BecameRivals => PlynlingBond.Rivals,
        JournalKind.BecameEnemies => PlynlingBond.Enemies,
        _ => null,
    };
}
