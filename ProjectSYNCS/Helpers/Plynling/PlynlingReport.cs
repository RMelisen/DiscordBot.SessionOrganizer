using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>A Plynling worth a look, by its owner: sick, or about to starve.</summary>
public sealed record PlynlingWatch(ulong OwnerId, string Name, bool Sick, bool Starving);

/// <summary>What /report plynlings shows. Built by <see cref="PlynlingReport.Compute"/>.</summary>
public sealed record PlynlingReportData(
    int Living,
    int Born,
    IReadOnlyList<(DeathCause Cause, int Count)> Deaths,
    TimeSpan? AverageLifespan,
    int Sick,
    int Starving,
    int Hungry,
    int Dirty,
    int Frozen,
    IReadOnlyList<(PlynlingStage Stage, int Count)> Stages,
    int EventsResolved,
    int DecidedAlone,
    int Challenges,
    int ChallengesWon,
    IReadOnlyList<(string Title, int Count)> TopEvents,
    IReadOnlyList<(PlynlingBond Bond, int Count)> Bonds,
    int Badges,
    int Traits,
    IReadOnlyList<PlynlingWatch> Watch);

/// <summary>
/// The Plynlings over one window, plus the living's state now. The caller loads the guild's
/// Plynlings AsNoTracking and settles them to now in memory (as /admin stats does); the other rows
/// are those Plynlings'. The mascot (Ping-Qilin) never counts. An abandoned Plynling is
/// hard-deleted with its history, so abandons can't be reported. Species is in /admin stats, not here.
/// A relation is counted once (its row), when its current bond began (Since). Pure.
/// </summary>
public static class PlynlingReport
{
    public const int TopEventCount = 5;
    public const int WatchCount = 10;

    public static PlynlingReportData Compute(
        IReadOnlyList<Plynling> plynlings, IReadOnlyList<PlynlingEventInstance> events, IReadOnlyList<PlynlingRelation> relations,
        IReadOnlyList<PlynlingBadge> badges, IReadOnlyList<PlynlingTrait> traits, ReportWindow window, DateTimeOffset now)
    {
        var pets = plynlings.Where(p => !PlynlingMascot.Is(p)).ToList();
        var ids = pets.Select(p => p.Id).ToHashSet();
        var living = pets.Where(p => p.DiedAt is null).ToList();
        var awake = living.Where(p => !PlynlingLife.IsFrozen(p)).ToList();
        var dead = pets.Where(p => p.DiedAt is { } died && window.Contains(died)).ToList();
        bool Starving(Plynling p) => PlynlingLife.HungerAt(p, now) < PlynlingLife.StarvingBelow;
        var resolved = events.Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt is { } at && window.Contains(at)).ToList();

        return new PlynlingReportData(
            living.Count,
            pets.Count(p => window.Contains(p.AdoptedAt)),
            dead.GroupBy(p => p.DeathCause).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList(),
            dead.Count == 0 ? null : TimeSpan.FromTicks((long)dead.Average(p => PlynlingLife.Age(p, p.DiedAt!.Value).Ticks)),
            awake.Count(PlynlingLife.IsSick),
            awake.Count(Starving),
            awake.Count(p => !Starving(p) && PlynlingLife.HungerAt(p, now) < PlynlingLife.HungryBelow),
            awake.Count(p => PlynlingLife.IsDirty(p, now)),
            living.Count(PlynlingLife.IsFrozen),
            living.GroupBy(p => PlynlingLife.Stage(p, now)).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())).ToList(),
            resolved.Count,
            resolved.Count(e => e.DecidedAlone),
            resolved.Count(e => e.ChallengeSucceeded is not null),
            resolved.Count(e => e.ChallengeSucceeded == true),
            resolved.GroupBy(e => e.EventKey)
                .Select(g => (PlynlingEvents.ByKey(g.Key)?.Title ?? g.Key, g.Count()))
                .OrderByDescending(x => x.Item2).Take(TopEventCount).ToList(),
            relations.Where(r => ids.Contains(r.PlynlingAId) && r.Bond != PlynlingBond.Acquaintances && window.Contains(r.Since))
                .GroupBy(r => r.Bond).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())).ToList(),
            badges.Count(b => ids.Contains(b.PlynlingId) && window.Contains(b.EarnedAt)),
            traits.Count(t => ids.Contains(t.PlynlingId) && window.Contains(t.AcquiredAt)),
            awake.Where(p => PlynlingLife.IsSick(p) || Starving(p))
                .OrderByDescending(PlynlingLife.IsSick).ThenBy(p => PlynlingLife.HungerAt(p, now))
                .Take(WatchCount)
                .Select(p => new PlynlingWatch(p.OwnerId, p.Name, PlynlingLife.IsSick(p), Starving(p)))
                .ToList());
    }
}
