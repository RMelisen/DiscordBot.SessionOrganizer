using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// What two Plynlings are to each other. Stored as an int on PlynlingRelation, so
// **append-only**: a value inserted in the middle would turn every later row into its neighbour.
public enum PlynlingBond { Acquaintances, Friends, BestFriends, Lovers, Rivals, Enemies }

public enum Confession { None, Accepted, Refused }

/// <summary>
/// Every rule of the relationships, pure: no database, no clock, and each draw takes the
/// <see cref="Random"/> it should use. Visits move a pair's affinity (−100…+100) through a
/// good scene or a squabble; the bond follows the affinity, except a couple, which only a
/// confession makes and only a slide below <see cref="BreakUpBelow"/> undoes.
/// </summary>
public static class PlynlingBonds
{
    public const int EnemiesAtMost = -60;
    public const int RivalsAtMost = -20;
    public const int FriendsFrom = 20;
    public const int BestFriendsFrom = 60;
    public const int ConfessFrom = 80;
    public const int BreakUpBelow = 40;
    public const int HeartbreakLoss = 40;
    public const double HeartbreakSadness = 0.20;
    public const double ConfessionChance = 0.25;
    public const double GriefCeiling = 0.20;

    /// <summary>
    /// A pair's hidden compatibility, −15…+15: some pairs click, others never will. Derived from
    /// the two ids in either order, so it needs no storage and never changes.
    /// </summary>
    public static int Compatibility(int a, int b)
    {
        var (lo, hi) = a < b ? (a, b) : (b, a);
        unchecked
        {
            var h = (uint)(lo * 73856093) ^ (uint)(hi * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (int)(h % 31) - 15;
        }
    }

    // The chance a visit goes well: 75 %, tilted by compatibility and by how they already get on.
    public static double GoodSceneChance(int compatibility, PlynlingBond bond)
    {
        var chance = 0.75 + compatibility / 100.0
                     - (bond == PlynlingBond.Enemies ? 0.15 : bond == PlynlingBond.Rivals ? 0.05 : 0);
        return Math.Clamp(chance, 0.10, 0.95);
    }

    /// <summary>One visit's scene: whether it went well, and what it did to their affinity.</summary>
    public static (bool Good, int Delta) RollScene(int compatibility, PlynlingBond bond, Random rng) =>
        rng.NextDouble() < GoodSceneChance(compatibility, bond)
            ? (true, rng.Next(10, 16))
            : (false, -rng.Next(12, 19));

    /// <summary>
    /// The bond an affinity makes. A couple stays a couple until its affinity falls below
    /// <see cref="BreakUpBelow"/> — then it becomes whatever that affinity says.
    /// </summary>
    public static PlynlingBond BondFor(int affinity, PlynlingBond current) =>
        current == PlynlingBond.Lovers && affinity >= BreakUpBelow ? PlynlingBond.Lovers
        : affinity <= EnemiesAtMost ? PlynlingBond.Enemies
        : affinity <= RivalsAtMost ? PlynlingBond.Rivals
        : affinity >= BestFriendsFrom ? PlynlingBond.BestFriends
        : affinity >= FriendsFrom ? PlynlingBond.Friends
        : PlynlingBond.Acquaintances;

    // Whether a good visit can turn into a confession: best friends, a boy and a girl, close
    // enough, and neither already in a couple (one partner at a time).
    public static bool CanConfess(PlynlingBond bond, int affinity, Plynling a, Plynling b, bool eitherInCouple) =>
        bond == PlynlingBond.BestFriends && affinity >= ConfessFrom && a.Gender != b.Gender && !eitherInCouple;

    public static Confession RollConfession(int compatibility, Random rng)
    {
        if (rng.NextDouble() >= ConfessionChance) return Confession.None;
        return rng.NextDouble() < 0.5 + compatibility / 100.0 ? Confession.Accepted : Confession.Refused;
    }

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
