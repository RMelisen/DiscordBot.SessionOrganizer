using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>A name and its count in /report minecraft: a player (by in-game name, lower case) or a resource (by key).</summary>
public sealed record McCountLine(string Name, long Count);

/// <summary>Her Minecraft life over one report window, from the daily buckets.</summary>
/// <param name="Minutes">Her minutes in game.</param>
/// <param name="DaysPlayed">The days with any minute in game.</param>
/// <param name="Orders">Orders confirmed under way.</param>
/// <param name="TopPlayers">Who gave the most, largest first, <see cref="MinecraftReport.ListedPlayers"/> at most.</param>
/// <param name="Gathered">Blocks gathered by resource (its body key), largest first.</param>
public sealed record MinecraftReportData(long Minutes, int DaysPlayed, long Orders, IReadOnlyList<McCountLine> TopPlayers, long Deaths,
    long Kills, long Fish, long Treasure, long Harvested, IReadOnlyList<McCountLine> Gathered)
{
    /// <summary>She was in game at all, or someone ordered her about.</summary>
    public bool Played => Minutes > 0 || Orders > 0;
}

/// <summary>/report minecraft's arithmetic over MinecraftDailyStat rows (McStats keys). Pure, walked by the checks.</summary>
public static class MinecraftReport
{
    public const int ListedPlayers = 5;

    public static MinecraftReportData Compute(IReadOnlyList<MinecraftDailyStat> rows)
    {
        long Sum(string key) => rows.Where(r => r.Key == key).Sum(r => r.Count);
        List<McCountLine> ByPrefix(string prefix) => rows
            .Where(r => r.Key.StartsWith(prefix, StringComparison.Ordinal))
            .GroupBy(r => r.Key[prefix.Length..])
            .Select(g => new McCountLine(g.Key, g.Sum(r => r.Count)))
            .Where(l => l.Count > 0)
            .OrderByDescending(l => l.Count).ThenBy(l => l.Name, StringComparer.Ordinal)
            .ToList();

        return new MinecraftReportData(
            Sum(McStats.Minutes),
            rows.Where(r => r.Key == McStats.Minutes && r.Count > 0).Select(r => r.Day).Distinct().Count(),
            Sum(McStats.Orders),
            ByPrefix(McStats.OrdersByPrefix).Take(ListedPlayers).ToList(),
            Sum(McStats.Deaths), Sum(McStats.Kills), Sum(McStats.Fish), Sum(McStats.Treasure), Sum(McStats.Harvested),
            ByPrefix(McStats.GatheredPrefix));
    }
}
