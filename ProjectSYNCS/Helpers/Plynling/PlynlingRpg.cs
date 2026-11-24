using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>What a drill did: the XP, the level before and after, whether the stat grew for good, the energy left.</summary>
public sealed record DrillResult(int XpGained, int LevelBefore, int LevelAfter, bool Boosted, double EnergyLeft);

/// <summary>
/// The RPG's rules (docs/agents/plynling-rpg.md), pure. A Plynling's XP, the points its owner placed
/// and its Énergie are stored; its level and the points left to place are derived from them. Énergie
/// works like the needs: a value at an instant, refilled with elapsed time, rebased on every spend.
/// Every roll is hashed (<see cref="StableRoll"/>), never a <see cref="Random"/>.
/// </summary>
public static class PlynlingRpg
{
    public const int MaxLevel = 100;
    public const int DrillXp = 10;
    public const int DrillCost = 15;
    public const int BaseEnergy = 100;
    public const int EnergyPerEndurance = 5;
    // A drill's permanent +1 to the stat it trained: rare, so levels stay the main way a body stat grows.
    // About eight drills a day at full energy: 0.5 % is one +1 every three or four weeks, under half the
    // points a year of training alone earns (the checks pin it). Growth is stored for good — never raise it
    // without re-running that check. None once the level cap is reached.
    public const double BoostChance = 0.005;
    public static readonly TimeSpan EnergyRefill = TimeSpan.FromHours(24);

    private const int DrillSalt = 500;
    // The curve's scale: with 1.5 as its power, the cap is reached in about a year of full daily play
    // (180 XP a day; training alone is 60, a third). The checks pin the days to 10, 50 and 100.
    private const double CurveScale = 66;
    private const double CurvePower = 1.5;

    /// <summary>The total XP a Plynling needs to reach <paramref name="level"/> (0 for level 1).</summary>
    public static long XpForLevel(int level) =>
        level <= 1 ? 0 : (long)Math.Round(CurveScale * Math.Pow(Math.Min(level, MaxLevel) - 1, CurvePower));

    /// <summary>The level <paramref name="xp"/> reaches, 1 to <see cref="MaxLevel"/>.</summary>
    public static int Level(long xp)
    {
        var level = 1;
        while (level < MaxLevel && XpForLevel(level + 1) <= xp) level++;
        return level;
    }

    /// <summary>One point per level past the first, minus those placed. Never below 0.</summary>
    public static int UnspentPoints(Plynling p) =>
        Math.Max(0, Level(p.RpgXp) - 1 - PlynlingStats.Body.Sum(s => PlynlingStats.Points(p, s)));

    /// <summary>
    /// Its maximum Énergie, from the Endurance it was born with (die, species, traits) — never points or
    /// growth, or a higher level would train more often and level faster still.
    /// </summary>
    public static int MaxEnergy(Plynling p, IReadOnlyList<TraitInfo> traits) =>
        BaseEnergy + EnergyPerEndurance * Math.Max(0, PlynlingStats.Innate(p, traits, PlynlingStat.Endurance));

    /// <summary>
    /// Its Énergie now: the stored value plus the refill since <see cref="Plynling.EnergyAsOf"/>, a full
    /// <paramref name="max"/> per <see cref="EnergyRefill"/>. A row that never spent any (EnergyAsOf at
    /// year 1) reads full; a clock going backwards refills nothing.
    /// </summary>
    public static double EnergyAt(Plynling p, int max, DateTimeOffset now)
    {
        var hours = Math.Max(0, (now - p.EnergyAsOf).TotalHours);
        return Math.Clamp(p.Energy + hours * max / EnergyRefill.TotalHours, 0, max);
    }

    /// <summary>When it will have <paramref name="amount"/> Énergie (now, if it already has).</summary>
    public static DateTimeOffset EnergyReadyAt(Plynling p, int max, double amount, DateTimeOffset now)
    {
        var missing = amount - EnergyAt(p, max, now);
        return missing <= 0 || max <= 0 ? now : now.AddHours(missing * EnergyRefill.TotalHours / max);
    }

    /// <summary>The RPG starts at ado and leaves the mascot out (no player behind her).</summary>
    public static bool CanTrain(Plynling p, DateTimeOffset now) =>
        !PlynlingMascot.Is(p) && PlynlingLife.Stage(p, now) >= PlynlingStage.Teen;

    /// <summary>
    /// A body-stat test's odds, in percent: a ratio, not the mind stats' difference, so ten levels' worth of
    /// points ahead is a clear edge but never a sure win, at any level. Clamped 5–95 like the events.
    /// </summary>
    public static int BodyChance(int stat, int against) =>
        Math.Clamp((int)Math.Round(100.0 * (Math.Max(0, stat) + 1) / (Math.Max(0, stat) + Math.Max(0, against) + 2)), 5, 95);

    /// <summary>
    /// The card's RPG line, inside the status text (never a component of its own): « Niv. 4 · ⚡ 12/120 ·
    /// 3 points à placer ». Null where the RPG doesn't apply: a bébé, the dead, the mascot.
    /// </summary>
    public static string? CardLine(Plynling p, IReadOnlyList<TraitInfo> traits, DateTimeOffset now)
    {
        if (p.DiedAt is not null || !CanTrain(p, now)) return null;
        var max = MaxEnergy(p, traits);
        var line = $"Niv. {Level(p.RpgXp)} · ⚡ {(int)Math.Floor(EnergyAt(p, max, now))}/{max}";
        return UnspentPoints(p) is var points and > 0 ? $"{line} · {PointsToPlace(points)}" : line;
    }

    public static string PointsToPlace(int points) => points == 1 ? "1 point à placer" : $"{points} points à placer";

    /// <summary>
    /// One drill of a body stat: spends <see cref="DrillCost"/> Énergie (the caller checked there was
    /// enough), gives <see cref="DrillXp"/> (stopping at the cap) and, rarely, a permanent +1 to that stat.
    /// The roll is keyed on the drill count, so the same drill always decides the same way.
    /// </summary>
    public static DrillResult Drill(Plynling p, IReadOnlyList<TraitInfo> traits, PlynlingStat drill, DateTimeOffset now)
    {
        if (!PlynlingStats.IsBody(drill))
            throw new ArgumentOutOfRangeException(nameof(drill), drill, "only body stats are drilled");

        var max = MaxEnergy(p, traits);
        p.Energy = Math.Max(0, EnergyAt(p, max, now) - DrillCost);
        p.EnergyAsOf = now;

        var before = Level(p.RpgXp);
        var xpBefore = p.RpgXp;
        p.RpgXp = Math.Min(XpForLevel(MaxLevel), p.RpgXp + DrillXp);

        var boosted = before < MaxLevel && StableRoll.Unit(p.Id, DrillSalt, (int)p.Drills) < BoostChance;
        if (boosted) PlynlingStats.AddGrowth(p, drill, 1);
        p.Drills++;

        return new DrillResult((int)(p.RpgXp - xpBefore), before, Level(p.RpgXp), boosted, p.Energy);
    }
}
