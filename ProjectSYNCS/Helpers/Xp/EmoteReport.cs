using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>One of the server's own emotes, from the live guild: its id and markup.</summary>
public sealed record ServerEmote(ulong Id, string Markup);

public sealed record EmoteLine(string Markup, long Count);

/// <summary>What /report emotes shows. Built by <see cref="EmoteReport.Compute"/>.</summary>
public sealed record EmoteReportData(
    long Own,
    long Foreign,
    long Unicode,
    IReadOnlyList<EmoteLine> TopOwn,
    IReadOnlyList<string> UnusedOwn,
    IReadOnlyList<EmoteLine> TopForeign,
    IReadOnlyDictionary<ulong, long> OwnById,
    IReadOnlyList<ServerEmote> Server);

/// <summary>
/// Emote health over one window, from EmoteDailyStat rows (written and reacted together; buckets
/// from 2026-08-05). Emotes from other servers are counted by EmoteTracker too: an emote is the
/// server's own when its id is in the live guild's list. A server emote with no row is « never
/// used ». A foreign emote with no EmoteStat row shows by id. Pure.
/// </summary>
public static class EmoteReport
{
    public const int TopOwnCount = 10;
    public const int TopForeignCount = 5;
    public const int MoverCount = 3;

    public static EmoteReportData Compute(
        IReadOnlyList<EmoteDailyStat> rows, IReadOnlyList<ServerEmote> server, IReadOnlyDictionary<(ulong, string), string> markup)
    {
        var own = server.ToDictionary(e => e.Id);
        var per = rows.GroupBy(r => (r.EmoteId, r.Unicode))
            .Select(g => (g.Key.EmoteId, g.Key.Unicode, Count: g.Sum(r => r.WrittenCount + r.ReactedCount)))
            .Where(x => x.Count > 0)
            .ToList();
        var ownCounts = per.Where(x => x.EmoteId != 0 && own.ContainsKey(x.EmoteId)).ToDictionary(x => x.EmoteId, x => x.Count);
        var foreign = per.Where(x => x.EmoteId != 0 && !own.ContainsKey(x.EmoteId)).ToList();

        return new EmoteReportData(
            ownCounts.Values.Sum(),
            foreign.Sum(x => x.Count),
            per.Where(x => x.EmoteId == 0).Sum(x => x.Count),
            ownCounts.OrderByDescending(x => x.Value).Take(TopOwnCount).Select(x => new EmoteLine(own[x.Key].Markup, x.Value)).ToList(),
            server.Where(e => !ownCounts.ContainsKey(e.Id)).Select(e => e.Markup).ToList(),
            foreign.OrderByDescending(x => x.Count).Take(TopForeignCount)
                .Select(x => new EmoteLine(markup.TryGetValue((x.EmoteId, x.Unicode), out var m) && m.Length > 0 ? m : $"`{x.EmoteId}`", x.Count))
                .ToList(),
            ownCounts,
            server);
    }

    /// <summary>The server's emotes that rose (or fell) the most against the previous window, with the change.</summary>
    public static IReadOnlyList<EmoteLine> Movers(EmoteReportData current, EmoteReportData previous, bool rising) =>
        current.Server
            .Select(e => new EmoteLine(e.Markup, current.OwnById.GetValueOrDefault(e.Id) - previous.OwnById.GetValueOrDefault(e.Id)))
            .Where(l => rising ? l.Count > 0 : l.Count < 0)
            .OrderByDescending(l => rising ? l.Count : -l.Count)
            .Take(MoverCount)
            .ToList();
}
