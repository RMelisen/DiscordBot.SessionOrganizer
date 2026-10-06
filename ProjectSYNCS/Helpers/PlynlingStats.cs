using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The five stats. Never stored as values, but **append-only** all the same: the RPG layer appends
// Force, Agilité and Endurance at the end, and the growth columns are named after these.
public enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage }

// One stat and where it comes from, for the « Personnalité » breakdown.
public sealed record StatLine(PlynlingStat Stat, int Base, int Passion, int Traits, int Growth, int Total);

/// <summary>
/// A stat is computed, never stored: a die rolled from the id (1d6, hashed — nothing stored), the
/// passion's +2, every trait's modifier, and the growth events earned. Floor 0, no upper limit.
/// The mascot's die is fixed at <see cref="PlynlingMascot.BaseStat"/>, so she is the same character
/// on every guild and on dev.
/// </summary>
public static class PlynlingStats
{
    public const int PassionBonus = 2;
    private const int BaseSalt = 200;           // + the stat

    public static readonly IReadOnlyList<PlynlingStat> All = Enum.GetValues<PlynlingStat>();

    public static string Name(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "Diplomatie",
        PlynlingStat.Stewardship => "Intendance",
        PlynlingStat.Learning => "Sagesse",
        PlynlingStat.Intrigue => "Ruse",
        PlynlingStat.Courage => "Courage",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static string Emoji(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "💬",
        PlynlingStat.Stewardship => "📦",
        PlynlingStat.Learning => "📚",
        PlynlingStat.Intrigue => "🦊",
        PlynlingStat.Courage => "🛡️",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    // Exhaustive on purpose: a 13th passion must be given a stat here, or this throws.
    public static PlynlingStat PassionStat(PlynlingPassion passion) => passion switch
    {
        PlynlingPassion.Music or PlynlingPassion.Dance or PlynlingPassion.Painting => PlynlingStat.Diplomacy,
        PlynlingPassion.Cooking or PlynlingPassion.Gardening or PlynlingPassion.Rocks => PlynlingStat.Stewardship,
        PlynlingPassion.Astronomy or PlynlingPassion.Stories => PlynlingStat.Learning,
        PlynlingPassion.Gaming or PlynlingPassion.Naps => PlynlingStat.Intrigue,
        PlynlingPassion.Sport or PlynlingPassion.Insects => PlynlingStat.Courage,
        _ => throw new ArgumentOutOfRangeException(nameof(passion), passion, null),
    };

    // The passion that earns the bonus: a taught passion the catalog recognises, else the innate one.
    // A taught passion that stays free text changes nothing.
    public static PlynlingPassion PassionFor(Plynling p) =>
        PlynlingPassions.Taught(p) is { Catalog: { } taught } ? taught : p.Passion;

    public static int Base(Plynling p, PlynlingStat stat) =>
        PlynlingMascot.Is(p) ? PlynlingMascot.BaseStat : 1 + (int)(StableRoll.Unit(p.Id, BaseSalt + (int)stat, 0) * 6);

    public static int Growth(Plynling p, PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => p.GrowthDiplomacy,
        PlynlingStat.Stewardship => p.GrowthStewardship,
        PlynlingStat.Learning => p.GrowthLearning,
        PlynlingStat.Intrigue => p.GrowthIntrigue,
        PlynlingStat.Courage => p.GrowthCourage,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static IReadOnlyList<StatLine> Compute(Plynling p, IReadOnlyList<TraitInfo> traits)
    {
        var passionStat = PassionStat(PassionFor(p));
        return All.Select(stat =>
        {
            var die = Base(p, stat);
            var passion = stat == passionStat ? PassionBonus : 0;
            var fromTraits = traits.Sum(t => t.Stats.GetValueOrDefault(stat));
            var growth = Growth(p, stat);
            return new StatLine(stat, die, passion, fromTraits, growth, Math.Max(0, die + passion + fromTraits + growth));
        }).ToList();
    }
}
