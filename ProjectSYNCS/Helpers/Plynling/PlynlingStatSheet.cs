using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>The stats with a server rank on /plynling stats (docs/agents/plynling.md, *Stats*). Never stored; appended anyway.</summary>
public enum RankedStat { Score, Level, Meals, Pets, Plays, Drills, Visits, EventsLived, Badges, Hosted, Baths, WinRate, Week }

/// <summary>A place on the server: 1 is first; Of is every Plynling of the guild, dead included, never the mascot.</summary>
public sealed record StatRank(int Rank, int Of);

/// <summary>Another Plynling it is close to, and since when the bond holds (PlynlingRelation.Since).</summary>
public sealed record StatFriend(string Name, DateTimeOffset Since);

/// <summary>What two Plynlings of a duel are to each other, or null on the duel when they never met.</summary>
public sealed record StatPair(PlynlingBond Bond, int Meetings, DateTimeOffset Since, bool BothFemale);

/// <summary>
/// What the sheet reads from the existing tables rather than from the counters. Met / Population: the Plynlings
/// it has a relation with, out of every other Plynling of the guild. FirstWinAfter / FirstFriendAfter: from its
/// adoption to that journal moment, null when the journal holds none (never happened, or trimmed past the cap).
/// LuckiestPercent: the lowest chance of a challenge it won. EventStress: the stress its resolved events left.
/// </summary>
public sealed record StatFacts(
    int TimesSick, int TimesFrozen, int Badges, int Memories,
    IReadOnlyDictionary<TraitKind, int> Traits, IReadOnlyDictionary<MarkKind, int> Marks,
    IReadOnlyDictionary<PlynlingBond, int> Bonds, int Meetings, string? MostMetName, int MostMetMeetings,
    int EventsLived, int ChallengesTried, int ChallengesWon, int StoriesFinished,
    int Met = 0, int Population = 0, StatFriend? Partner = null, StatFriend? BestFriend = null,
    int BreakUps = 0, int Heartbreaks = 0, TimeSpan? FirstWinAfter = null, TimeSpan? FirstFriendAfter = null,
    int DecidedAlone = 0, int? LuckiestPercent = null, int EventStress = 0);

/// <summary>
/// Its score over time, from the daily buckets (PlynlingScoreDay, kept since the score shipped). Week: the last
/// seven Paris days, today included. BestDay: that day's key (0 for none). Streak: days in a row with points,
/// ending today, or yesterday while today has none yet. Last14: the last fourteen days, oldest first.
/// </summary>
public sealed record StatScore(long Week, int BestDay, long BestDayPoints, int Streak, int BestStreak, IReadOnlyList<long> Last14)
{
    public static readonly StatScore None = new(0, 0, 0, 0, 0, new long[14]);

    /// <summary>Reads the buckets (day key → points) as of <paramref name="today"/>'s key. Pure.</summary>
    public static StatScore From(IReadOnlyDictionary<int, long> byDay, int today)
    {
        static DateOnly D(int k) => new(k / 10000, k / 100 % 100, k % 100);
        static int K(DateOnly d) => d.Year * 10000 + d.Month * 100 + d.Day;
        var t = D(today);
        var active = byDay.Where(kv => kv.Value > 0).Select(kv => D(kv.Key)).ToHashSet();

        var last14 = Enumerable.Range(0, 14).Select(i => byDay.GetValueOrDefault(K(t.AddDays(i - 13)))).ToList();
        var week = last14.Skip(7).Sum();
        var best = byDay.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).FirstOrDefault();

        var streak = 0;
        for (var d = active.Contains(t) ? t : t.AddDays(-1); active.Contains(d); d = d.AddDays(-1)) streak++;
        int bestStreak = 0, run = 0;
        DateOnly? prev = null;
        foreach (var d in active.OrderBy(d => d))
        {
            run = prev is { } p && p.AddDays(1) == d ? run + 1 : 1;
            bestStreak = Math.Max(bestStreak, run);
            prev = d;
        }
        return new StatScore(week, best.Key, best.Value, streak, bestStreak, last14);
    }
}

/// <summary>
/// Everything /plynling stats shows about one Plynling, read once (PlynlingService.GetStatSheetAsync, read-only).
/// Partial: adopted before PlynlingCounters.Since, so its counters and carers miss its first days. Ranks holds
/// only the stats it has a place in (above 0, never for the mascot).
/// </summary>
public sealed record PlynlingStatSheet(
    Plynling Plynling, DateTimeOffset Now, bool Partial,
    IReadOnlyDictionary<string, long> Counters,
    IReadOnlyList<PlynlingCarer> Carers,
    StatFacts Facts,
    IReadOnlyDictionary<RankedStat, StatRank> Ranks,
    StatScore? Score = null)
{
    public long Count(string key) => Counters.TryGetValue(key, out var v) ? v : 0;
    public StatScore History => Score ?? StatScore.None;
}
