using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>The stats with a server rank on /plynling stats (docs/agents/plynling.md, *Stats*).</summary>
public enum RankedStat { Score, Level, Meals, Pets, Plays, Drills, Visits, EventsLived, Badges }

/// <summary>A place on the server: 1 is first; Of is every Plynling of the guild, dead included, never the mascot.</summary>
public sealed record StatRank(int Rank, int Of);

/// <summary>What the sheet reads from the existing tables rather than from the counters.</summary>
public sealed record StatFacts(
    int TimesSick, int TimesFrozen, int Badges, int Memories,
    IReadOnlyDictionary<TraitKind, int> Traits, IReadOnlyDictionary<MarkKind, int> Marks,
    IReadOnlyDictionary<PlynlingBond, int> Bonds, int Meetings, string? MostMetName, int MostMetMeetings,
    int EventsLived, int ChallengesTried, int ChallengesWon, int StoriesFinished);

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
    IReadOnlyDictionary<RankedStat, StatRank> Ranks)
{
    public long Count(string key) => Counters.TryGetValue(key, out var v) ? v : 0;
}
