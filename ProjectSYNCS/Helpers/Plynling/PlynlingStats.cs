using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The stats. Never stored as values, but **append-only** all the same: story files and the growth and
// points columns are named after these. The five mind stats come first; the RPG's five body stats
// (docs/agents/plynling-rpg.md) were appended after them.
public enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage, Force, Agility, Endurance, Intelligence, Speed }

// One stat and where it comes from, for the « Personnalité » breakdown. Species and Points are only
// ever non-zero on a body stat, Passion only on a mind stat.
// State is what passes: the stress penalty and the modifiers' deltas.
public sealed record StatLine(PlynlingStat Stat, int Base, int Passion, int Species, int Traits, int Growth, int Points, int State, int Total);

/// <summary>
/// A stat is computed, never stored: a die rolled from the id (1d6, hashed — nothing stored), the
/// passion's +2 (mind stats), its species' lean (body stats), every trait's modifier, the growth events
/// and drills earned, the level points its owner placed (body stats), and its state (stress, modifiers).
/// Floor 0, no upper limit.
/// The mascot's die is fixed at <see cref="PlynlingMascot.BaseStat"/>, so she is the same character
/// on every guild and on dev.
/// </summary>
public static class PlynlingStats
{
    public const int PassionBonus = 2;
    private const int BaseSalt = 200;           // + the stat

    public static readonly IReadOnlyList<PlynlingStat> All = Enum.GetValues<PlynlingStat>();
    public static readonly IReadOnlyList<PlynlingStat> Mind = All.Where(s => s <= PlynlingStat.Courage).ToArray();
    public static readonly IReadOnlyList<PlynlingStat> Body = All.Where(IsBody).ToArray();

    // A closed range, so an undefined value (a forged custom-id parsed by Enum.TryParse) is never one.
    public static bool IsBody(PlynlingStat stat) => stat is >= PlynlingStat.Force and <= PlynlingStat.Speed;

    public static string Name(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "Diplomatie",
        PlynlingStat.Stewardship => "Intendance",
        PlynlingStat.Learning => "Sagesse",
        PlynlingStat.Intrigue => "Ruse",
        PlynlingStat.Courage => "Courage",
        PlynlingStat.Force => "Force",
        PlynlingStat.Agility => "Agilité",
        PlynlingStat.Endurance => "Endurance",
        PlynlingStat.Intelligence => "Intelligence",
        PlynlingStat.Speed => "Vitesse",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static string Emoji(PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => "💬",
        PlynlingStat.Stewardship => "📦",
        PlynlingStat.Learning => "📚",
        PlynlingStat.Intrigue => "🦊",
        PlynlingStat.Courage => "🛡️",
        PlynlingStat.Force => "💪",
        PlynlingStat.Agility => "🤸",
        PlynlingStat.Endurance => "❤️",
        PlynlingStat.Intelligence => "🧠",
        PlynlingStat.Speed => "💨",
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

    // Each mushroom leans +1 on one body stat; the dormant sunflowers lean on none. Exhaustive: a new
    // species must be given its lean here, or this throws.
    public static PlynlingStat? LeanOf(PlynlingSpecies species) => species switch
    {
        PlynlingSpecies.Amanite or PlynlingSpecies.Mystique => PlynlingStat.Intelligence,
        PlynlingSpecies.Cepe => PlynlingStat.Endurance,
        PlynlingSpecies.Rose => PlynlingStat.Agility,
        PlynlingSpecies.Russule => PlynlingStat.Force,
        PlynlingSpecies.Dore or PlynlingSpecies.Coprin => PlynlingStat.Speed,
        PlynlingSpecies.Tournesol or PlynlingSpecies.Citron or PlynlingSpecies.Roux or PlynlingSpecies.Ivoire
            or PlynlingSpecies.Nocturne or PlynlingSpecies.Solaire => null,
        _ => throw new ArgumentOutOfRangeException(nameof(species), species, null),
    };

    public static int SpeciesLean(PlynlingSpecies species, PlynlingStat stat) => LeanOf(species) == stat ? 1 : 0;

    // What it was born with: the die, its species and its traits — no growth, no points, no state.
    // Énergie's maximum reads this for Endurance, so levelling up never buys more drills.
    public static int Innate(Plynling p, IReadOnlyList<TraitInfo> traits, PlynlingStat stat) =>
        Base(p, stat) + SpeciesLean(p.Species, stat) + traits.Sum(t => t.Stats.GetValueOrDefault(stat));

    public static int Base(Plynling p, PlynlingStat stat) =>
        PlynlingMascot.Is(p) ? PlynlingMascot.BaseStat : 1 + (int)(StableRoll.Unit(p.Id, BaseSalt + (int)stat, 0) * 6);

    public static int Growth(Plynling p, PlynlingStat stat) => stat switch
    {
        PlynlingStat.Diplomacy => p.GrowthDiplomacy,
        PlynlingStat.Stewardship => p.GrowthStewardship,
        PlynlingStat.Learning => p.GrowthLearning,
        PlynlingStat.Intrigue => p.GrowthIntrigue,
        PlynlingStat.Courage => p.GrowthCourage,
        PlynlingStat.Force => p.GrowthForce,
        PlynlingStat.Agility => p.GrowthAgility,
        PlynlingStat.Endurance => p.GrowthEndurance,
        PlynlingStat.Intelligence => p.GrowthIntelligence,
        PlynlingStat.Speed => p.GrowthSpeed,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static void AddGrowth(Plynling p, PlynlingStat stat, int amount)
    {
        switch (stat)
        {
            case PlynlingStat.Diplomacy: p.GrowthDiplomacy += amount; break;
            case PlynlingStat.Stewardship: p.GrowthStewardship += amount; break;
            case PlynlingStat.Learning: p.GrowthLearning += amount; break;
            case PlynlingStat.Intrigue: p.GrowthIntrigue += amount; break;
            case PlynlingStat.Courage: p.GrowthCourage += amount; break;
            case PlynlingStat.Force: p.GrowthForce += amount; break;
            case PlynlingStat.Agility: p.GrowthAgility += amount; break;
            case PlynlingStat.Endurance: p.GrowthEndurance += amount; break;
            case PlynlingStat.Intelligence: p.GrowthIntelligence += amount; break;
            case PlynlingStat.Speed: p.GrowthSpeed += amount; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }

    // The level points its owner placed (PlynlingRpg): body stats only, 0 for a mind stat.
    public static int Points(Plynling p, PlynlingStat stat) => stat switch
    {
        PlynlingStat.Force => p.PointsForce,
        PlynlingStat.Agility => p.PointsAgility,
        PlynlingStat.Endurance => p.PointsEndurance,
        PlynlingStat.Intelligence => p.PointsIntelligence,
        PlynlingStat.Speed => p.PointsSpeed,
        _ => 0,
    };

    // One level point into a body stat. Whether one is left to place is the caller's check
    // (PlynlingRpg.UnspentPoints); a mind stat takes no points and throws.
    public static void AddPoint(Plynling p, PlynlingStat stat)
    {
        switch (stat)
        {
            case PlynlingStat.Force: p.PointsForce++; break;
            case PlynlingStat.Agility: p.PointsAgility++; break;
            case PlynlingStat.Endurance: p.PointsEndurance++; break;
            case PlynlingStat.Intelligence: p.PointsIntelligence++; break;
            case PlynlingStat.Speed: p.PointsSpeed++; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, "level points go into body stats only");
        }
    }

    public static IReadOnlyList<StatLine> Compute(Plynling p, IReadOnlyList<TraitInfo> traits)
    {
        var passionStat = PassionStat(PassionFor(p));
        return All.Select(stat =>
        {
            var die = Base(p, stat);
            var passion = stat == passionStat ? PassionBonus : 0;
            var species = SpeciesLean(p.Species, stat);
            var fromTraits = traits.Sum(t => t.Stats.GetValueOrDefault(stat));
            var growth = Growth(p, stat);
            var points = Points(p, stat);
            var state = PlynlingStress.StatPenalty(PlynlingStress.Level(p.Stress)) + PlynlingModifiers.StatDelta(p, stat);
            return new StatLine(stat, die, passion, species, fromTraits, growth, points, state,
                Math.Max(0, die + passion + species + fromTraits + growth + points + state));
        }).ToList();
    }
}
