using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// What two Plynlings are to each other. Stored as an int on PlynlingRelation, so
// **append-only**: a value inserted in the middle would turn every later row into its neighbour.
public enum PlynlingBond { Acquaintances, Friends, BestFriends, Lovers, Rivals, Enemies }

public enum Confession { None, Accepted, Refused }

/// <summary>
/// Every rule of the relationships, pure: no database, no clock, and each draw takes the
/// <see cref="Random"/> it should use. Visits move a pair's affinity (−100…+100) through a
/// good scene or a squabble; the bond follows the affinity (with a margin), except a couple,
/// which only a confession makes and only a slide below <see cref="BreakUpBelow"/> undoes.
/// </summary>
public static class PlynlingBonds
{
    public const int EnemiesAtMost = -60;
    public const int RivalsAtMost = -20;
    public const int FriendsFrom = 20;
    public const int BestFriendsFrom = 60;
    public const int ConfessFrom = 80;
    public const int BreakUpBelow = 40;
    public const int HeartbreakLoss = 25;
    public const double HeartbreakSadness = 0.20;

    /// <summary>
    /// After a refused confession, the pair may not confess again, in either direction, for this long:
    /// the brake that keeps couples near the old pace now that an owner may confess on every eligible
    /// visit (VisitOdds; the simulation counts it). Read from <see cref="PlynlingRelation.LastRefusalAt"/>.
    /// </summary>
    public static readonly TimeSpan RefusalCooldown = TimeSpan.FromDays(3);

    public static bool ConfessionCoolingDown(DateTimeOffset? lastRefusalAt, DateTimeOffset now) =>
        lastRefusalAt is { } at && now - at < RefusalCooldown;

    /// <summary>
    /// The share of a pair's eligible days on which a visit offers « 💌 » (the owner's choice, by
    /// simulation): offered on every one, an owner confessing at the first chance made couples twice as
    /// fast as before. A quarter matched the old automatic confession; 35 % (8.24.0) is a little faster.
    /// </summary>
    public const double ConfessionOfferChance = 0.35;

    /// <summary>
    /// Whether this pair's visits offer the confession on this Paris day. Hashed from the pair and the
    /// day, never a <see cref="Random"/>: starting the visit again the same day cannot reroll it.
    /// </summary>
    public static bool ConfessionOffered(int a, int b, int dayKey)
    {
        var (lo, hi) = a < b ? (a, b) : (b, a);
        unchecked
        {
            var h = (uint)(lo * 73856093) ^ (uint)(hi * 19349663) ^ (uint)(dayKey * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return h % 1000 < ConfessionOfferChance * 1000;
        }
    }
    public const double GriefCeiling = 0.20;

    /// <summary>
    /// A pair's hidden compatibility, −20…+20: some pairs click, others never will. Derived from
    /// the two ids in either order, so it needs no storage and never changes.
    /// </summary>
    public const int MaxCompatibility = 20;

    public static int Compatibility(int a, int b)
    {
        var (lo, hi) = a < b ? (a, b) : (b, a);
        unchecked
        {
            var h = (uint)(lo * 73856093) ^ (uint)(hi * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (int)(h % 41) - 20;
        }
    }

    // Tuned by simulation (see docs/agents/plynling.md): BaseSceneChance sits a little above the
    // ~51 % at which good (+12…17) and bad (−12…18) scenes cancel out, and AffinityPull draws a pair
    // back toward the middle, so where a pair settles is set by its compatibility — about
    // +17 + 4 × compatibility — with wide scatter,
    // rather than every pair ending as best friends. The pull is also what keeps enemies from
    // being a dead end.
    public const double BaseSceneChance = 0.55;
    public const double AffinityPull = 400;
    public const double EnemiesPenalty = 0.05;   // a grudge: enemies find it a little harder to make up
    public const double LoversBonus = 0.10;      // a couple feels no pull, or nearly all would break up
    // How far past a band's edge affinity must go before the bond changes, so a pair sitting on a
    // boundary does not flip back and forth (and get announced) every visit.
    public const int BondMargin = 10;

    // The chance a visit goes well: tilted by compatibility, pulled back by how far the pair already is
    // from the middle — except a couple, which instead gets a bonus.
    public static double GoodSceneChance(int compatibility, PlynlingBond bond, int affinity)
    {
        var chance = BaseSceneChance + compatibility / 100.0
                     + (bond == PlynlingBond.Lovers ? LoversBonus : -affinity / AffinityPull)
                     - (bond == PlynlingBond.Enemies ? EnemiesPenalty : 0);
        return Math.Clamp(chance, 0.05, 0.95);
    }

    /// <summary>One visit's scene: whether it went well, and what it did to their affinity.</summary>
    public static (bool Good, int Delta) RollScene(int compatibility, PlynlingBond bond, int affinity, Random rng) =>
        rng.NextDouble() < GoodSceneChance(compatibility, bond, affinity)
            ? (true, rng.Next(12, 18))
            : (false, -rng.Next(12, 19));

    /// <summary>
    /// The bond an affinity makes. A couple stays a couple until its affinity falls below
    /// <see cref="BreakUpBelow"/> — then it becomes whatever that affinity says. Any other bond
    /// changes only once affinity is <see cref="BondMargin"/> past the edge of its band.
    /// </summary>
    public static PlynlingBond BondFor(int affinity, PlynlingBond current)
    {
        if (current == PlynlingBond.Lovers)
            return affinity >= BreakUpBelow ? PlynlingBond.Lovers : Band(affinity);
        var next = Band(affinity);
        if (next == current) return current;
        return Band(affinity + (Rank(next) > Rank(current) ? -BondMargin : BondMargin));
    }

    private static PlynlingBond Band(int affinity) =>
        affinity <= EnemiesAtMost ? PlynlingBond.Enemies
        : affinity <= RivalsAtMost ? PlynlingBond.Rivals
        : affinity >= BestFriendsFrom ? PlynlingBond.BestFriends
        : affinity >= FriendsFrom ? PlynlingBond.Friends
        : PlynlingBond.Acquaintances;

    // Bands from worst to best, to tell which way a pair is moving.
    private static int Rank(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Enemies => 0,
        PlynlingBond.Rivals => 1,
        PlynlingBond.Acquaintances => 2,
        PlynlingBond.Friends => 3,
        _ => 4,
    };

    // Whether a pair may confess: best friends, a boy and a girl, close enough, and neither already
    // in a couple (one partner at a time). MayConfess adds the refusal cooldown: that is the rule the
    // visit's « 💌 » and the declaration event both go by.
    public static bool CanConfess(PlynlingBond bond, int affinity, Plynling a, Plynling b, bool eitherInCouple) =>
        bond == PlynlingBond.BestFriends && affinity >= ConfessFrom && a.Gender != b.Gender && !eitherInCouple;

    // CanConfess, and no refusal between the two in the last RefusalCooldown (either way, whether the
    // refusal came from a visit or from the village declaration): « 💌 » is offered on it and the
    // declaration event targets with it, so neither is a way round the other's wait.
    public static bool MayConfess(PlynlingBond bond, int affinity, Plynling a, Plynling b, bool eitherInCouple,
        DateTimeOffset? lastRefusalAt, DateTimeOffset now) =>
        !ConfessionCoolingDown(lastRefusalAt, now) && CanConfess(bond, affinity, a, b, eitherInCouple);

    // What a visit does to both Plynlings' happiness, by the bond it ends on.
    public static double VisitHappiness(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => 0.25,
        PlynlingBond.BestFriends => 0.30,
        PlynlingBond.Lovers => 0.40,
        PlynlingBond.Rivals => 0.10,
        PlynlingBond.Enemies => -0.10,
        _ => 0.20,
    };

    // The grief bonds: losing one of these (death, abandonment) hurts.
    public static bool IsClose(PlynlingBond bond) => bond is PlynlingBond.BestFriends or PlynlingBond.Lovers;

    // The bonds a hostile event (TargetKind.Hostile) is with.
    public static bool IsHostile(PlynlingBond bond) => bond is PlynlingBond.Rivals or PlynlingBond.Enemies;

    // How close a bond is, for sorting a Plynling's relations: partner first, enemies last.
    public static int Closeness(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Lovers => 0,
        PlynlingBond.BestFriends => 1,
        PlynlingBond.Friends => 2,
        PlynlingBond.Acquaintances => 3,
        PlynlingBond.Rivals => 4,
        _ => 5,
    };

    public static string Emoji(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => "🤝",
        PlynlingBond.BestFriends => "💛",
        PlynlingBond.Lovers => "💞",
        PlynlingBond.Rivals => "⚡",
        PlynlingBond.Enemies => "😠",
        _ => "🙂",
    };

    // What the other one is to it, agreed with the *other* Plynling: « son amie », « rival »…
    public static string Role(PlynlingBond bond, PlynlingGender other) => bond switch
    {
        PlynlingBond.Friends => other.Agree("ami", "amie"),
        PlynlingBond.BestFriends => other.Agree("meilleur ami", "meilleure amie"),
        PlynlingBond.Lovers => other.Agree("amoureux", "amoureuse"),
        PlynlingBond.Rivals => other.Agree("rival", "rivale"),
        PlynlingBond.Enemies => other.Agree("ennemi", "ennemie"),
        _ => "connaissance",
    };

    /// <summary>
    /// The announcement when a pair's bond changes. Plural agreement: feminine only when both
    /// are girls. Names are already sanitised.
    /// </summary>
    public static string ChangeLine(PlynlingBond bond, string a, PlynlingGender ga, string b, PlynlingGender gb)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        string P(string m, string f) => girls ? f : m;
        return bond switch
        {
            PlynlingBond.Friends => $"🤝 **{a}** et **{b}** sont {P("devenus amis", "devenues amies")} !",
            PlynlingBond.BestFriends => $"💛 **{a}** et **{b}** sont {P("devenus meilleurs amis", "devenues meilleures amies")} !",
            PlynlingBond.Lovers => $"💞 **{a}** et **{b}** sont amoureux !",
            PlynlingBond.Rivals => $"⚡ **{a}** et **{b}** sont {P("devenus rivaux", "devenues rivales")}…",
            PlynlingBond.Enemies => $"😠 **{a}** et **{b}** sont {P("devenus ennemis", "devenues ennemies")}…",
            _ => $"🙂 **{a}** et **{b}** ne sont plus que des connaissances.",
        };
    }
}
